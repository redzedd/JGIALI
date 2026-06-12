using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_01_Config", menuName = "Overcooked-Like/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Level Settings")]
    public string levelName;

    [Header("Allowed Recipes")]
    public List<RecipeData> availableRecipes;

    /// <summary>
    /// 嚴格驗證：檢查當前狀況是否符合食譜（包含 Station）
    /// </summary>
    public RecipeData ValidateRecipe(ItemData proposedIngredient, List<ItemData> currentContent, KitchenToolData tool, StationType station)
    {
        // 複用下方邏輯，但強制檢查 Station
        return FindMatchingRecipe(proposedIngredient, currentContent, tool, station, true);
    }

    /// <summary>
    /// 寬鬆驗證：檢查食材是否能放入工具（忽略 Station）
    /// 用於：把肉放入平底鍋（即使平底鍋還沒放在爐子上）
    /// </summary>
    public bool IsValidIngredientForTool(ItemData proposedIngredient, List<ItemData> currentContent, KitchenToolData tool)
    {
        // 只要找到任何一個食譜允許這個組合（不管需要什麼檯面），就回傳 true
        RecipeData match = FindMatchingRecipe(proposedIngredient, currentContent, tool, StationType.None, false);
        return match != null;
    }

    /// <summary>
    /// 核心搜尋邏輯
    /// </summary>
    private RecipeData FindMatchingRecipe(ItemData proposedIngredient, List<ItemData> currentContent, KitchenToolData tool, StationType station, bool checkStation)
    {
        if (availableRecipes == null) return null;

        List<ItemData> potentialContent = new List<ItemData>(currentContent);
        if (proposedIngredient != null)
        {
            potentialContent.Add(proposedIngredient);
        }

        foreach (var recipe in availableRecipes)
        {
            // 1. 工具檢查
            if (recipe.requiredTool != tool) continue;

            // 2. 檯面檢查 (根據參數決定是否忽略)
            if (checkStation)
            {
                bool stationMatch = (recipe.requiredStation == StationType.None || recipe.requiredStation == station);
                if (!stationMatch) continue;
            }

            // 3. 食材集合檢查 (是否為子集)
            List<ItemData> remainingRequirements = new List<ItemData>(recipe.inputIngredients);
            bool isSubset = true;

            foreach (var item in potentialContent)
            {
                if (remainingRequirements.Contains(item))
                {
                    remainingRequirements.Remove(item);
                }
                else
                {
                    isSubset = false;
                    break;
                }
            }

            if (isSubset) return recipe;
        }

        return null;
    }

    public bool IsRecipeReadyToCook(RecipeData recipe, List<ItemData> currentContent)
    {
        if (recipe == null || currentContent == null) return false;
        if (recipe.inputIngredients.Count != currentContent.Count) return false;

        List<ItemData> remaining = new List<ItemData>(recipe.inputIngredients);
        foreach (var item in currentContent)
        {
            if (remaining.Contains(item)) remaining.Remove(item);
            else return false;
        }
        return remaining.Count == 0;
    }
}