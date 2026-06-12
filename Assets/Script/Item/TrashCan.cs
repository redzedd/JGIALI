using UnityEngine;

public class TrashCan : MonoBehaviour, IInteractable
{
    [Tooltip("垃圾桶蓋子動畫或特效可以加在這裡")]
    public Transform trashLid;

    public void OnInteract(PlayerStateMachine player, ItemData heldItemData)
    {
        // 1. 玩家手上拿著東西
        if (player.IsHoldingItem)
        {
            // 情況 A: 拿著的是食材或食物 -> 直接銷毀
            if (heldItemData.category == ItemCategory.Ingredient || heldItemData.category == ItemCategory.Food)
            {
                player.DestroyHeldItem();
                Debug.Log("垃圾桶: 已銷毀食材");
                // TODO: 播放丟垃圾音效
            }
            // 情況 B: 拿著的是廚具 (如鍋子) -> 清空廚具內容物
            else if (heldItemData.category == ItemCategory.KitchenTool)
            {
                if (player.HeldItem.TryGetComponent<KitchenTool>(out KitchenTool tool))
                {
                    if (tool.HasContents())
                    {
                        tool.ClearContents();
                        Debug.Log("垃圾桶: 已清空廚具");
                        // TODO: 播放倒掉音效
                    }
                }
            }
        }
    }

    public void OnPickup(PlayerStateMachine player)
    {
        // 垃圾桶通常不能撿起，或視設計而定
    }
}