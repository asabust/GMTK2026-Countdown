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

    private Tween activeTween;
    private Action<NumberDeltaPopup> completed;

    public void Play(
        int delta,
        Vector2 anchoredPosition,
        Action<NumberDeltaPopup> onCompleted
    )
    {
        activeTween?.Kill(false);
        completed = onCompleted;
        gameObject.SetActive(true);

        popupTransform.anchoredPosition = anchoredPosition;
        popupTransform.localScale = Vector3.one * 0.7f;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        deltaText.text = delta > 0 ? $"+{delta}" : delta.ToString();

        Sequence sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        sequence.Append(popupTransform.DOScale(1.15f, 0.12f));
        sequence.Append(popupTransform.DOScale(1f, 0.08f));
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
