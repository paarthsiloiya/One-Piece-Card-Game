using UnityEngine;

public interface ICardDropArea
{
    void OnCardDrop(Card card);
    
    // Check if area can accept a card
    bool CanAcceptCard(Card card);
    
    // Check if cards in this area can be discarded
    bool IsDiscardable();
}
