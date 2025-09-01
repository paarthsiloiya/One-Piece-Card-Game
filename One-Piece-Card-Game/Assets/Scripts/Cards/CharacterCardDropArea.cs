using UnityEngine;

public class CharacterCardDropArea : MonoBehaviour, ICardDropArea
{
    [SerializeField] private Transform cardAnchor; // Optional anchor point
    
    // Track if the area already has a card
    private bool isOccupied = false;
    private Card occupyingCard = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnCardDrop(Card card)
    {
        // Only proceed if area can accept the card
        if (!CanAcceptCard(card))
        {
            // Return card to hand if it can't be placed here
            if (HandManager.Instance != null && HandManager.Instance.IsCardInHand(card.gameObject))
            {
                HandManager.Instance.UpdateCardPositions();
            }
            return;
        }
        
        // Position the card properly
        Vector3 targetPos = cardAnchor != null ? cardAnchor.position : transform.position;
        
        // Reset rotation and move to position
        card.transform.rotation = Quaternion.identity;
        card.transform.position = targetPos;
        
        // Ensure the card is visible above the drop area
        SpriteRenderer[] cardRenderers = card.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in cardRenderers)
        {
            renderer.sortingOrder = 10; // Above the drop area but below UI
        }
        
        // Mark this area as occupied
        isOccupied = true;
        occupyingCard = card;
        
        // Tell the card which area it's in
        card.SetCurrentDropArea(this);
    }
    
    public bool CanAcceptCard(Card card)
    {
        // Can only accept a card if not already occupied
        return !isOccupied;
    }
    
    // Add a method to remove the card from this area
    public void RemoveCard()
    {
        isOccupied = false;
        occupyingCard = null;
    }

    public bool IsDiscardable()
    {
        // Character cards can be discarded
        return true;
    }
}
