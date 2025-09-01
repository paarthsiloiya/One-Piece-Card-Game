using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Linq;

public class Card : MonoBehaviour
{
    private Collider2D col;
    public SpriteRenderer spriteRenderer;
    private Vector3 startDragPos;
    private bool isDragging = false;
    private Quaternion startDragRotation;
    private Vector3 lastMousePos;
    
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float maxRotationAngle = 40f;
    
    // Hover detection
    private bool isHovered = false;
    
    // Events for hover
    public delegate void CardHoverEvent(GameObject card);
    public event CardHoverEvent OnHoverEnter;
    public event CardHoverEvent OnHoverExit;
    
    // Public property to expose dragging state
    public bool IsDragging => isDragging;

    // Flag to track if card has been placed in a drop area
    private bool isPlacedInDropArea = false;

    // Add a new field to track if card is in discard pile
    private bool isInDiscardPile = false;

    private CardModel cardModel;

    // Static field to track if any card is being dragged
    private static bool isAnyCardBeingDragged = false;
    
    // Add these fields
    private ICardDropArea currentDropArea;
    [SerializeField] private float discardAnimationDuration = 0.5f;
    
    // Add these fields to track sorting orders
    private int[] originalSortingOrders;
    private SpriteRenderer[] cardRenderers;
    
    public void Setup(CardModel cardModel)
    {
        this.cardModel = cardModel;
        
        // Initialize components if they haven't been initialized yet
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        spriteRenderer.sprite = cardModel.Sprite;
        // Set other properties as needed
    }

    public void OnPointerClick()
    {
        cardModel.PerformEffect();
    }

    void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cardRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (Mouse.current == null) return; // safety check

        // Skip interaction logic if card is in discard pile
        if (isInDiscardPile) return;

