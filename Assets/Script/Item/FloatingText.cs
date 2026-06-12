using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    public Text textComponent;
    public float duration = 2.0f;
    public float moveSpeed = 50f; // 向上飄移的速度

    public void Setup(string text, Color color)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = color;
        }
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        float timer = 0f;
        Vector3 startPos = transform.position;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // 向上飄移
            transform.position = startPos + Vector3.up * (moveSpeed * timer);

            // 淡出 (如果 Text 支援 alpha)
            if (textComponent != null)
            {
                Color c = textComponent.color;
                c.a = Mathf.Lerp(1f, 0f, timer / duration);
                textComponent.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}