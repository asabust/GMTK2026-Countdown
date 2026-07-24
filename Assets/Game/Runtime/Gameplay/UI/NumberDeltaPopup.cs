using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class NumberDeltaPopup : MonoBehaviour
{
    [SerializeField] private RectTransform popupTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text deltaText;
    [SerializeField, Min(0f)] private float riseDistance = 70f;
    [SerializeField, Min(0f)] private float duration = 0.65f;
    [SerializeField] private Color countdownColor = new(1f, 0.82f, 0.22f, 1f);
    [SerializeField] private Color damageColor = new(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color rewardColor = new(0.35f, 1f, 0.45f, 1f);

    private Tween activeTween;
    private Action<NumberDeltaPopup> completed;
    private float baseFontSize;

    private void Awake()
    {
        if (deltaText != null)
        {
            baseFontSize = deltaText.fontSize;
        }
    }

    public void Play(
        NumberChange change,
        Vector2 anchoredPosition,
        Action<NumberDeltaPopup> onCompleted
    )
    {
        activeTween?.Kill(false);
        completed = onCompleted;
        gameObject.SetActive(true);

        popupTransform.anchoredPosition = anchoredPosition;
        popupTransform.localRotation = Quaternion.identity;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        int delta = change.Delta;
        deltaText.text = delta > 0 ? $"+{delta}" : delta.ToString();
        ConfigureStyle(change);

        Sequence sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (IsCountdownCost(change))
        {
            popupTransform.localScale = Vector3.one * 0.35f;
            sequence.Append(
                popupTransform
                    .DOScale(1.65f, 0.16f)
                    .SetEase(Ease.OutBack)
            );
            sequence.Append(
                popupTransform
                    .DOScale(1f, 0.14f)
                    .SetEase(Ease.OutQuad)
            );
            sequence.Insert(
                0.05f,
                popupTransform.DOPunchRotation(
                    new Vector3(0f, 0f, 14f),
                    0.32f,
                    9,
                    0.55f
                )
            );
        }
        else if (change.Reason == NumberChangeReason.Damage)
        {
            popupTransform.localScale = Vector3.one * 0.75f;
            sequence.Append(
                popupTransform
                    .DOScale(1.45f, 0.12f)
                    .SetEase(Ease.OutBack)
            );
            sequence.Append(popupTransform.DOScale(1f, 0.12f));
            sequence.Insert(
                0f,
                popupTransform.DOPunchRotation(
                    new Vector3(0f, 0f, -18f),
                    0.38f,
                    12,
                    0.45f
                )
            );
        }
        else
        {
            popupTransform.localScale = Vector3.one * 0.7f;
            sequence.Append(popupTransform.DOScale(1.2f, 0.12f));
            sequence.Append(popupTransform.DOScale(1f, 0.1f));
        }

        sequence.Insert(
            0f,
            popupTransform
                .DOAnchorPosY(anchoredPosition.y + riseDistance, duration)
                .SetEase(Ease.OutCubic)
        );
        sequence.Insert(
            duration * 0.46f,
            canvasGroup.DOFade(0f, duration * 0.54f)
        );
        sequence.OnComplete(HandleCompleted);
        activeTween = sequence;
    }

    private void ConfigureStyle(NumberChange change)
    {
        deltaText.fontStyle = FontStyles.Bold;
        deltaText.fontSize = baseFontSize > 0f ? baseFontSize : deltaText.fontSize;

        if (IsCountdownCost(change))
        {
            deltaText.color = countdownColor;
            deltaText.fontSize *= 1.25f;
        }
        else if (change.Reason == NumberChangeReason.Damage)
        {
            deltaText.color = damageColor;
            deltaText.fontSize *= 1.15f;
        }
        else if (change.Reason == NumberChangeReason.Reward)
        {
            deltaText.color = rewardColor;
        }
        else
        {
            deltaText.color = Color.white;
        }
    }

    private static bool IsCountdownCost(NumberChange change)
    {
        return change.Delta == -1 &&
               (change.Reason == NumberChangeReason.Move ||
                change.Reason == NumberChangeReason.Attack ||
                change.Reason == NumberChangeReason.Wait);
    }

    public void StopAndHide()
    {
        activeTween?.Kill(false);
        activeTween = null;
        completed = null;
        gameObject.SetActive(false);
    }

    private void HandleCompleted()
    {
        activeTween = null;
        Action<NumberDeltaPopup> callback = completed;
        completed = null;
        callback?.Invoke(this);
    }

    private void OnDestroy()
    {
        activeTween?.Kill(false);
    }
}
