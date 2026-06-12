using UnityEngine;

public enum ItemCategory
{
    Prop,
    Ingredient,
    Food,
    KitchenTool,
    IngredientBox,
    PackBox
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Overcooked-Like/Item Data")]
public class ItemData : ScriptableObject
{
    public ItemCategory category;
    public float durability = 100f;
    public bool canDashWhileHolding = true;
    public bool canBePickedUp = true;

    [Header("UI Settings")]
    [Tooltip("UI 顯示用的圖示")]
    public Sprite icon; // (New) 新增圖示

    [Header("Economy (經濟設定)")]
    public int price = 10;

    [Header("Spawning Settings")]
    public GameObject itemPrefab;

    [Header("Assembly Settings")]
    public bool isProcessed = false;
    public int assemblySortOrder;
    public GameObject visualPrefab;
    public GameObject topBunVisual;

    [Header("Transform Settings")]
    public Vector3 heldRotation = Vector3.zero;
    public Vector3 placedRotation = Vector3.zero;
    public Vector3 inToolRotation = Vector3.zero;

    [Header("Packaging Settings")]
    public ItemData packedBoxData;
    public bool requireBunToPack = false;
}