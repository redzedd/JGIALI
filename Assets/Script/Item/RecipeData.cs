using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Overcooked-Like/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("Recipe Rules")]
    [Tooltip("必須使用的廚具 (例如：平底鍋)。若為 null 代表直接在檯面上操作")]
    public KitchenToolData requiredTool;

    [Tooltip("必須在哪種檯面上 (例如：爐火、砧板)")]
    public StationType requiredStation;

    [Header("Ingredients")]
    [Tooltip("所需的所有食材清單 (例如：馬鈴薯 + 起司)")]
    public List<ItemData> inputIngredients; // (Fix Issue 3) 改為 List

    [Header("Process")]
    [Tooltip("處理方式")]
    public CookingProcessType processType;

    [Tooltip("所需時間 (秒)")]
    public float processDuration = 3f;

    [Header("Result")]
    [Tooltip("產出的物品 (例如：焗烤馬鈴薯)")]
    public ItemData outputItem;
}