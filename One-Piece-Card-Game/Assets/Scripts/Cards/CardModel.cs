using UnityEngine;

public class CardModel
{
    private readonly CharacterCardSO cardData;

    public CardModel(CharacterCardSO cardData)
    {
        this.cardData = cardData;
        Effects = cardData.Effects;
        Cost = cardData.Cost;
    }

    public Sprite Sprite { get => cardData.Sprite; }
    public string Title { get => cardData.CardName; }

    public int Cost { get; set; }
    public int Power { get; set; }
    public int Attribute { get; set; }
    public int Counter { get; set; }
    public int Color { get; set; }
    public string Type { get; set; }
    public string Effects { get; set; }

    public void PerformEffect()
    {
        // Implement the effect logic here
        Debug.Log($"Performing effect: {Effects}, Paid cost: {Cost}");
    }
}
