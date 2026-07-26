using System;
using System.Collections;
using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverRequest
{
    public GameOverRequest(
        string reason,
        int finalNumber,
        Action retry,
        Action returnToTitle,
        string title = null
    )
    {
        Reason = reason;
        FinalNumber = finalNumber;
        Retry = retry;
        ReturnToTitle = returnToTitle;
        Title = title;
    }

    public string Reason { get; }
    public int FinalNumber { get; }
    public Action Retry { get; }
    public Action ReturnToTitle { get; }
    public string Title { get; }
}

public class GameOverPanel : UIPanel
{
    [SerializeField] private Image titleImage;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button titleButton;
    [SerializeField, Min(0f)] private float titleFadeDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float titleStartScale = 0.9f;

    private GameOverRequest request;
    private bool submitted;
    private Coroutine titleAnimation;
    private Vector3 titleRestingScale = Vector3.one;
    private Color titleRestingColor = Color.white;

    public override void OnInit()
    {
        retryButton?.onClick.AddListener(Retry);
        titleButton?.onClick.AddListener(ReturnToTitle);
        if (titleImage != null)
        {
            titleRestingScale = titleImage.rectTransform.localScale;
            titleRestingColor = titleImage.color;
        }
    }

    public override void OnOpen(object data = null)
    {
        request = data as GameOverRequest;
        submitted = false;
        SetButtonsInteractable(true);
        UILocalization.SetButtonText(retryButton, "game_over.retry");
        UILocalization.SetButtonText(titleButton, "game_over.return_title");

        if (request == null)
        {
            Debug.LogError("GameOverPanel received invalid data.", this);
            return;
        }

        if (reasonText != null)
        {
            reasonText.text = request.Reason;
        }
        PlayTitleAnimation();
    }

    public override void OnClose()
    {
        StopTitleAnimation();
        request = null;
        submitted = false;
    }

    private void Retry()
    {
        if (!TrySubmit())
        {
            return;
        }

        request.Retry?.Invoke();
    }

    private void ReturnToTitle()
    {
        if (!TrySubmit())
        {
            return;
        }

        request.ReturnToTitle?.Invoke();
    }

    private bool TrySubmit()
    {
        if (submitted || request == null)
        {
            return false;
        }

        submitted = true;
        SetButtonsInteractable(false);
        return true;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (retryButton != null)
        {
            retryButton.interactable = interactable;
        }

        if (titleButton != null)
        {
            titleButton.interactable = interactable;
        }
    }

    private void PlayTitleAnimation()
    {
        StopTitleAnimation();
        if (titleImage == null)
        {
            return;
        }

        titleAnimation = StartCoroutine(AnimateTitle());
    }

    private IEnumerator AnimateTitle()
    {
        RectTransform rectTransform = titleImage.rectTransform;
        Vector3 startScale = titleRestingScale * titleStartScale;
        Color color = titleRestingColor;
        rectTransform.localScale = startScale;
        color.a = 0f;
        titleImage.color = color;

        if (titleFadeDuration <= 0f)
        {
            RestoreTitlePresentation();
            titleAnimation = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < titleFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(
                elapsed / titleFadeDuration
            );
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            rectTransform.localScale = Vector3.LerpUnclamped(
                startScale,
                titleRestingScale,
                eased
            );
            color.a = Mathf.LerpUnclamped(
                0f,
                titleRestingColor.a,
                eased
            );
            titleImage.color = color;
            yield return null;
        }

        RestoreTitlePresentation();
        titleAnimation = null;
    }

    private void StopTitleAnimation()
    {
        if (titleAnimation != null)
        {
            StopCoroutine(titleAnimation);
            titleAnimation = null;
        }
        RestoreTitlePresentation();
    }

    private void RestoreTitlePresentation()
    {
        if (titleImage == null)
        {
            return;
        }

        titleImage.rectTransform.localScale = titleRestingScale;
        titleImage.color = titleRestingColor;
    }

    private void OnDestroy()
    {
        StopTitleAnimation();
        retryButton?.onClick.RemoveListener(Retry);
        titleButton?.onClick.RemoveListener(ReturnToTitle);
    }
}
