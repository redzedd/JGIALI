using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; // 引入 TextMeshPro
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class LocalLobbyManager : MonoBehaviour
{
    [Header("UI 設定 (Switch 風格)")]
    [Tooltip("請配置玩家對應 P1, P2, P3, P4 的 UI Slot 物件 (需包含 Image 與 TextMeshProUGUI)")]
    public List<GameObject> playerSlots;

    [Tooltip("預設連線顏色 (P1~P4)，建議：紅藍綠黃")]
    public Color[] playerColors;

    [Tooltip("未連線時的顏色")]
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("下一步按鈕 (Next Button)")]
    public RectTransform nextButtonPanel;
    public Button nextButtonComp;
    public float animDuration = 0.5f;
    public float slideDistance = 300f;

    // 內部變數
    private List<PlayerInput> _connectedPlayers = new List<PlayerInput>();
    private PlayerInputManager _manager;
    private Vector2 _nextBtnShownPos;
    private Vector2 _nextBtnHiddenPos;
    private bool _isNextButtonVisible = false;

    private PlayerControls _controls;

    private void Awake()
    {
        _controls = new PlayerControls();
        InitializeColors();
        InitializeNextButton();
        InitializeSlots();
    }

    private void InitializeColors()
    {
        if (playerColors == null || playerColors.Length < 4)
        {
            playerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };
        }
    }

    private void InitializeNextButton()
    {
        if (nextButtonPanel != null)
        {
            _nextBtnShownPos = nextButtonPanel.anchoredPosition;
            _nextBtnHiddenPos = _nextBtnShownPos - new Vector2(0, slideDistance);

            nextButtonPanel.anchoredPosition = _nextBtnHiddenPos;
            nextButtonPanel.gameObject.SetActive(false);
            if (nextButtonComp != null) nextButtonComp.interactable = false;
        }
    }

    private void InitializeSlots()
    {
        // 預設將所有 Slot 初始化為未連線狀態
        for (int i = 0; i < playerSlots.Count; i++)
        {
            UpdateSlotUI(i, false);
            // 隱藏 Slot 以便稍後播放進場動畫
            if (playerSlots[i] != null) playerSlots[i].SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 獲取場景中的 PlayerInputManager
        _manager = FindAnyObjectByType<PlayerInputManager>();

        if (_manager == null)
        {
            Debug.LogError("錯誤：場景中找不到 PlayerInputManager，請確認已創建 ConnectionManager。");
            return;
        }

        // 啟用加入功能 (Switch 風格的配對)
        _manager.EnableJoining();
        _manager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed; // 預設為按鈕加入模式 (會被覆蓋)

        // 強制設定 (建議在 Inspector 設定，這裡只是保險)
        // 注意：為了避免重複設定，這裡再次呼叫
        // 因為 PlayerInputManager 的 Unity Event，若沒設定可能會沒反應
        // 建議主要在 Inspector 綁定 (比較直觀)，這裡主要處理 UI 邏輯

        // Override the behavior to Manual to support LB+RB specific join
        _manager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
        
        _controls.Lobby.Enable();
        _controls.Lobby.Join.performed += OnJoinAction;

        DoEntranceAnimation();
    }

    private void OnDisable()
    {
        if (_controls != null)
        {
            _controls.Lobby.Join.performed -= OnJoinAction;
            _controls.Lobby.Disable();
        }

        if (_manager != null)
        {
            _manager.DisableJoining();
        }
    }

    private void OnDestroy()
    {
        _controls?.Dispose();
    }

    private void OnJoinAction(InputAction.CallbackContext context)
    {
        if (_manager == null || !_manager.joiningEnabled) return;

        // 檢查最大玩家數 (如果需要)
        // if (_manager.playerCount >= _manager.maxPlayerCount) return;

        var device = context.control.device;
        
        // 檢查裝置是否已被使用
        foreach (var player in PlayerInput.all)
        {
            foreach (var d in player.devices)
            {
                if (d.deviceId == device.deviceId) return; // 該裝置已配對
            }
        }

        // 手動加入玩家並綁定該裝置
        _manager.JoinPlayer(pairWithDevice: device);
    }

    // --- PlayerInputManager 事件接收 (請在 Inspector 的 Events 介面綁定此處) ---

    public void OnPlayerJoined(PlayerInput input)
    {
        int index = input.playerIndex;
        Debug.Log($"[Lobby] Player {index + 1} Joined via {input.devices[0].displayName}");

        // 1. 設定玩家物件層級
        input.transform.SetParent(transform); // 保持 Hierarchy 整潔
        input.gameObject.name = $"Player_{index + 1}_Data";

        // 重要：切換場景時保留玩家物件
        DontDestroyOnLoad(input.gameObject);

        // 2. 加入列表
        if (!_connectedPlayers.Contains(input))
        {
            _connectedPlayers.Add(input);
        }

        // 3. 更新 UI
        UpdateSlotUI(index, true, input.devices[0].displayName);

        // 4. 檢查是否達到開始條件
        CheckReadyStatus();
    }

    public void OnPlayerLeft(PlayerInput input)
    {
        int index = input.playerIndex;
        Debug.Log($"[Lobby] Player {index + 1} Left");

        if (_connectedPlayers.Contains(input))
        {
            _connectedPlayers.Remove(input);
        }

        // 更新對應 UI 為未連線
        UpdateSlotUI(index, false);

        // 檢查是否取消開始條件
        CheckReadyStatus();
    }

    // --- UI 更新 ---

    private void UpdateSlotUI(int index, bool isConnected, string deviceName = "")
    {
        if (index >= playerSlots.Count || playerSlots[index] == null) return;

        GameObject slot = playerSlots[index];
        slot.SetActive(true); // 確保顯示

        Image bgImage = slot.GetComponent<Image>();
        var texts = slot.GetComponentsInChildren<TextMeshProUGUI>();
        // 假設 Text 0 是狀態文字 ("Press L+R" / "Ready")

        if (isConnected)
        {
            // 設定玩家代表顏色
            if (bgImage != null)
            {
                Color c = (index < playerColors.Length) ? playerColors[index] : Color.white;
                c.a = 1f;
                bgImage.color = c;
                bgImage.DOFade(1, 0.3f); // 淡入效果
            }

            if (texts.Length > 0)
            {
                texts[0].text = $"P{index + 1}\nReady!";
                texts[0].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f); // 文字跳動效果
            }
        }
        else
        {
            // 設定未連線顏色
            if (bgImage != null)
            {
                bgImage.color = inactiveColor;
            }

            if (texts.Length > 0)
            {
                // 這裡的 L+R 提示文字可以根據 Input Actions 設定調整
                texts[0].text = "Press L+R\nto Join";
            }
        }
    }

    private void CheckReadyStatus()
    {
        // 規則：至少 2 人才能開始 (可依需求調整)
        bool canStart = _connectedPlayers.Count >= 2;

        if (canStart && !_isNextButtonVisible)
        {
            ShowNextButton();
        }
        else if (!canStart && _isNextButtonVisible)
        {
            HideNextButton();
        }
    }

    // --- 進場與流程控制 ---

    private void DoEntranceAnimation()
    {
        // 播放 Slot 進場動畫
        for (int i = 0; i < playerSlots.Count; i++)
        {
            if (playerSlots[i] != null)
            {
                playerSlots[i].SetActive(true);
                playerSlots[i].transform.localScale = Vector3.zero;
                playerSlots[i].transform.DOScale(Vector3.one, 0.5f).SetDelay(i * 0.1f).SetEase(Ease.OutBack);
            }
        }
    }

    private void ShowNextButton()
    {
        if (nextButtonPanel == null) return;
        _isNextButtonVisible = true;
        nextButtonPanel.gameObject.SetActive(true);
        if (nextButtonComp != null) nextButtonComp.interactable = true;
        nextButtonPanel.DOAnchorPos(_nextBtnShownPos, animDuration).SetEase(Ease.OutBack);
    }

    private void HideNextButton()
    {
        if (nextButtonPanel == null) return;
        _isNextButtonVisible = false;
        if (nextButtonComp != null) nextButtonComp.interactable = false;
        nextButtonPanel.DOAnchorPos(_nextBtnHiddenPos, animDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => nextButtonPanel.gameObject.SetActive(false));
    }

    // --- 供 MainMenuManager 呼叫的退出方法 ---

    public void QuitLobby(System.Action onComplete)
    {
        if (_manager != null) _manager.DisableJoining();

        Sequence seq = DOTween.Sequence();

        // 1. Slot 縮小
        foreach (var slot in playerSlots)
        {
            if (slot != null) seq.Join(slot.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
        }

        // 2. 下一步按鈕隱藏
        if (_isNextButtonVisible && nextButtonPanel != null)
        {
            seq.Join(nextButtonPanel.DOAnchorPos(_nextBtnHiddenPos, animDuration).SetEase(Ease.InBack));
        }

        seq.OnComplete(() => {
            // 清除所有玩家物件
            RemoveAllPlayers();

            // 重置 UI
            _isNextButtonVisible = false;
            if (nextButtonPanel != null) nextButtonPanel.gameObject.SetActive(false);
            foreach (var slot in playerSlots) { if (slot != null) slot.SetActive(false); }

            onComplete?.Invoke();
        });
    }

    private void RemoveAllPlayers()
    {
        // 複製一份列表以避免在迴圈中修改集合
        var playersToRemove = new List<PlayerInput>(_connectedPlayers);

        foreach (var p in playersToRemove)
        {
            if (p != null) Destroy(p.gameObject);
        }
        _connectedPlayers.Clear();
    }
}
