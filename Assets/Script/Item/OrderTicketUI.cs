using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
// 如果你使用 TextMeshPro，請取消註解下一行並將 Text 改為 TextMeshProUGUI
// using TMPro; 

public class OrderTicketUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("倒數計時條 (Image Type 設為 Filled)")]
    public Image timerFillImage;

    [Tooltip("設定四個固定的食材顯示槽位")]
    public OrderIngredientSlot[] ingredientSlots;

    private Order _currentOrder;

    // 初始化訂單 UI
    public void Setup(Order order)
    {
        _currentOrder = order;
        UpdateVisuals();
    }

    // 每幀由 Manager 呼叫更新時間
    public void UpdateTimerUI()
    {
        if (_currentOrder == null) return;

        float percent = _currentOrder.timeRemaining / _currentOrder.totalDuration;
        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = percent;

            // 可選：時間快到時變紅色
            if (percent < 0.3f) timerFillImage.color = Color.red;
            else timerFillImage.color = Color.green;
        }
    }

    private void UpdateVisuals()
    {
        if (_currentOrder == null) return;

        // 統計訂單內每種食材的數量
        // Key: ItemData, Value: 數量
        Dictionary<ItemData, int> requirementCounts = new Dictionary<ItemData, int>();
        foreach (var item in _currentOrder.requirements)
        {
            if (requirementCounts.ContainsKey(item))
                requirementCounts[item]++;
            else
                requirementCounts[item] = 1;
        }

        // 遍歷預設的四個固定槽位，更新顯示
        foreach (var slot in ingredientSlots)
        {
            if (slot.targetItemData == null) continue;

            // 設定圖示 (從 ItemData 讀取)
            if (slot.iconImage != null) slot.iconImage.sprite = slot.targetItemData.icon;

            // 檢查訂單是否包含此食材
            if (requirementCounts.ContainsKey(slot.targetItemData))
            {
                int count = requirementCounts[slot.targetItemData];

                // 1. 設定不透明 (Alpha = 1)
                SetImageAlpha(slot.iconImage, 1f);

                // 2. 顯示數量
                if (slot.quantityText != null)
                {
                    slot.quantityText.gameObject.SetActive(true);
                    slot.quantityText.text = count.ToString();
                }
            }
            else
            {
                // 訂單沒有此食材

                // 1. 設定半透明 (Alpha = 0.3)
                SetImageAlpha(slot.iconImage, 0.3f);

                // 2. 隱藏數量
                if (slot.quantityText != null)
                {
                    slot.quantityText.gameObject.SetActive(false);
                }
            }
        }
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}

[System.Serializable]
public class OrderIngredientSlot
{
    [Tooltip("此槽位對應的食材資料 (例如: 這格固定顯示肉排)")]
    public ItemData targetItemData;

    [Tooltip("顯示圖示的 Image")]
    public Image iconImage;

    [Tooltip("顯示數量的 Text (或 TextMeshProUGUI)")]
    public Text quantityText;
}