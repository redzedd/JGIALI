using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject startScreenPanel;
    public Image p1JoinIndicator;
    public Image p2JoinIndicator;
    public CanvasGroup startScreenCanvasGroup;

    public GameObject tutorialPanel;
    public Animator tutorialAnimator;

    public GameObject countdownPanel;
    public Text countdownText;

    public GameObject gameplayUIPanel;
    public Text gameTimerText;

    public GameObject gameOverPanel;
    public Text winnerText;
    public Text p1FinalScoreText;
    public Text p2FinalScoreText;

    // [New] 重新開始提示文字
    public Text restartPromptText;

    [Header("Settings")]
    public float fadeDuration = 1.0f;
    public int countdownTime = 3;
    public float gameDuration = 180f;

    private bool _p1Joined = false;
    private bool _p2Joined = false;
    private bool _isInTutorial = false;
    public bool IsGameRunning { get; private set; } = false;

    private float _currentGameTime;

    // [New] 遊戲結束狀態
    private bool _isGameOver = false;
    private float _gameOverInputDelay = 5.0f; // 等待 5 秒
    private float _gameOverTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        startScreenPanel.SetActive(true);
        tutorialPanel.SetActive(false);
        countdownPanel.SetActive(false);
        gameplayUIPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (restartPromptText) restartPromptText.gameObject.SetActive(false);

        if (p1JoinIndicator) p1JoinIndicator.color = Color.white;
        if (p2JoinIndicator) p2JoinIndicator.color = Color.white;

        var inputManager = GetComponent<PlayerInputManager>();
        if (inputManager != null)
        {
            inputManager.onPlayerJoined += OnPlayerJoined;
        }
    }

    private void OnPlayerJoined(PlayerInput input)
    {
        if (IsGameRunning || _isInTutorial || _isGameOver) return;

        int index = input.playerIndex;

        if (index == 0)
        {
            _p1Joined = true;
            if (p1JoinIndicator) p1JoinIndicator.color = Color.blue;
        }
        else if (index == 1)
        {
            _p2Joined = true;
            if (p2JoinIndicator) p2JoinIndicator.color = Color.red;
        }

        if (_p1Joined && _p2Joined)
        {
            StartCoroutine(TransitionToTutorial());
        }
    }

    private IEnumerator TransitionToTutorial()
    {
        yield return new WaitForSeconds(0.5f);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            startScreenCanvasGroup.alpha = 1 - (timer / fadeDuration);
            yield return null;
        }
        startScreenPanel.SetActive(false);

        _isInTutorial = true;
        tutorialPanel.SetActive(true);
        if (tutorialAnimator != null) tutorialAnimator.SetTrigger("PopUp");
    }

    private void Update()
    {
        // 教學階段
        if (_isInTutorial)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
            {
                StartCoroutine(StartCountdown());
            }
        }

        // 遊戲進行中
        if (IsGameRunning)
        {
            _currentGameTime -= Time.deltaTime;
            UpdateTimerUI();

            if (_currentGameTime <= 0)
            {
                EndGame();
            }
        }

        // [New] 遊戲結束階段
        if (_isGameOver)
        {
            _gameOverTimer += Time.deltaTime;

            // 5秒後顯示提示並允許輸入
            if (_gameOverTimer >= _gameOverInputDelay)
            {
                if (restartPromptText && !restartPromptText.gameObject.activeSelf)
                    restartPromptText.gameObject.SetActive(true);

                CheckGameOverInput();
            }
        }
    }

    // [New] 偵測全域輸入
    private void CheckGameOverInput()
    {
        bool interactPressed = false;
        bool pickupPressed = false;

        // 檢查鍵盤
        if (Keyboard.current != null)
        {
            // 假設 Interact = E, Pickup = F (根據你的 Input Action 對應)
            if (Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
            if (Keyboard.current.fKey.wasPressedThisFrame) pickupPressed = true;
        }

        // 檢查手把 (全部手把)
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame) interactPressed = true; // A/B
            if (gamepad.buttonWest.wasPressedThisFrame) pickupPressed = true; // X/Y
        }

        if (interactPressed)
        {
            RestartGame();
        }
        else if (pickupPressed)
        {
            QuitGame();
        }
    }

    private IEnumerator StartCountdown()
    {
        _isInTutorial = false;
        tutorialPanel.SetActive(false);
        countdownPanel.SetActive(true);

        for (int i = countdownTime; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        countdownPanel.SetActive(false);
        StartGame();
    }

    private void StartGame()
    {
        IsGameRunning = true;
        _currentGameTime = gameDuration;
        gameplayUIPanel.SetActive(true);
        if (OrderManager.Instance != null) OrderManager.Instance.StartGameLogic();
    }

    private void UpdateTimerUI()
    {
        if (gameTimerText != null)
        {
            int minutes = Mathf.FloorToInt(_currentGameTime / 60F);
            int seconds = Mathf.FloorToInt(_currentGameTime - minutes * 60);
            gameTimerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
            if (_currentGameTime <= 10f) gameTimerText.color = Color.red;
            else gameTimerText.color = Color.white;
        }
    }

    private void EndGame()
    {
        IsGameRunning = false;
        _isGameOver = true; // 進入結束狀態
        _gameOverTimer = 0f;

        _currentGameTime = 0;
        UpdateTimerUI();

        if (OrderManager.Instance != null) OrderManager.Instance.StopGameLogic();

        gameplayUIPanel.SetActive(false);
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            int p1Score = OrderManager.Instance.team1Data.currentMoney;
            int p2Score = OrderManager.Instance.team2Data.currentMoney;

            if (p1FinalScoreText) p1FinalScoreText.text = $"P1 (Red): ${p1Score}";
            if (p2FinalScoreText) p2FinalScoreText.text = $"P2 (Blue): ${p2Score}";

            if (p1Score > p2Score)
            {
                winnerText.text = "P1 Wins!";
                winnerText.color = Color.red;
            }
            else if (p2Score > p1Score)
            {
                winnerText.text = "P2 Wins!";
                winnerText.color = Color.blue;
            }
            else
            {
                winnerText.text = "Draw!";
                winnerText.color = Color.white;
            }
        }
    }

    public void RestartGame()
    {
        // 重新載入當前場景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}