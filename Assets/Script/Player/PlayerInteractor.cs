using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("偵測球心的偏移量 (建議 Z=0.5 表示在前方 0.5m 處)")]
    public Vector3 interactionOffset = new Vector3(0, 0.5f, 0.5f);

    [Tooltip("偵測半徑 (範圍大小，建議 0.8 以覆蓋更廣的區域)")]
    public float interactionRadius = 0.8f;

    [Tooltip("互動圖層")]
    public LayerMask interactionLayer;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    public IInteractable GetClosestInteractable()
    {
        // (Fix Issue 1) 改用 OverlapSphere
        // 這會偵測以 interactionOffset 為圓心，interactionRadius 為半徑內的所有物體
        // 比起 Raycast/SphereCast，這更能解決 "貼太近撿不到" 的問題
        Vector3 center = transform.position + transform.TransformDirection(interactionOffset);

        Collider[] colliders = Physics.OverlapSphere(center, interactionRadius, interactionLayer);

        if (colliders.Length == 0) return null;

        // 尋找最近的互動目標
        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            IInteractable interactable = col.GetComponent<IInteractable>();
            if (interactable == null) interactable = col.GetComponentInParent<IInteractable>();

            if (interactable != null && interactable is MonoBehaviour mb)
            {
                float dist = Vector3.Distance(transform.position, mb.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestInteractable = interactable;
                }
            }
        }

        return closestInteractable;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = new Color(1, 1, 0, 0.4f); // 半透明黃色
        Vector3 center = transform.position + transform.TransformDirection(interactionOffset);
        Gizmos.DrawSphere(center, interactionRadius);
    }
}