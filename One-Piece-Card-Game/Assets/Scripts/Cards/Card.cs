using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class Card : MonoBehaviour
{
    private Collider2D col;
    private Vector3 startDragPos;
    private bool isDragging = false;
    private Quaternion startDragRotation;
    private Vector3 lastMousePos;
    
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float maxRotationAngle = 40f;

    void Start()
    {
        col = GetComponent<Collider2D>(); 
    }

    void Update()
    {
        if (Mouse.current == null) return; // safety check

        // Detect mouse down
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mouseWorldPos = GetMousePosInWorldSpace();

            // Check if we clicked on THIS card
            if (col == Physics2D.OverlapPoint(mouseWorldPos))
            {
                // Debug.Log("Mouse Down on Card");
                startDragPos = transform.position;
                startDragRotation = transform.rotation;
                lastMousePos = mouseWorldPos;
                isDragging = true;
                
                // Reset rotation immediately when picked up
                transform.rotation = Quaternion.identity;
            }
        }

        // While dragging
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector3 mouseWorldPos = GetMousePosInWorldSpace();
            transform.position = mouseWorldPos;
            
            // Calculate movement direction
            Vector3 movementDirection = mouseWorldPos - lastMousePos;
            
            // Only rotate if there's significant movement to avoid jittery rotation
            if (movementDirection.magnitude > 0.01f)
            {
                // Calculate angle based on movement direction
                float targetAngle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
                
                // Adjust angle so card faces forward when moving right (0 degrees = facing right)
                targetAngle -= 90f; // Subtract 90 to make "up" direction 0 degrees for card orientation
                
                // Clamp the angle to the maximum rotation range
                targetAngle = Mathf.Clamp(targetAngle, -maxRotationAngle, maxRotationAngle);
                
                // Apply rotation with smoothing
                Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            lastMousePos = mouseWorldPos;
        }

        // On mouse up
        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            // Debug.Log("Mouse Up on Card");
            isDragging = false;

            col.enabled = false;
            Collider2D hitCollider = Physics2D.OverlapPoint(transform.position);
            col.enabled = true;

            if (hitCollider != null && hitCollider.TryGetComponent(out ICardDropArea dropArea))
            {
                // Reset rotation when successfully dropped
                transform.rotation = Quaternion.identity;
                
                dropArea.OnCardDrop(this);
                
                // Remove card from hand if it was successfully dropped on a drop area
                if (HandManager.Instance != null && HandManager.Instance.IsCardInHand(gameObject))
                {
                    HandManager.Instance.RemoveCardFromHand(gameObject);
                }
            }
            else
            {
                transform.position = startDragPos; // reset if not dropped in valid area
                // Reset rotation when releasing the card (only if not dropped successfully)
                transform.rotation = startDragRotation;
            }
        }
    }

    private Vector3 GetMousePosInWorldSpace()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;
        return worldPos;
    }
}
