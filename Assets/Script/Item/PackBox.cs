using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PackBox : MonoBehaviour, IInteractable
{
    public ItemData data;

    // 儲存原本漢堡的內容物
    [SerializeField]
    private List<ItemData> _packedContents = new List<ItemData>();

    // 儲存原本漢堡的 ItemData (以便還原 FoodGroup 的設定)
    private ItemData _originalFoodGroupData;

    // 用於初始化盒子內容
    public void PackFood(List<ItemData> contents, ItemData foodGroupData)
    {
        _packedContents = new List<ItemData>(contents);
        _originalFoodGroupData = foodGroupData;
    }

    public List<ItemData> GetContents()
    {
        return _packedContents;
    }

    public ItemData GetOriginalGroupData()
    {
        return _originalFoodGroupData;
    }

    // 實作 IInteractable
    public void OnInteract(PlayerStateMachine player, ItemData heldItemData)
    {
        // 盒子通常只能被拿起來，不能再往裡面塞東西 (除非你有特殊需求)
        // 可以在這裡加邏輯：如果拿著標籤紙貼上去...等
    }

    public void OnPickup(PlayerStateMachine player)
    {
        if (!player.IsHoldingItem)
        {
            // 通知檯面清除佔用 (如果放在桌上)
            // 這裡為了簡化，你可以讓 PackBox 繼承 PickupableItem，或手動實作類似邏輯
            if (TryGetComponent<PickupableItem>(out var pickup))
            {
                // 如果你有掛 PickupableItem，讓它處理
                pickup.OnPickup(player);
            }
            else
            {
                // 否則手動處理
                // 注意：這裡如果放在 Countertop 上，需要 Countertop 的引用
                // 建議 PackBox 直接掛載 PickupableItem 腳本來處理通用撿取邏輯
                player.PickupItem(this.gameObject, this.data);
            }
        }
    }
}