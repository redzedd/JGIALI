using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Team 1 UI (P1)")]
    public Transform team1OrdersContainer;
    public Transform team1PendingContainer;
    public Text team1MoneyText;
    public Transform team1FloatTextPoint; // [New] P1 浮動文字生成點

    [Header("Team 2 UI (P2)")]
    public Transform team2OrdersContainer;
    public Transform team2PendingContainer;
    public Text team2MoneyText;
    public Transform team2FloatTextPoint; // [New] P2 浮動文字生成點

    [Header("Active Order Prefabs")]
    public GameObject team1OrderPrefab;
    public GameObject team2OrderPrefab;

    [Header("Pending Order Prefabs")]
    public GameObject team1PendingOrderPrefab;
    public GameObject team2PendingOrderPrefab;

    [Header("Feedback UI")]
    public GameObject floatingTextPrefab; // [New] 浮動文字 Prefab

    private Dictionary<int, OrderTicketUI> _activeOrderUIs = new Dictionary<int, OrderTicketUI>();
    private List<GameObject> _team1PendingObjects = new List<GameObject>();
    private List<GameObject> _team2PendingObjects = new List<GameObject>();

    // [New] 記錄當前顯示的浮動文字實例，用於 "只顯示最近一次" (如果有舊的就刪除)
    private GameObject _currentP1FloatText;
    private GameObject _currentP2FloatText;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // ... (AddOrderUI, RemoveOrderUI, UpdateAllOrderTimers, UpdatePendingOrdersUI 保持不變) ...
    // 請將原有的這些方法完整保留，為了節省篇幅此處省略，但請不要刪除

    public void AddOrderUI(Order order, int teamID)
    {
        Transform container = (teamID == 0) ? team1OrdersContainer : team2OrdersContainer;
        GameObject prefabToUse = (teamID == 0) ? team1OrderPrefab : team2OrderPrefab;
        if (container == null || prefabToUse == null) return;
        GameObject uiObj = Instantiate(prefabToUse, container);
        OrderTicketUI ticket = uiObj.GetComponent<OrderTicketUI>();
        if (ticket != null) { ticket.Setup(order); _activeOrderUIs.Add(order.orderID, ticket); }
    }

    public void RemoveOrderUI(int orderID)
    {
        if (_activeOrderUIs.ContainsKey(orderID)) { Destroy(_activeOrderUIs[orderID].gameObject); _activeOrderUIs.Remove(orderID); }
    }

    public void UpdateAllOrderTimers()
    {
        foreach (var ticket in _activeOrderUIs.Values) ticket.UpdateTimerUI();
    }

    public void UpdatePendingOrdersUI(List<Order> pendingOrders, int teamID)
    {
        Transform container = (teamID == 0) ? team1PendingContainer : team2PendingContainer;
        List<GameObject> trackerList = (teamID == 0) ? _team1PendingObjects : _team2PendingObjects;
        GameObject prefabToUse = (teamID == 0) ? team1PendingOrderPrefab : team2PendingOrderPrefab;
        if (container == null || prefabToUse == null) return;
        foreach (var obj in trackerList) Destroy(obj);
        trackerList.Clear();
        foreach (var order in pendingOrders)
        {
            GameObject uiObj = Instantiate(prefabToUse, container);
            OrderTicketUI ticket = uiObj.GetComponent<OrderTicketUI>();
            if (ticket != null) { ticket.Setup(order); if (ticket.timerFillImage != null) ticket.timerFillImage.fillAmount = 1f; }
            trackerList.Add(uiObj);
        }
    }

    public void UpdateMoneyUI(int amount, int teamID)
    {
        Text targetText = (teamID == 0) ? team1MoneyText : team2MoneyText;
        if (targetText != null)
        {
            targetText.text = "$" + amount.ToString();
            if (amount < 0) targetText.color = Color.red;
            else targetText.color = Color.white;
        }
    }

    // [New] 顯示金錢變動
    public void ShowMoneyChange(int amount, int teamID)
    {
        if (floatingTextPrefab == null) return;

        // 決定生成點與父物件
        Transform spawnPoint = (teamID == 0) ? team1FloatTextPoint : team2FloatTextPoint;
        if (spawnPoint == null) spawnPoint = (teamID == 0) ? team1MoneyText.transform : team2MoneyText.transform;

        // 刪除舊的 (只顯示最近一次)
        if (teamID == 0)
        {
            if (_currentP1FloatText != null) Destroy(_currentP1FloatText);
        }
        else
        {
            if (_currentP2FloatText != null) Destroy(_currentP2FloatText);
        }

        // 生成新的
        // 注意：生成在 Canvas 上，所以 Parent 要設為 spawnPoint (它應該在 Canvas 裡)
        GameObject obj = Instantiate(floatingTextPrefab, spawnPoint);
        // 重置位置 (相對於 SpawnPoint)
        obj.transform.localPosition = Vector3.zero;

        // 設定內容
        FloatingText floatTxt = obj.GetComponent<FloatingText>();
        if (floatTxt != null)
        {
            string prefix = amount >= 0 ? "+" : "";
            Color color = amount >= 0 ? Color.green : Color.red;
            floatTxt.Setup(prefix + "$" + amount, color);
        }

        // 記錄參照
        if (teamID == 0) _currentP1FloatText = obj;
        else _currentP2FloatText = obj;
    }
}