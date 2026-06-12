using UnityEngine;

[CreateAssetMenu(fileName = "NewKitchenToolData", menuName = "Overcooked-Like/Kitchen Tool Data")]
public class KitchenToolData : ItemData
{
    [Range(1, 5)]
    public int maxIngredientCount = 3;
}