using UnityEngine;

public class CheckoutCounter : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [Tooltip("此櫃台歸屬的隊伍 ID (0 = P1/藍隊, 1 = P2/紅隊)")]
    public int teamID = 0;

    public void OnInteract(PlayerStateMachine player, ItemData heldItemData)
    {
        // 1. 基本檢查
        if (!player.IsHoldingItem) return;

        // [Mod] 移除隊伍權限檢查，允許搶單
        // if (player.TeamID != this.teamID) { ... } 

        // 2. 結帳邏輯
        GameObject heldObj = player.HeldItem;
        if (heldObj.TryGetComponent<PackBox>(out PackBox box))
        {
            // 呼叫管理器結帳
            // 參數: (物品內容, 送餐者ID, 櫃台歸屬ID)
            // 透過比對 送餐者ID 與 訂單歸屬ID 來決定是否為搶單 (這部分邏輯在 OrderManager 處理)
            // 這裡我們只需要傳入 "是誰送的" 即可，OrderManager 會去查這張單是誰的

            bool success = OrderManager.Instance.TryDeliveryOrder(box.GetContents(), player.TeamID);

            if (success)
            {
                player.DestroyHeldItem();
                Debug.Log($"玩家 P{player.TeamID + 1} 結帳成功！");
                // TODO: 播放收銀機音效
            }
            else
            {
                Debug.Log("結帳失敗：沒有匹配的訂單");
                // TODO: 播放錯誤音效
            }
        }
    }

    public void OnPickup(PlayerStateMachine player) { }
}