using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Analytics;

public class DragandShoot : MonoBehaviour
{
    //Variables for tracking drag and velocity
    private Vector2 startDragPosition;
    private Vector2 velocity;
    private bool isDragging = false;
    private bool isLaunched = false;

    //Settings for launch force and gravity
    [SerializeField] private float launchForceMultiplier = 5f;
    [SerializeField] private float gravity = 9.8f;
    private float gravityScale = 0.5f;

    //Trajectory visual settings
    [SerializeField] private int trajectoryResolution = 20;
    [SerializeField] private LineRenderer trajectoryLine; 
    [SerializeField] private float maxDragDistance = 3f;

    //Thresholds for stopping movement and bounce damping
    private float velocityThreshold = 0.05f;
    private float bounceDampingFactor = 0.7f;

    //Gamestate and UI
    [SerializeField] private GameObject goalZone; //Goal area for player to reach
    [SerializeField] private int maxShots = 6; // Maximum allowed shots
    private int shotsUsed = 0; // Shots taken by the player
    private bool gameWon = false;
    private bool waitingForLastShot = false;

    [SerializeField] private TextMeshProUGUI shotsLeftText;  //UI element for displaying remaining shots
    [SerializeField] private TextMeshProUGUI winLoseText;  //UI element for win/lose state

    void Start()
    {
        //Initialize game state and UI
        Time.timeScale = 1f;
        trajectoryLine.positionCount = 0; //hide trajectoyr initially

        UpdateShotsLeftUI();
        winLoseText.text = "";   //Clear win/lose message
    }

    void Update()
    {
        if (gameWon) return;  //skip input handling if game is already won

        if(Input.GetMouseButtonDown(0) && shotsUsed < maxShots)    //start dragging if the player clicks on the object and has shots left 
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if(Vector2.Distance(mousePos, transform.position) < 1f)
            {
                isDragging = true;
                startDragPosition = mousePos;

                //Initialize trajectory visualization
                trajectoryLine.positionCount = 2;
                trajectoryLine.SetPosition(0, transform.position);
                trajectoryLine.SetPosition(1, transform.position);
            }
        }

        if(Input.GetMouseButton(0) && isDragging)   //Update drag position and trajectory while dragging
        {
            Vector2 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragVector = startDragPosition - currentMousePosition;

            //Clamp Drag distance to max allowed value
            if (dragVector.magnitude > maxDragDistance)
            {
                dragVector = dragVector.normalized * maxDragDistance;
            }

            Vector2 clampedPosition = (Vector2)transform.position + dragVector;
            trajectoryLine.SetPosition(1, clampedPosition);

            //Simulate and display the trajectory
            float magnitude = dragVector.magnitude;
            Vector2 direction = dragVector.normalized;
            Vector2 simulatedVelocity = direction * magnitude * launchForceMultiplier;
            DrawTrajectory(transform.position, simulatedVelocity);
        }

        if(Input.GetMouseButtonUp(0) && isDragging)  //Launch the object on mouse release
        {
            isDragging = false;
            trajectoryLine.positionCount = 0;  //hide trajectory visualization
            Vector2 endDragPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragVector = startDragPosition - endDragPosition;

            float magnitude = Mathf.Min(dragVector.magnitude, maxDragDistance);
            Vector2 direction = dragVector.normalized;

            // Apply the velocity for launching
            velocity = direction * magnitude * launchForceMultiplier;                      
            isLaunched = true;

            shotsUsed++;
            UpdateShotsLeftUI();

            if(shotsUsed >= maxShots)  //Check if the player is out of shots
            {
                waitingForLastShot = true;
            }
        }

        if (isLaunched)  //Simulate physics if the object is in motion
        {
            SimulatePhysics();
        }
    }   

    private void SimulatePhysics()
    {
        //Apply gravity and update position
        velocity.y -= gravity * gravityScale * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        
        CheckCollisions();

        //Handle end of game conditions after the last shot
        if(waitingForLastShot && velocity.magnitude < velocityThreshold)
        {
            isLaunched = false;
            waitingForLastShot = false;

            if (!gameWon)
            {
                HandleLoseCondition();
            }
        }
    }

    //switch things around so you can bounce off of walls but not the floor - check out Boxcast as well, might work better for my detection
    private void CheckCollisions()
    {
        //perform a raycast to detect collision
        RaycastHit2D hit = Physics2D.Raycast(transform.position, velocity.normalized, velocity.magnitude * Time.deltaTime);
        if(hit.collider != null)
        {
            Vector2 normal = hit.normal;
            
            //Stop movement if hitting the ground
            if(Vector2.Dot(normal, Vector2.up) > 0.7f)  
            {
                velocity = Vector2.zero;
                isLaunched = false;
                Debug.Log("Hit the ground. Stopping");
            }
            //Reflect velocity for wall collisions
            else if(Vector2.Dot(normal, Vector2.left) > 0.7f || Vector2.Dot(normal, Vector2.right) > 0.7f)
            {
                velocity = Vector2.Reflect(velocity, normal) * bounceDampingFactor;
                Debug.Log("Hit a wall. Reflecting.");
            }
            //Reflect velocity for ceiling collisions
            else if(Vector2.Dot(normal, Vector2.down) > 0.7f)
            {
                velocity = Vector2.Reflect(velocity, normal) * bounceDampingFactor;
                Debug.Log("Hit the roof. Reflecting velocity.");
            }

            transform.position = hit.point + normal * 0.01f; //adjust position slighlty to avoid repeated collisions

            //stop if velocity is to low
            if (velocity.magnitude < velocityThreshold)  
            {
                velocity = Vector2.zero;
                isLaunched = false;
                Debug.Log("Velocity too small. Stopping");
            }
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        //Handle win condition if the goal is reached
        if(collision.gameObject == goalZone)
        {
            HandleWinCondition();
        }
    }

    private void HandleWinCondition()
    {
        gameWon = true;
        winLoseText.text = "You Win!";
        Time.timeScale = 0f;  //pause the game
    }

    private void HandleLoseCondition()
    {
        winLoseText.text = "Out of shots! Better luck next time";
        Time.timeScale = 0f;  //pause the game
    }

    private void UpdateShotsLeftUI() 
    {
        //Update the shots remaining UI
        shotsLeftText.text = "Shots Left: " + (maxShots - shotsUsed);
    }

    private void DrawTrajectory(Vector2 startPosition, Vector2 startVelocity)
    {
        //Simulate and visualize the remaining UI
        Vector2 currentPosition = startPosition;
        Vector2 currentVelocity = startVelocity * 2;

        trajectoryLine.positionCount = 0;
        List<Vector3> trajectoryPoints = new List<Vector3>();

        int maxBounces = 3;  //limit the number of trajectory bounces
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

        //Update the trajectory line renderer
        trajectoryLine.positionCount = trajectoryPoints.Count;
        trajectoryLine.SetPositions(trajectoryPoints.ToArray());
    }
}