        Vector3 mouseWorldPos = GetMousePosInWorldSpace();
        // Skip drag logic if card has been placed in a drop area
        if (isPlacedInDropArea)
        {
            // Handle clicks on placed cards
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (col.OverlapPoint(mouseWorldPos))
                {
                    // Only discard if the drop area allows it
                    if (currentDropArea != null && currentDropArea.IsDiscardable())
                    {
                        // Card was clicked while placed in a drop area
                        DiscardFromArea();
                    }
                    else
                    {
                        // Just perform effect without discarding for non-discardable areas
                        cardModel.PerformEffect();
                    }
                }
            }
            return;
        }
        
        
        // Handle hovering (when not dragging)
        if (!isDragging)
        {
            bool isMouseOver = col.OverlapPoint(mouseWorldPos);
            
            // Mouse just entered the card
            if (isMouseOver && !isHovered)
            {
                isHovered = true;
                OnHoverEnter?.Invoke(gameObject);
            }
            // Mouse just left the card
            else if (!isMouseOver && isHovered)
            {
                isHovered = false;
                OnHoverExit?.Invoke(gameObject);
            }
        }

        // Detect mouse down
        if (Mouse.current.leftButton.wasPressedThisFrame && !isAnyCardBeingDragged)
        {
            // Check if we clicked on THIS card
            if (col.OverlapPoint(mouseWorldPos))
            {
                // Check if this is the topmost card under the cursor
                Collider2D[] hitColliders = Physics2D.OverlapPointAll(mouseWorldPos);
                
                // Get all cards under the cursor
                var cardsUnderCursor = hitColliders
                    .Where(c => c.GetComponent<Card>() != null)
                    .Select(c => c.GetComponent<Card>())
                    .ToArray();
                    
                // Find the card with the highest sorting order (topmost)
                Card topmostCard = null;
                int highestOrder = -1;
                
                foreach (var cardUnderCursor in cardsUnderCursor)
                {
                    SpriteRenderer cardRenderer = cardUnderCursor.GetComponentInChildren<SpriteRenderer>();
                    if (cardRenderer != null && cardRenderer.sortingOrder > highestOrder)
                    {
                        highestOrder = cardRenderer.sortingOrder;
                        topmostCard = cardUnderCursor;
                    }
                }
                
                // Only start dragging if this is the topmost card
                if (topmostCard == this)
                {
                    startDragPos = transform.position;
                    startDragRotation = transform.rotation;
                    lastMousePos = mouseWorldPos;
                    isDragging = true;
                    isAnyCardBeingDragged = true;
                    
                    // End hover state when starting drag
                    if (isHovered)
                    {
                        isHovered = false;
                        OnHoverExit?.Invoke(gameObject);
                    }
                    
                    // Store original sorting orders before changing them
                    originalSortingOrders = new int[cardRenderers.Length];
                    for (int i = 0; i < cardRenderers.Length; i++)
                    {
                        originalSortingOrders[i] = cardRenderers[i].sortingOrder;
                        // Set sorting order to 20 to be above all UI elements
                        cardRenderers[i].sortingOrder = 20;
                    }
                    
                    // Reset rotation immediately when picked up
                    transform.rotation = Quaternion.identity;
                }
            }
        }

        // While dragging
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            transform.position = mouseWorldPos;
            
            // Calculate movement direction
            Vector3 movementDirection = mouseWorldPos - lastMousePos;
            
            // Only rotate if there's significant movement to avoid jittery rotation
            if (movementDirection.magnitude > 0.01f)
            {
                // Calculate angle based on movement direction
                float targetAngle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg - 90f;
                
                // Clamp the angle to the maximum allowed rotation
                targetAngle = Mathf.Clamp(targetAngle, -maxRotationAngle, maxRotationAngle);
                
                // Create a rotation that only affects the Z axis
                Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
                
                // Apply rotation with smoothing
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
            
            lastMousePos = mouseWorldPos;
        }

        // Mouse released while dragging
        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            isAnyCardBeingDragged = false;
            
            // Reset rotation when the card is released
            transform.rotation = Quaternion.identity;
            
            // More robust drop area detection - check for overlaps instead of a single raycast
            Collider2D[] hitColliders = Physics2D.OverlapBoxAll(transform.position, 
                                                          new Vector2(1f, 1f), 0f);
            
            bool droppedOnArea = false;
            
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider != col && hitCollider.TryGetComponent(out ICardDropArea dropArea))
                {
                    if (HandManager.Instance != null && HandManager.Instance.IsCardInHand(gameObject))
                    {
                        // Check if the area can accept this card
                        if (dropArea.CanAcceptCard(this))
                        {
                            // Perform drop actions
                            dropArea.OnCardDrop(this);
                            
                            // Remove from hand
                            HandManager.Instance.RemoveCardFromHand(gameObject);
                            
                            // Flag that this card is now placed in a drop area and shouldn't be draggable
                            isPlacedInDropArea = true;
                            droppedOnArea = true;
                            
                            // Break out of the loop once we've found a valid drop area
                            break;
                        }
                    }
                }
            }
            
            // Restore original sorting orders if returning to hand
            if (!droppedOnArea && HandManager.Instance != null && HandManager.Instance.IsCardInHand(gameObject))
            {
                // Restore the original sorting orders
                for (int i = 0; i < cardRenderers.Length; i++)
                {
                    if (i < originalSortingOrders.Length)
                    {
                        cardRenderers[i].sortingOrder = originalSortingOrders[i];
                    }
                }
                
                // The hand manager will handle moving it back to the right position
                HandManager.Instance.UpdateCardPositions();
            }
        }
    }
    
    private Vector3 GetMousePosInWorldSpace()
    {
        // Get the mouse position in screen space
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // Convert to world space
        return Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z));
    }
    
    // Add this new method to handle discarding
    private void DiscardFromArea()
    {
        // Trigger card effect before discarding
        cardModel.PerformEffect();
        
        // Find the discard pile
        DiscardArea discardArea = FindFirstObjectByType<DiscardArea>();
        if (discardArea == null)
        {
            Debug.LogWarning("Discard pile not found!");
            return;
        }
        
        // Free up the drop area
        if (currentDropArea != null && currentDropArea is MonoBehaviour dropAreaMono)
        {
            // Access the specific drop area implementation to reset its state
            if (dropAreaMono is LeaderCardDropArea leaderArea)
            {
                leaderArea.RemoveCard();
            }
            else if (dropAreaMono is CharacterCardDropArea characterArea)
            {
                characterArea.RemoveCard();
            }
        }
        
        // Get the discard pile position
        Transform discardPileTransform = discardArea.GetDiscardPileTransform();
        Vector3 targetPos = discardPileTransform != null ? 
                            discardPileTransform.position : 
                            discardArea.transform.position;
                            
        // Add slight offset for stacking effect
        targetPos += new Vector3(
            Random.Range(-0.05f, 0.05f), 
            Random.Range(-0.05f, 0.05f), 
            0f);
            
        // Animate the card moving to the discard pile
        transform.DOMove(targetPos, discardAnimationDuration)
                 .SetEase(Ease.OutQuad)
                 .OnComplete(() => {
                     // Add the card to the discard pile
                     discardArea.AddDiscardedCard(this);
                 });
    }
    
    // This setter should be called by drop areas when the card is dropped
    public void SetCurrentDropArea(ICardDropArea area)
    {
        currentDropArea = area;
    }

    // Add this method to mark the card as in discard pile
    public void MarkAsDiscarded()
    {
        isInDiscardPile = true;
        isPlacedInDropArea = true; // Also mark as placed so it can't be dragged
    }
}
