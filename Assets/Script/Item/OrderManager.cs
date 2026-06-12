using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    [System.Serializable]
    public class TeamData
    {
        public int teamID;
        public List<Order> activeOrders = new List<Order>();
        public List<Order> pendingOrders = new List<Order>();
        public int currentMoney = 0;
    }

    [Header("Configuration")]
    public int maxActiveOrdersPerTeam = 2;
    public int maxPendingOrdersPerTeam = 2;

    [Header("Ingredient References")]
    public ItemData bunData;
    public ItemData meatData;
    public ItemData cheeseData;
    public ItemData tomatoData;

    [Header("State")]
    public TeamData team1Data = new TeamData() { teamID = 0 };
    public TeamData team2Data = new TeamData() { teamID = 1 };

    private int _orderIdCounter = 0;
    private bool _isGameStarted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateMoneyUI(team1Data.currentMoney, 0);
            GameUIManager.Instance.UpdateMoneyUI(team2Data.currentMoney, 1);
        }
    }

    public void StartGameLogic()
    {
        _isGameStarted = true;
        RefillTeamOrders(team1Data);
        RefillTeamOrders(team2Data);
    }

    public void StopGameLogic()
    {
        _isGameStarted = false;
    }

    private void Update()
    {
        if (!_isGameStarted) return;

        UpdateTeamOrders(team1Data);
        UpdateTeamOrders(team2Data);

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.UpdateAllOrderTimers();
    }

    private void UpdateTeamOrders(TeamData teamData)
    {
        for (int i = teamData.activeOrders.Count - 1; i >= 0; i--)
        {
            Order order = teamData.activeOrders[i];
            order.timeRemaining -= Time.deltaTime;

            if (order.timeRemaining <= 0)
            {
                FailOrder(order, teamData);
            }
        }
    }

    // [Mod] 結帳邏輯：支援搶單判定
    public bool TryDeliveryOrder(List<ItemData> deliveredItems, int delivererTeamID)
    {
        // 1. 檢查 Team 1 的訂單
        // 注意：這裡我們遍歷所有 activeOrders，看是否有符合的
        // 我們會優先檢查 deliverer 自己的隊伍 (這通常是好設計，避免誤搶自己的單)
        // 但為了公平搶單，我們可以按照固定順序檢查，或者同時檢查

        // 策略 A：先查 Team 1 再查 Team 2
        // 這意味著如果兩隊有相同的單，Team 1 的單會先被判定完成。
        // 這可能會導致 P2 本來想做自己的單，結果不小心幫 P1 完成了 (如果 P2 去 P2 櫃台)
        // 但因為 CheckoutCounter 已經限制了 P1 只能去 P1 櫃台，所以送餐者一定是櫃台主人
        // 所以邏輯很單純：我看我的手上有沒有符合 "任何一隊" 的訂單

        // 檢查 Team 1
        foreach (var order in team1Data.activeOrders)
        {
            if (CheckMatch(order.requirements, deliveredItems))
            {
                // 完成了 Team 1 的訂單
                CompleteOrder(order, team1Data, delivererTeamID);
                return true;
            }
        }

        // 檢查 Team 2
        foreach (var order in team2Data.activeOrders)
        {
            if (CheckMatch(order.requirements, deliveredItems))
            {
                // 完成了 Team 2 的訂單
                CompleteOrder(order, team2Data, delivererTeamID);
                return true;
            }
        }

        return false;
    }

    private bool CheckMatch(List<ItemData> required, List<ItemData> delivered)
    {
        if (required.Count != delivered.Count) return false;
        List<ItemData> tempDelivered = new List<ItemData>(delivered);

        foreach (var reqItem in required)
        {
            if (tempDelivered.Contains(reqItem)) tempDelivered.Remove(reqItem);
            else return false;
        }
        return true;
    }

    // [Fix] 修正金錢計算邏輯
    private void CompleteOrder(Order order, TeamData ownerTeam, int earnerTeamID)
    {
        // 基礎金額
        int money = order.totalValue;

        // 判斷是否為搶單 (送餐者 != 訂單擁有者)
        bool isSteal = (ownerTeam.teamID != earnerTeamID);

        // 如果是搶單，獎勵翻倍
        if (isSteal)
        {
            money *= 2;
            Debug.Log($"<color=yellow>搶單成功！</color> P{earnerTeamID + 1} 搶了 P{ownerTeam.teamID + 1} 的單，獲得雙倍獎勵 ${money}！");
        }
        else
        {
            Debug.Log($"Team {earnerTeamID + 1} 完成了自己的訂單，獲得 ${money}");
        }

        // [關鍵修正] 錢給 "送餐者 (Earner)"，而不是 "擁有者 (Owner)"
        if (earnerTeamID == 0)
        {
            team1Data.currentMoney += money;
        }
        else
        {
            team2Data.currentMoney += money;
        }

        // 更新 UI
        if (GameUIManager.Instance != null)
        {
            // 更新雙方金錢顯示
            GameUIManager.Instance.UpdateMoneyUI(team1Data.currentMoney, 0);
            GameUIManager.Instance.UpdateMoneyUI(team2Data.currentMoney, 1);

            // 移除完成的訂單 UI
            GameUIManager.Instance.RemoveOrderUI(order.orderID);

            // 顯示浮動文字 (在送餐者頭上或分數旁顯示加錢)
            GameUIManager.Instance.ShowMoneyChange(money, earnerTeamID);
        }

        // 從 "擁有者" 的列表中移除該訂單 (因為被搶走了/完成了)
        ownerTeam.activeOrders.Remove(order);

        // 補單 (補給 "擁有者"，因為他少了一張單)
        RefillTeamOrders(ownerTeam);
    }

    private void FailOrder(Order order, TeamData ownerTeam)
    {
        // 失敗扣錢 (這部分邏輯不變，誰的單過期就扣誰的錢)
        ownerTeam.currentMoney -= order.totalValue;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateMoneyUI(ownerTeam.currentMoney, ownerTeam.teamID);
            GameUIManager.Instance.RemoveOrderUI(order.orderID);

            // 顯示扣錢浮動文字
            GameUIManager.Instance.ShowMoneyChange(-order.totalValue, ownerTeam.teamID);
        }

        ownerTeam.activeOrders.Remove(order);
        RefillTeamOrders(ownerTeam);
    }

    private void RefillTeamOrders(TeamData teamData)
    {
        bool pendingChanged = false;
        while (teamData.activeOrders.Count < maxActiveOrdersPerTeam)
        {
            if (teamData.pendingOrders.Count > 0)
            {
                Order order = teamData.pendingOrders[0];
                teamData.pendingOrders.RemoveAt(0);
                teamData.activeOrders.Add(order);
                pendingChanged = true;
                if (GameUIManager.Instance != null) GameUIManager.Instance.AddOrderUI(order, teamData.teamID);
            }
            else
            {
                Order newOrder = CreateOrderData();
                teamData.activeOrders.Add(newOrder);
                if (GameUIManager.Instance != null) GameUIManager.Instance.AddOrderUI(newOrder, teamData.teamID);
            }
        }
        while (teamData.pendingOrders.Count < maxPendingOrdersPerTeam)
        {
            Order newOrder = CreateOrderData();
            teamData.pendingOrders.Add(newOrder);
            pendingChanged = true;
        }
        if (pendingChanged && GameUIManager.Instance != null) GameUIManager.Instance.UpdatePendingOrdersUI(teamData.pendingOrders, teamData.teamID);
    }

    private Order CreateOrderData() { List<ItemData> ingredients = GenerateRandomIngredients(); return new Order(_orderIdCounter++, ingredients, 1.0f); }
    private List<ItemData> GenerateRandomIngredients()
    {
        List<ItemData> items = new List<ItemData>();
        int bunCount = Random.Range(1, 2); int meatCount = Random.Range(0, 2); int cheeseCount = Random.Range(0, 3); int tomatoCount = Random.Range(0, 2);
        if (meatCount == 0 && cheeseCount == 0 && tomatoCount == 0) bunCount = Random.Range(2, 4);
        for (int i = 0; i < bunCount; i++) items.Add(bunData);
        for (int i = 0; i < meatCount; i++) items.Add(meatData);
        for (int i = 0; i < cheeseCount; i++) items.Add(cheeseData);
        for (int i = 0; i < tomatoCount; i++) items.Add(tomatoData);
        return items;
    }
}