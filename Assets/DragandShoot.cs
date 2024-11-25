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

    private float velocityThreshold = 0.05f;
    private float bounceDampingFactor = 0.7f;

    void Start()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log($"Mouse down at: {mousePos}");

            if(Vector2.Distance(mousePos, transform.position) < 1f)
            {
                Debug.Log("Dragging Started.");
                isDragging = true;
                startDragPosition = mousePos;
            }
        }

        if(Input.GetMouseButtonUp(0) && isDragging)
        {
            Vector2 endDragPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragVector = startDragPosition - endDragPosition;
            float magnitude = dragVector.magnitude;
            Vector2 direction = dragVector.normalized;

            // Apply the velocity
            velocity = direction * magnitude * launchForceMultiplier;
            Debug.Log($"Drag Start: {startDragPosition}, End: {endDragPosition}, Magnitude: {magnitude}, Direction: {direction}, Velocity: {velocity}");

            isDragging = false;
            isLaunched = true;
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
        Debug.Log($"Simulated position: {transform.position}, velocity: {velocity}");

        CheckCollisions();
    }

    private void CheckCollisions()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, velocity.normalized, velocity.magnitude * Time.deltaTime);
        if(hit.collider != null)
        {
            Vector2 normal = hit.normal;
            velocity = Vector2.Reflect(velocity, normal);

            velocity *= bounceDampingFactor;

            transform.position = hit.point + normal * 0.01f;

            if(velocity.magnitude < velocityThreshold)
            {
                velocity = Vector2.zero;
                isLaunched = false;         
            }

            Debug.Log($"Hit: {hit.collider.name}, Velocity after bounce: {velocity}");
        }
    }
}
