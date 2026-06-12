using System.Collections.Generic;
using UnityEngine;

public class PackagingStation : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [Tooltip("生成的 FoodGroup Prefab (用於拆包還原)")]
    public GameObject foodGroupPrefab;

    public void OnInteract(PlayerStateMachine player, ItemData heldItemData)
    {
        // 必須手持物品才能互動
        if (heldItemData == null || !player.IsHoldingItem) return;

        GameObject heldObj = player.HeldItem;

        // 情境 1: 玩家拿著食物 (FoodGroup) -> 嘗試打包
        if (heldObj.TryGetComponent<FoodGroup>(out FoodGroup foodGroup))
        {
            TryPackFood(player, foodGroup, heldItemData);
        }
        // 情境 2: 玩家拿著盒子 (PackBox) -> 嘗試拆包 (變回食物)
        else if (heldObj.TryGetComponent<PackBox>(out PackBox packBox))
        {
            TryUnpackBox(player, packBox);
        }
    }

    public void OnPickup(PlayerStateMachine player)
    {
        // 打包區通常是固定設施，不可移動
    }

    private void TryPackFood(PlayerStateMachine player, FoodGroup foodGroup, ItemData groupData)
    {
        // 1. 檢查是否有打包設定
        ItemData configData = groupData;

        // 2. 檢查是否允許打包
        if (configData.packedBoxData == null)
        {
            Debug.Log("此物品無法打包 (未設定 packedBoxData)");
            return;
        }

        // 3. 檢查麵包限制
        if (configData.requireBunToPack)
        {
            bool hasBun = false;
            foreach (var item in foodGroup.ingredients)
            {
                if (item.assemblySortOrder == 0)
                {
                    hasBun = true;
                    break;
                }
            }

            if (!hasBun)
            {
                Debug.Log("無法打包：缺少麵包底!");
                return;
            }
        }

        // 4. 執行打包
        List<ItemData> contents = new List<ItemData>(foodGroup.ingredients);
        ItemData boxData = configData.packedBoxData;

        player.DestroyHeldItem();

        if (boxData.itemPrefab != null)
        {
            GameObject boxObj = Instantiate(boxData.itemPrefab, Vector3.zero, Quaternion.identity);

            if (boxObj.TryGetComponent<PackBox>(out PackBox newBox))
            {
                newBox.PackFood(contents, groupData);
            }

            player.PickupItem(boxObj, boxData);
            Debug.Log("打包完成!");
        }
    }

    private void TryUnpackBox(PlayerStateMachine player, PackBox packBox)
    {
        // 1. 讀取資料
        List<ItemData> contents = packBox.GetContents();
        ItemData originalData = packBox.GetOriginalGroupData();

        if (contents == null || contents.Count == 0) return;

        // 2. 銷毀手上的盒子
        player.DestroyHeldItem();

        // 3. 還原 FoodGroup
        if (foodGroupPrefab != null)
        {
            // 強制歸零生成
            GameObject newGroupObj = Instantiate(foodGroupPrefab, Vector3.zero, Quaternion.identity);
            newGroupObj.transform.localRotation = Quaternion.identity;

            // 初始化 FoodGroup
            if (newGroupObj.TryGetComponent<FoodGroup>(out FoodGroup newGroup))
            {
                // [Fix Issue] 修正旋轉問題
                // 不要將 originalData 還原給 groupItemData，除非你確定這是一個專屬的 Burger ItemData
                // 如果 originalData 只是麵包 (Bun) 的數據，指派給 groupItemData 會導致 FoodGroup 繼承麵包的旋轉 (-90)
                // 保持 groupItemData 為 null，PlayerStateMachine 就會強制歸零旋轉

                // newGroup.groupItemData = originalData; // <--- 移除這行

                // 加回所有食材
                foreach (var item in contents)
                {
                    newGroup.AddIngredient(item);
                }
            }

            // 讓玩家拿著還原的食物
            // 我們依然傳入 originalData，這樣下一次打包時，系統才知道要讀取哪個 ItemData (例如麵包) 的打包設定
            // PlayerStateMachine 的 PickupItem 會因為 newGroup.groupItemData 為 null 而忽略 originalData 的旋轉設定
            player.PickupItem(newGroupObj, originalData);
            Debug.Log("拆包完成!");
        }
    }
}