using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

// 如果你有 FEEL，建議在此腳本中加入 MMF_Player 的參考來播放音效和震動
// using MoreMountains.Feedbacks;

[RequireComponent(typeof(Button))]
public class BouncyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("外觀設定")]
    [SerializeField] private float scaleAmount = 1.15f; // 選中時放大的倍率
    [SerializeField] private float duration = 0.2f; // 動畫時間

    [Header("Unity 6 / DOTween 設定")]
    // 儲存原始縮放大小
    private Vector3 originalScale;
    private Tween currentTween;

    // 【新增】確保只初始化一次的旗標
    private bool isInitialized = false;

    // 【修正重點】改用 Awake 初始化
    // Awake 會在 MainMenuManager 的 Start 之前執行，確保在被關閉前就記錄好 Scale
    private void Awake()
    {
        InitializeScale();
    }

    private void Start()
    {
        InitializeScale();
    }

    private void InitializeScale()
    {
        if (!isInitialized)
        {
            // 防呆：如果 Awake 時抓到的是 0 (可能剛好被縮小)，且不是原本就想隱藏，這裡可以做個保護
            // 但最重要的是不要在 OnDisable 時把 0 寫回去覆蓋掉原本的 Inspector 數值

            // 只有當抓到的 Scale 不為 0，才視為有效初始化
            if (transform.localScale != Vector3.zero)
            {
                originalScale = transform.localScale;
                isInitialized = true;
            }
        }
    }

    private void OnDisable()
    {
        // 【修正重點】只有初始化過 (有抓到正確 Scale) 才還原
        // 防止未初始化時 (originalScale為0) 覆蓋掉正確的 Scale
        if (isInitialized)
        {
            if (currentTween != null) currentTween.Kill();

            // 防止物件被隱藏時 Tween 還在跑導致報錯，並還原大小
            transform.localScale = originalScale;
        }
    }

    // --- 搖桿/鍵盤 選取邏輯 (ISelectHandler) ---
    public void OnSelect(BaseEventData eventData)
    {
        DoScaleUp();
        // 如果有 FEEL: feedbacksSelect.PlayFeedbacks();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        DoScaleDown();
    }

    // --- 滑鼠 游標邏輯 (IPointerEnterHandler) ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        DoScaleUp();
        // 如果有 FEEL: feedbacksSelect.PlayFeedbacks();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DoScaleDown();
    }

    // --- 按下邏輯 (ISubmitHandler - 搖桿A鍵或Enter) ---
    public void OnSubmit(BaseEventData eventData)
    {
        DoPressAnimation();
    }

    private void DoScaleUp()
    {
        // 確保有初始化，避免對 Scale 0 進行放大
        if (!isInitialized) InitializeScale();

        if (currentTween != null) currentTween.Kill();
        // 使用 OutBack 讓按鈕選中時有彈一下的感覺
        currentTween = transform.DOScale(originalScale * scaleAmount, duration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void DoScaleDown()
    {
        if (!isInitialized) return;

        if (currentTween != null) currentTween.Kill();
        currentTween = transform.DOScale(originalScale, duration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void DoPressAnimation()
    {
        // 按下時瞬間縮小再彈回
        transform.DOPunchScale(Vector3.one * -0.2f, 0.2f, 10, 1).SetUpdate(true);
        // 如果有 FEEL: feedbacksClick.PlayFeedbacks();
    }
}