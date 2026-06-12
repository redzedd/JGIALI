using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 掛在 Player Prefab 上，負責處理大廳中的個別玩家輸入 (如取消準備/退出)
/// </summary>
public class PlayerLobbyHandler : MonoBehaviour
{
    private PlayerInput _playerInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        // 確保這個物件在切換場景時不會被自動銷毀 (由 LobbyManager 在加入時設定，這裡再次確認)
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// 對應 Input Actions 中的 "Cancel" 動作 (通常是 B 鍵或 Esc)
    /// 需要在 PlayerInput 組件的 Events -> GameControls -> Lobby -> Cancel 中綁定此方法
    /// </summary>
    public void OnCancel(InputAction.CallbackContext context)
    {
        // 只在按下瞬間觸發
        if (context.performed)
        {
            Debug.Log($"[Player] P{_playerInput.playerIndex + 1} 請求退出");

            // 自行銷毀，PlayerInputManager 會自動偵測到物件消失，
            // 並觸發 LobbyManager 的 OnPlayerLeft 事件，進而更新 UI
            Destroy(gameObject);
        }
    }

    // 未來擴充：
    // public void OnChangeCharacter(InputAction.CallbackContext context) { ... }
    // public void OnReadyToggle(InputAction.CallbackContext context) { ... }
}