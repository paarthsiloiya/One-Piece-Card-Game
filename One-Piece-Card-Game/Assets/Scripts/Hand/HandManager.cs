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
    [SerializeField] private Transform cardSpawnPoint;
    [SerializeField] private float minCardSpacing = 0.05f; // Minimum spacing between cards
    [SerializeField] private float maxCardSpacing = 0.15f; // Maximum spacing between cards
    [SerializeField] private float handSpread = 0.8f; // How much of the spline to use (0.8 = 80%)

    private List<GameObject> handCards = new List<GameObject>();

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

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
        {
            DrawCard();
        }
    }

    private void UpdateCardPositions()
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
            handCards[i].transform.DOMove(position, 0.25f);
            handCards[i].transform.DOLocalRotateQuaternion(rotation, 0.25f);
        }
    }

    private void DrawCard()
    {
        if (handCards.Count >= maxHandSize)
        {
            Debug.Log("Hand is full!");
            return;
        }
        GameObject newCard = Instantiate(cardPrefab, cardSpawnPoint.position, Quaternion.identity);
        handCards.Add(newCard);
        UpdateCardPositions();
    }

    public void RemoveCardFromHand(GameObject card)
    {
        if (handCards.Contains(card))
        {
            handCards.Remove(card);
            UpdateCardPositions();
        }
    }

    public bool IsCardInHand(GameObject card)
    {
        return handCards.Contains(card);
    }
}
