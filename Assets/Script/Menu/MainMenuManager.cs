using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI 面板與 CanvasGroup")]
    [Tooltip("遊戲標題圖片的 RectTransform")]
    public RectTransform titlePanel;
    [Tooltip("主選單 (紙條) 的 RectTransform")]
    public RectTransform paperMenuPanel;
    [Tooltip("主選單的 CanvasGroup")]
    public CanvasGroup mainMenuCanvasGroup;

    [Tooltip("設置面板 RectTransform")]
    public RectTransform settingsPanel;
    [Tooltip("設置面板 CanvasGroup")]
    public CanvasGroup settingsCanvasGroup;

    [Tooltip("離開確認面板 CanvasGroup")]
    public CanvasGroup quitConfirmCanvasGroup;

    [Header("返回功能 (Back Button)")]
    [Tooltip("返回按鈕的 RectTransform (包含按鈕本身)")]
    public RectTransform backButtonPanel;
    [Tooltip("實際的返回按鈕元件")]
    public Button backButtonComp;
    [Tooltip("返回按鈕從下方彈出的動畫時間")]
    public float backButtonAnimDuration = 0.5f;
    [Tooltip("返回按鈕隱藏時，要往原本位置下方移動多少距離")]
    public float backButtonSlideDistance = 300f;

    [Header("【新增】本地多人大廳")]
    [Tooltip("負責管理圓圈顏色與下一步按鈕的管理器")]
    public LocalLobbyManager localLobbyManager;

    [Header("按鈕焦點 (Gamepad支援)")]
    public GameObject firstMenuButton;
    public GameObject firstSettingsButton;
    public GameObject firstQuitButton;

    [Header("Cinemachine 鏡頭設定")]
    [Tooltip("主選單用的攝影機")]
    public CinemachineCamera mainMenuCamera;
    [Tooltip("本機遊玩用的攝影機")]
    public CinemachineCamera localPlayCamera;
    [Tooltip("連線遊玩用的攝影機")]
    public CinemachineCamera onlinePlayCamera;

    [Tooltip("UI 收回後，等待多久才開始移動鏡頭")]
    [SerializeField] private float cameraMoveDelay = 0.5f;
    [Tooltip("鏡頭開始移動後，等待多久才彈出取消按鈕")]
    [SerializeField] private float showBackButtonDelay = 2.0f;

    [Header("動畫參數")]
    [SerializeField] private float paperDropDuration = 1.0f;
    [SerializeField] private float settingsSlideDuration = 0.5f;
    [SerializeField] private float uiHideDuration = 0.5f;

    private Vector2 _paperStartPos;
    private Vector2 _paperEndPos;
    private Vector2 _titleStartPos;
    private Vector2 _titleHiddenPos;
    private Vector2 _settingsHiddenPos;
    private Vector2 _settingsShownPos;
    private Vector2 _backButtonHiddenPos;
    private Vector2 _backButtonShownPos;

    private GameObject _lastMenuButton;
    private bool _isInGameView = false;

    private List<CanvasGroup> _allCanvasGroups = new List<CanvasGroup>();

    private void Start()
    {
        InitializeUI();
        InitializeCameras();
        StartCoroutine(PlayIntroAnimation());

        if (backButtonComp != null)
        {
            backButtonComp.onClick.AddListener(OnBackToMainMenuClicked);
        }
    }

    private void Update()
    {
        CheckCancelInput();
    }

    private void CheckCancelInput()
    {
        if (_isInGameView)
        {
            bool cancelPressed = false;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                cancelPressed = true;

            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                cancelPressed = true;

            if (cancelPressed)
            {
                if (backButtonComp != null && backButtonComp.interactable)
                {
                    backButtonComp.onClick.Invoke();
                    backButtonComp.transform.DOPunchScale(Vector3.one * -0.1f, 0.1f, 10, 1);
                }
            }
        }
    }

    private void InitializeUI()
    {
        SetGameObjectActive(paperMenuPanel, true);
        SetGameObjectActive(titlePanel, true);
        SetGameObjectActive(settingsPanel, false);
        SetGameObjectActive(quitConfirmCanvasGroup, false);
        SetGameObjectActive(backButtonPanel, false);

        if (localLobbyManager != null)
            SetGameObjectActive(localLobbyManager, false);

        if (mainMenuCanvasGroup != null) _allCanvasGroups.Add(mainMenuCanvasGroup);
        if (settingsCanvasGroup != null) _allCanvasGroups.Add(settingsCanvasGroup);
        if (quitConfirmCanvasGroup != null) _allCanvasGroups.Add(quitConfirmCanvasGroup);

        _paperEndPos = paperMenuPanel.anchoredPosition;
        _paperStartPos = new Vector2(_paperEndPos.x, Screen.height + 500);
        paperMenuPanel.anchoredPosition = _paperStartPos;

        if (titlePanel != null)
        {
            _titleStartPos = titlePanel.anchoredPosition;
            _titleHiddenPos = new Vector2(_titleStartPos.x, Screen.height + 300);
        }

        _settingsShownPos = settingsPanel.anchoredPosition;
        _settingsHiddenPos = _settingsShownPos + new Vector2(Screen.width, 0);
        settingsPanel.anchoredPosition = _settingsHiddenPos;

        if (backButtonPanel != null)
        {
            _backButtonShownPos = backButtonPanel.anchoredPosition;
            _backButtonHiddenPos = _backButtonShownPos - new Vector2(0, backButtonSlideDistance);

            backButtonPanel.anchoredPosition = _backButtonHiddenPos;
            backButtonComp.interactable = false;
        }

        LockAllWindows();
        _lastMenuButton = firstMenuButton;
    }

    private void InitializeCameras()
    {
        if (mainMenuCamera != null) mainMenuCamera.Priority = 10;
        if (localPlayCamera != null) localPlayCamera.Priority = 0;
        if (onlinePlayCamera != null) onlinePlayCamera.Priority = 0;
    }

    private IEnumerator PlayIntroAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        SetGameObjectActive(paperMenuPanel, true);
        SetGameObjectActive(titlePanel, true);

        paperMenuPanel.DOAnchorPos(_paperEndPos, paperDropDuration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => SwitchToWindow(mainMenuCanvasGroup, firstMenuButton));
    }

    // --- 進入遊戲流程 ---

    public void OnLocalPlayClicked()
    {
        Debug.Log("本機遊玩流程開始");
        RecordCurrentButton();
        LockAllWindows();

        Sequence hideSeq = DOTween.Sequence();
        hideSeq.Join(paperMenuPanel.DOAnchorPos(_paperStartPos, uiHideDuration).SetEase(Ease.InBack));
        if (titlePanel != null)
            hideSeq.Join(titlePanel.DOAnchorPos(_titleHiddenPos, uiHideDuration).SetEase(Ease.InBack));

        hideSeq.OnComplete(() => {
            SetGameObjectActive(paperMenuPanel, false);
            SetGameObjectActive(titlePanel, false);
            StartCoroutine(SwitchToCameraRoutine(localPlayCamera));
        });
    }

    public void OnOnlinePlayClicked()
    {
        Debug.Log("連線遊玩流程開始");
        RecordCurrentButton();
        LockAllWindows();

        Sequence hideSeq = DOTween.Sequence();
        hideSeq.Join(paperMenuPanel.DOAnchorPos(_paperStartPos, uiHideDuration).SetEase(Ease.InBack));
        if (titlePanel != null)
            hideSeq.Join(titlePanel.DOAnchorPos(_titleHiddenPos, uiHideDuration).SetEase(Ease.InBack));

        hideSeq.OnComplete(() => {
            SetGameObjectActive(paperMenuPanel, false);
            SetGameObjectActive(titlePanel, false);
            StartCoroutine(SwitchToCameraRoutine(onlinePlayCamera));
        });
    }

    private IEnumerator SwitchToCameraRoutine(CinemachineCamera targetCamera)
    {
        yield return new WaitForSeconds(cameraMoveDelay);

        if (mainMenuCamera != null) mainMenuCamera.Priority = 0;
        if (localPlayCamera != null) localPlayCamera.Priority = 0;
        if (onlinePlayCamera != null) onlinePlayCamera.Priority = 0;

        if (targetCamera != null)
            targetCamera.Priority = 10;

        yield return new WaitForSeconds(showBackButtonDelay);

        ShowBackButton();

        // 啟用大廳 (這會觸發 LocalLobbyManager 的 OnEnable，進而播放淡入動畫)
        if (targetCamera == localPlayCamera && localLobbyManager != null)
        {
            SetGameObjectActive(localLobbyManager, true);
        }
    }

    private void ShowBackButton()
    {
        if (backButtonPanel != null)
        {
            if (backButtonPanel.localScale == Vector3.zero) backButtonPanel.localScale = Vector3.one;

            SetGameObjectActive(backButtonPanel, true);
            backButtonComp.interactable = true;

            backButtonPanel.DOAnchorPos(_backButtonShownPos, backButtonAnimDuration).SetEase(Ease.OutBack)
                .OnComplete(() => {
                    _isInGameView = true;
                    SelectButton(backButtonComp.gameObject);
                });
        }
    }

    // --- 退出邏輯更新 ---
    public void OnBackToMainMenuClicked()
    {
        if (!_isInGameView) return;

        Debug.Log("取消遊玩，返回主選單");
        _isInGameView = false;
        backButtonComp.interactable = false;

        // 【修改點】通知大廳執行退出動畫與清理
        if (localLobbyManager != null && localLobbyManager.gameObject.activeSelf)
        {
            localLobbyManager.QuitLobby(() => {
                SetGameObjectActive(localLobbyManager, false);
            });
        }
        else
        {
            // 防呆：如果沒開大廳，直接確保關閉
            SetGameObjectActive(localLobbyManager, false);
        }

        // 收回返回按鈕
        if (backButtonPanel != null)
        {
            backButtonPanel.DOAnchorPos(_backButtonHiddenPos, backButtonAnimDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    SetGameObjectActive(backButtonPanel, false);
                });
        }

        // 鏡頭復原
        if (mainMenuCamera != null) mainMenuCamera.Priority = 10;
        if (localPlayCamera != null) localPlayCamera.Priority = 0;
        if (onlinePlayCamera != null) onlinePlayCamera.Priority = 0;

        StartCoroutine(RestoreUIRoutine());
    }

    private IEnumerator RestoreUIRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        if (titlePanel != null && titlePanel.localScale == Vector3.zero) titlePanel.localScale = Vector3.one;
        if (paperMenuPanel.localScale == Vector3.zero) paperMenuPanel.localScale = Vector3.one;

        SetGameObjectActive(titlePanel, true);
        SetGameObjectActive(paperMenuPanel, true);

        if (titlePanel != null)
            titlePanel.DOAnchorPos(_titleStartPos, uiHideDuration).SetEase(Ease.OutBack);

        paperMenuPanel.DOAnchorPos(_paperEndPos, uiHideDuration).SetEase(Ease.OutBack)
            .OnComplete(() => {
                SwitchToWindow(mainMenuCanvasGroup, _lastMenuButton);
            });
    }

    // --- 設置與離開 ---

    public void OnSettingsClicked()
    {
        RecordCurrentButton();
        if (settingsPanel.localScale == Vector3.zero) settingsPanel.localScale = Vector3.one;
        SetGameObjectActive(settingsPanel, true);
        settingsPanel.DOAnchorPos(_settingsShownPos, settingsSlideDuration).SetEase(Ease.OutBack);
        SwitchToWindow(settingsCanvasGroup, firstSettingsButton);
    }

    public void CloseSettings()
    {
        settingsPanel.DOAnchorPos(_settingsHiddenPos, settingsSlideDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                SetGameObjectActive(settingsPanel, false);
                SwitchToWindow(mainMenuCanvasGroup, _lastMenuButton);
            });
    }

    public void OnQuitClicked()
    {
        RecordCurrentButton();
        if (quitConfirmCanvasGroup.transform.localScale == Vector3.zero) quitConfirmCanvasGroup.transform.localScale = Vector3.one;
        SetGameObjectActive(quitConfirmCanvasGroup, true);
        quitConfirmCanvasGroup.alpha = 1;
        quitConfirmCanvasGroup.transform.localScale = Vector3.zero;
        quitConfirmCanvasGroup.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        SwitchToWindow(quitConfirmCanvasGroup, firstQuitButton);
    }

    public void ConfirmQuit(bool isQuit)
    {
        if (isQuit)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        else
        {
            quitConfirmCanvasGroup.transform.DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    SetGameObjectActive(quitConfirmCanvasGroup, false);
                    SwitchToWindow(mainMenuCanvasGroup, _lastMenuButton);
                    quitConfirmCanvasGroup.transform.localScale = Vector3.one;
                });
            LockAllWindows();
        }
    }

    private void SwitchToWindow(CanvasGroup targetWindow, GameObject buttonToSelect)
    {
        foreach (var group in _allCanvasGroups)
        {
            if (group == null) continue;
            bool isTarget = (group == targetWindow);
            group.interactable = isTarget;
            group.blocksRaycasts = isTarget;
        }
        if (buttonToSelect != null) SelectButton(buttonToSelect);
    }

    private void LockAllWindows()
    {
        foreach (var group in _allCanvasGroups)
        {
            if (group != null) { group.interactable = false; group.blocksRaycasts = false; }
        }
    }

    private void SetGameObjectActive(Component comp, bool active)
    {
        if (comp != null) comp.gameObject.SetActive(active);
    }

    private void RecordCurrentButton()
    {
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            _lastMenuButton = EventSystem.current.currentSelectedGameObject;
        }
    }

    private void SelectButton(GameObject btn)
    {
        if (btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn);
        }
    }
}