using System.Collections.Generic;
using UnityEngine;

public class DiscardArea : MonoBehaviour, ICardDropArea
{
    [SerializeField] private Transform discardPile; // Reference to discard pile position
    
    // List to keep track of discarded cards
    private List<Card> discardedCards = new List<Card>();

    public void OnCardDrop(Card card)
    {
        // Position the card in the discard pile
        Vector3 targetPos = discardPile != null ? discardPile.position : transform.position;
        
        // Add slight offset for stacking effect
        targetPos += new Vector3(
            Random.Range(-0.05f, 0.05f), 
            Random.Range(-0.05f, 0.05f), 
            0f);
        
        // Reset rotation and move to position
        card.transform.rotation = Quaternion.identity;
        card.transform.position = targetPos;
        
        // Ensure the card is visible above the drop area
        SpriteRenderer[] cardRenderers = card.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in cardRenderers)
        {
            renderer.sortingOrder = 5 + discardedCards.Count; // Stack cards visually
        }
        
        // Add to discard pile
        discardedCards.Add(card);
        
        // Mark the card as discarded
        card.MarkAsDiscarded();
    }
    
    public bool CanAcceptCard(Card card)
    {
        // Discard area can always accept cards
        return true;
    }
    
    public bool IsDiscardable()
    {
        // Cards in the discard pile are not discardable
        return false;
    }
    
    // Add a method to get the discard pile transform
    public Transform GetDiscardPileTransform()
    {
        return discardPile != null ? discardPile : transform;
    }
    
    // Method to directly add a card to the discard pile
    public void AddDiscardedCard(Card card)
    {
        if (!discardedCards.Contains(card))
        {
            discardedCards.Add(card);
            
            // Update sorting order
            SpriteRenderer[] cardRenderers = card.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in cardRenderers)
            {
                renderer.sortingOrder = 5 + discardedCards.Count;
            }
            
            // Mark the card as discarded
            card.MarkAsDiscarded();
        }
    }
    
    // Get all discarded cards
    public IReadOnlyList<Card> GetDiscardedCards()
    {
        return discardedCards.AsReadOnly();
    }
}