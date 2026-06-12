using UnityEngine;

public class IngredientVisual : MonoBehaviour
{
    [Tooltip("此物件的底部接點 (用來接在下一個食材的 Top 上)")]
    public Transform bottomPoint;

    [Tooltip("此物件的頂部接點 (用來讓下一個食材疊上來)")]
    public Transform topPoint;

    private void OnDrawGizmosSelected()
    {
        if (bottomPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bottomPoint.position, 0.02f);
        }
        if (topPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(topPoint.position, 0.02f);
        }
    }
}