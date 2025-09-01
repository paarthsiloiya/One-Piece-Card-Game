using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCardSO", menuName = "Scriptable Objects/Character Card")]
public class CharacterCardSO : ScriptableObject
{
    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public int Cost { get; private set; }
    [field: SerializeField] public int Power { get; private set; }
    [field: SerializeField] public int Attribute { get; private set; }
    [field: SerializeField] public int Counter { get; private set; }
    [field: SerializeField] public int Color { get; private set; }
    [field: SerializeField] public string CardName { get; private set; }
    [field: SerializeField] public string Type { get; private set; }
    [field: SerializeField] public string Effects { get; private set; }
}
