using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Order
{
    public int orderID;
    public List<ItemData> requirements = new List<ItemData>();

    public float totalDuration;
    public float timeRemaining;

    public int totalValue; // 訂單總金額

    public Order(int id, List<ItemData> items, float durationMultiplier)
    {
        orderID = id;
        requirements = new List<ItemData>(items);

        // 計算總價值
        totalValue = 0;
        foreach (var item in items)
        {
            totalValue += item.price;
        }

        // 計算時間 (基礎時間 + 每個食材增加的時間)
        // 這裡設基礎時間 15秒，每個食材加 5 秒
        totalDuration = 15f + (items.Count * 5f);
        timeRemaining = totalDuration;
    }
}