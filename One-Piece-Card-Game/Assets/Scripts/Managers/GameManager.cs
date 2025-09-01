using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<CharacterCardSO> cardModels = new List<CharacterCardSO>();
    [SerializeField] private Card cardPrefab;
    [SerializeField] private List<CardModel> playerDeck = new List<CardModel>();
    public HandManager handManager;

    void Start()
    {
        // Make sure we have card models to work with
        if (cardModels == null || cardModels.Count == 0)
        {
            Debug.LogError("No card models assigned to GameManager! Please add CardSO objects in the Inspector.");
            return;
        }

        // Initialize the deck with random cards
        for (int i = 0; i < 20; i++)
        {
            CharacterCardSO cardData = cardModels[Random.Range(0, cardModels.Count)];
            CardModel card = new(cardData);
            playerDeck.Add(card);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
        {
            DrawCardFromDeck();
        }
    }
    
    public void DrawCardFromDeck()
    {
        if (playerDeck.Count == 0)
        {
            Debug.Log("Deck is empty!");
            return;
        }
        
        // Get a random card from the deck
        CardModel cardModel = playerDeck[Random.Range(0, playerDeck.Count)];
        playerDeck.Remove(cardModel);
        
        // Create a new card GameObject
        Card newCard = Instantiate(cardPrefab, handManager.cardSpawnPoint.position, Quaternion.identity);
        
        // Set up the card with its data
        newCard.Setup(cardModel);
        
        // Add the card to the player's hand
        handManager.DrawCard(newCard.gameObject);
        
        Debug.Log($"Drew card: {cardModel.Title} from deck. Remaining cards: {playerDeck.Count}");
    }
}
