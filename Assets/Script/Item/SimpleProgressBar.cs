using UnityEngine;
using UnityEngine.UI;

public class SimpleProgressBar : MonoBehaviour
{
    [Header("UI References")]
    public GameObject uiCanvas; // 整個 Canvas 或進度條的父物件 (用於隱藏)
    public Image fillImage;     // 進度條的填充圖 (Type = Filled)

    [Header("Settings")]
    public bool lookAtCamera = true;

    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
        Hide();
    }

    private void LateUpdate()
    {
        if (lookAtCamera && uiCanvas.activeSelf)
        {
            // 讓 UI 永遠面向攝影機
            transform.forward = _mainCamera.transform.forward;
        }
    }

    public void UpdateProgress(float current, float max)
    {
        if (!uiCanvas.activeSelf) uiCanvas.SetActive(true);

        float progress = Mathf.Clamp01(current / max);
        if (fillImage != null) fillImage.fillAmount = progress;
    }

    public void Hide()
    {
        if (uiCanvas.activeSelf) uiCanvas.SetActive(false);
    }
}