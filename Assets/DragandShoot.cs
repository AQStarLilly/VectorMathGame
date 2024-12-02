using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragandShoot : MonoBehaviour
{
    private Vector2 startDragPosition;
    private Vector2 velocity;
    private bool isDragging = false;
    private bool isLaunched = false;

    [SerializeField] private float launchForceMultiplier = 5f;
    [SerializeField] private float gravity = 9.8f;
    private float gravityScale = 0.5f;

    [SerializeField] private int trajectoryResolution = 20;
    [SerializeField] private LineRenderer trajectoryLine; 
    [SerializeField] private float maxDragDistance = 3f;

    private float velocityThreshold = 0.05f;
    private float bounceDampingFactor = 0.7f;

    [SerializeField] private GameObject goalZone; // Assign goal zone in Inspector
    [SerializeField] private int maxShots = 6; // Maximum allowed shots
    private int shotsUsed = 0; // Shots taken by the player
    private bool gameWon = false;

    void Start()
    {
        Time.timeScale = 1f;
        trajectoryLine.positionCount = 0;
    }

    void Update()
    {
        if (gameWon) return;

        if(Input.GetMouseButtonDown(0) && shotsUsed < maxShots)    
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if(Vector2.Distance(mousePos, transform.position) < 1f)
            {
                isDragging = true;
                startDragPosition = mousePos;

                trajectoryLine.positionCount = 2;
                trajectoryLine.SetPosition(0, transform.position);
                trajectoryLine.SetPosition(1, transform.position);
            }
        }

        if(Input.GetMouseButton(0) && isDragging)
        {
            Vector2 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragVector = startDragPosition - currentMousePosition;

            if (dragVector.magnitude > maxDragDistance)
            {
                dragVector = dragVector.normalized * maxDragDistance;
            }

            Vector2 clampedPosition = (Vector2)transform.position + dragVector;
            trajectoryLine.SetPosition(1, clampedPosition);

            float magnitude = dragVector.magnitude;
            Vector2 direction = dragVector.normalized;

            Vector2 simulatedVelocity = direction * magnitude * launchForceMultiplier;

            DrawTrajectory(transform.position, simulatedVelocity);
        }

        if(Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            trajectoryLine.positionCount = 0;
            Vector2 endDragPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragVector = startDragPosition - endDragPosition;

            float magnitude = Mathf.Min(dragVector.magnitude, maxDragDistance);
            Vector2 direction = dragVector.normalized;

            // Apply the velocity
            velocity = direction * magnitude * launchForceMultiplier;                      
            isLaunched = true;

            shotsUsed++;
            if(shotsUsed >= maxShots && !gameWon)
            {
                Debug.Log("Out of Shots! You Lose.");
            }
        }

        if (isLaunched)
        {
            SimulatePhysics();
        }
    }   

    private void SimulatePhysics()
    {
        velocity.y -= gravity * gravityScale * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        CheckCollisions();
    }

    //switch things around so you can bounce off of walls but not the floor - check out Boxcast as well, might work better for my detection
    private void CheckCollisions()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, velocity.normalized, velocity.magnitude * Time.deltaTime);
        if(hit.collider != null)
        {
            Vector2 normal = hit.normal;
            
            if(Vector2.Dot(normal, Vector2.up) > 0.7f)
            {
                velocity = Vector2.zero;
                isLaunched = false;
                Debug.Log("Hit the ground. Stopping");
            }
            else if(Vector2.Dot(normal, Vector2.left) > 0.7f || Vector2.Dot(normal, Vector2.right) > 0.7f)
            {
                velocity = Vector2.Reflect(velocity, normal) * bounceDampingFactor;
                Debug.Log("Hit a wall. Reflecting.");
            }
            else if(Vector2.Dot(normal, Vector2.down) > 0.7f)
            {
                velocity = Vector2.Reflect(velocity, normal) * bounceDampingFactor;
                Debug.Log("Hit the roof. Reflecting velocity.");
            }

            transform.position = hit.point + normal * 0.01f;

            if(velocity.magnitude < velocityThreshold)
            {
                velocity = Vector2.zero;
                isLaunched = false;
                Debug.Log("Velocity too small. Stopping");
            }
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if(collision.gameObject == goalZone)
        {
            gameWon = true;
            Debug.Log("You win!");
        }
    }

    private void DrawTrajectory(Vector2 startPosition, Vector2 startVelocity)
    {
        Vector2 currentPosition = startPosition;
        Vector2 currentVelocity = startVelocity * 2;

        trajectoryLine.positionCount = 0;
        List<Vector3> trajectoryPoints = new List<Vector3>();

        int maxBounces = 3;
        int bounceCount = 0;

        float maxTrajectoryLength = maxDragDistance * 2;
        float currentLength = 0f;

        while(bounceCount <= maxBounces && trajectoryPoints.Count < trajectoryResolution)
        {
            trajectoryPoints.Add(currentPosition);
            Vector2 nextPosition = currentPosition + currentVelocity * Time.fixedDeltaTime;
            Vector2 trajectoryStep = nextPosition - currentPosition;

            currentLength += trajectoryStep.magnitude;
            if (currentLength >= maxTrajectoryLength) break; // Stop if trajectory reaches the desired length

            RaycastHit2D hit = Physics2D.Raycast(currentPosition, trajectoryStep.normalized, trajectoryStep.magnitude);
            if(hit.collider != null)
            {
                trajectoryPoints.Add(hit.point);

                Vector2 normal = hit.normal;
                currentVelocity = Vector2.Reflect(currentVelocity, normal);
                currentPosition = hit.point + normal * 0.01f;

                bounceCount++;
            }
            else
            {
                currentPosition = nextPosition;
                currentVelocity.y -= gravity * gravityScale * Time.fixedDeltaTime;
            }
        }

        trajectoryLine.positionCount = trajectoryPoints.Count;
        trajectoryLine.SetPositions(trajectoryPoints.ToArray());
    }
}
