using System.Collections;
using TMPro;
using UnityEngine;

public class ToastPanel : UIPanel
{
    [SerializeField] private RectTransform window;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
    [SerializeField, Min(0f)] private float visibleDuration = 1.5f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;
    [SerializeField] private float riseDistance = 18f;

    private CanvasGroup canvasGroup;
    private Coroutine displayRoutine;
    private Vector2 restingPosition;

    public static void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        UIManager.Instance?.Open<ToastPanel>(message);
    }

    public override void OnInit()
    {
        if (window == null)
        {
            window = transform.Find("Window") as RectTransform;
        }
        if (feedbackText == null && window != null)
        {
            feedbackText = window.GetComponentInChildren<TMP_Text>(true);
        }

        canvasGroup = window != null
            ? window.GetComponent<CanvasGroup>()
            : null;
        if (canvasGroup == null && window != null)
        {
            canvasGroup = window.gameObject.AddComponent<CanvasGroup>();
        }

        if (window != null)
        {
            restingPosition = window.anchoredPosition;
        }
    }

    public override void OnOpen(object data = null)
    {
        string message = data as string;
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
        }

        displayRoutine = StartCoroutine(PlayToast(message));
    }

    public override void OnClose()
    {
        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }
    }

    private IEnumerator PlayToast(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }

        Vector2 startPosition =
            restingPosition + Vector2.down * riseDistance;
        SetPresentation(startPosition, 0f);

        yield return Animate(
            fadeInDuration,
            startPosition,
            restingPosition,
            0f,
            1f
        );
        yield return WaitRealtime(visibleDuration);
        yield return Animate(
            fadeOutDuration,
            restingPosition,
            restingPosition,
            1f,
            0f
        );

        displayRoutine = null;
        UIManager.Instance?.Close<ToastPanel>();
    }

    private IEnumerator Animate(
        float duration,
        Vector2 fromPosition,
        Vector2 toPosition,
        float fromAlpha,
        float toAlpha
    )
    {
        if (duration <= 0f)
        {
            SetPresentation(toPosition, toAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            SetPresentation(
                Vector2.LerpUnclamped(
                    fromPosition,
                    toPosition,
                    eased
                ),
                Mathf.LerpUnclamped(fromAlpha, toAlpha, eased)
            );
            yield return null;
        }

        SetPresentation(toPosition, toAlpha);
    }

    private static IEnumerator WaitRealtime(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetPresentation(Vector2 position, float alpha)
    {
        if (window != null)
        {
            window.anchoredPosition = position;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }
}
