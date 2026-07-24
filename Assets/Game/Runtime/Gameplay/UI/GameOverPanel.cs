using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverRequest
{
    public GameOverRequest(
        string reason,
        int finalNumber,
        Action retry,
        Action returnToTitle
    )
    {
        Reason = reason;
        FinalNumber = finalNumber;
        Retry = retry;
        ReturnToTitle = returnToTitle;
    }

    public string Reason { get; }
    public int FinalNumber { get; }
    public Action Retry { get; }
    public Action ReturnToTitle { get; }
}

public class GameOverPanel : UIPanel
{
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text finalNumberText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button titleButton;

    private GameOverRequest request;
    private bool submitted;

    public override void OnInit()
    {
        retryButton?.onClick.AddListener(Retry);
        titleButton?.onClick.AddListener(ReturnToTitle);
    }

    public override void OnOpen(object data = null)
    {
        request = data as GameOverRequest;
        submitted = false;
        SetButtonsInteractable(true);

        if (request == null)
        {
            Debug.LogError("GameOverPanel received invalid data.", this);
            return;
        }

        reasonText.text = request.Reason;
        finalNumberText.text = $"最终数字：{request.FinalNumber}";
    }

    public override void OnClose()
    {
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

    private void OnDestroy()
    {
        retryButton?.onClick.RemoveListener(Retry);
        titleButton?.onClick.RemoveListener(ReturnToTitle);
    }
}
