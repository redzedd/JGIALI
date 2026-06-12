using UnityEngine;
using UnityEngine.InputSystem;

// 掛在 Player Prefab 上
public class PlayerVisualSetup : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("要更換顏色的 Renderer (例如身體模型)")]
    public Renderer targetRenderer;

    [Tooltip("對應玩家索引的材質球 (Element 0 = P1, Element 1 = P2...)")]
    public Material[] playerMaterials;

    private void Start()
    {
        var input = GetComponent<PlayerInput>();

        if (input != null && targetRenderer != null && playerMaterials.Length > 0)
        {
            // playerIndex 是由 PlayerInputManager 自動分配的 (0, 1, 2...)
            int index = input.playerIndex;

            // 確保 index 不會超出陣列範圍 (例如 P3 重複使用 P1 顏色)
            int matIndex = index % playerMaterials.Length;

            targetRenderer.material = playerMaterials[matIndex];
        }
    }
}