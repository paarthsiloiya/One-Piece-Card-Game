using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }
    
    [SerializeField] private int maxHandSize = 45;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private SplineContainer handSplineContainer;
    [SerializeField] public Transform cardSpawnPoint;
    [SerializeField] private float minCardSpacing = 0.05f; // Minimum spacing between cards
    [SerializeField] private float maxCardSpacing = 0.15f; // Maximum spacing between cards
    [SerializeField] private float handSpread = 0.8f; // How much of the spline to use (0.8 = 80%)

    // Hover effect parameters
    [Header("Hover Effects")]
    [SerializeField] private float hoverPopDistance = 0.3f; // Distance the card pops out when hovered
    [SerializeField] private float hoverPopDuration = 0.15f; // How fast the card pops out
    [SerializeField] private float hoverScale = 1.1f; // Scale multiplier when hovered

    private List<GameObject> handCards = new List<GameObject>();
    private Dictionary<GameObject, Vector3> cardOriginalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> cardOriginalRotations = new Dictionary<GameObject, Quaternion>();
    private GameObject currentHoveredCard = null;

    // Add a new field to track the current highest sort order:
    private int currentHighestSortOrder = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;

        // Calculate dynamic card spacing based on number of cards
        float idealSpacing = (handCards.Count == 1) ? 0f : handSpread / (handCards.Count - 1);
        float cardSpacing = Mathf.Clamp(idealSpacing, minCardSpacing, maxCardSpacing);
        
        // Calculate the total width needed for all cards
        float totalWidth = (handCards.Count - 1) * cardSpacing;
        
        // Center the cards on the spline
        float startPosition = 0.5f - totalWidth / 2f;
        
        Spline spline = handSplineContainer.Spline;
        for (int i = 0; i < handCards.Count; i++)
        {
            float t = startPosition + i * cardSpacing;
            // Clamp t to ensure we stay within spline bounds
            t = Mathf.Clamp01(t);
            
            Vector3 position = spline.EvaluatePosition(t);
            Vector3 forward = spline.EvaluateTangent(t);
            Vector3 up = spline.EvaluateUpVector(t);
            Quaternion rotation = Quaternion.LookRotation(up, Vector3.Cross(up, forward));

            // Don't animate the currently hovered card
            if (handCards[i] != currentHoveredCard)
            {
                handCards[i].transform.DOMove(position, 0.25f);
                handCards[i].transform.DOLocalRotateQuaternion(rotation, 0.25f);
                
                // Store original position and rotation for hover effects
                cardOriginalPositions[handCards[i]] = position;
                cardOriginalRotations[handCards[i]] = rotation;
            }
            else
            {
                // Update the stored original position and rotation
                cardOriginalPositions[handCards[i]] = position;
                cardOriginalRotations[handCards[i]] = rotation;
            }
        }
    }

    public void DrawCard(GameObject card)
    {
        if (handCards.Count >= maxHandSize)
        {
            Debug.Log("Hand is full!");
            Destroy(card);
            return;
        }
        
        handCards.Add(card);
        
        // Set the sorting order to ensure the newest card appears on top
        SpriteRenderer[] cardRenderers = card.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in cardRenderers)
        {
            renderer.sortingOrder = currentHighestSortOrder;
        }
        currentHighestSortOrder += 2; // Increment by 2 to leave room for effects
        
        // Register card events for hover effect
        Card cardComponent = card.GetComponent<Card>();
        if (cardComponent != null)
        {
            cardComponent.OnHoverEnter += OnCardHoverEnter;
            cardComponent.OnHoverExit += OnCardHoverExit;
        }
        
        UpdateCardPositions();
    }

    public void RemoveCardFromHand(GameObject card)
    {
        if (handCards.Contains(card))
        {
            // Unregister hover events
            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.OnHoverEnter -= OnCardHoverEnter;
                cardComponent.OnHoverExit -= OnCardHoverExit;
            }
            
            handCards.Remove(card);
            
            // Remove from dictionaries
            if (cardOriginalPositions.ContainsKey(card))
                cardOriginalPositions.Remove(card);
            
            if (cardOriginalRotations.ContainsKey(card))
                cardOriginalRotations.Remove(card);
            
            if (currentHoveredCard == card)
                currentHoveredCard = null;
                
            UpdateCardPositions();
        }
    }

    public bool IsCardInHand(GameObject card)
    {
        return handCards.Contains(card);
    }
    
    // Hover effect methods
    private void OnCardHoverEnter(GameObject card)
    {
        // Only apply hover effect if card is in hand
        if (!IsCardInHand(card) || isDraggingAnyCard()) return;
        
        // If we had another card hovered, make sure to exit that one first
        if (currentHoveredCard != null && currentHoveredCard != card)
        {
            OnCardHoverExit(currentHoveredCard);
        }
        
        currentHoveredCard = card;
        
        // Make sure we have the original position and rotation
        if (!cardOriginalPositions.ContainsKey(card) || !cardOriginalRotations.ContainsKey(card))
            return;
            
        Vector3 position = cardOriginalPositions[card];
        Quaternion rotation = cardOriginalRotations[card];
        
        // Calculate pop-out position
        Vector3 direction = (rotation * Vector3.forward).normalized;
        Vector3 popPosition = position + direction * hoverPopDistance;
        
        // Boost sorting order to show on top when hovered
        SpriteRenderer[] cardRenderers = card.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in cardRenderers)
        {
            renderer.sortingOrder += 100; // Temporary boost to be on top
        }
        
        // Animate card popping out
        card.transform.DOKill(); // Stop any ongoing animations
        card.transform.DOMove(popPosition, hoverPopDuration);
        card.transform.DOScale(Vector3.one * hoverScale, hoverPopDuration);
    }
    
    private void OnCardHoverExit(GameObject card)
    {
        // Only process hover exit if this is the currently hovered card
        if (card != currentHoveredCard) return;
        
        currentHoveredCard = null;
        
        // Make sure we have the original position and rotation
        if (!cardOriginalPositions.ContainsKey(card) || !cardOriginalRotations.ContainsKey(card))
            return;
            
        Vector3 position = cardOriginalPositions[card];
        Quaternion rotation = cardOriginalRotations[card];
        
        // Restore original sorting order
        int cardIndex = handCards.IndexOf(card);
        SpriteRenderer[] cardRenderers = card.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in cardRenderers)
        {
            renderer.sortingOrder = cardIndex * 2; // Restore to normal layering
        }
        
        // Animate card returning to original position
        card.transform.DOKill(); // Stop any ongoing animations
        card.transform.DOMove(position, hoverPopDuration);
        card.transform.DOScale(Vector3.one, hoverPopDuration);
        card.transform.DOLocalRotateQuaternion(rotation, hoverPopDuration);
    }
    
    // Helper method to check if any card is being dragged
    private bool isDraggingAnyCard()
    {
        foreach (var card in handCards)
        {
            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent != null && cardComponent.IsDragging)
                return true;
        }
        return false;
    }
}
