using UnityEngine;

public class LeaderCardDropArea : MonoBehaviour, ICardDropArea
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnCardDrop(Card card)
    {
        card.transform.position = transform.position;
    }
}
