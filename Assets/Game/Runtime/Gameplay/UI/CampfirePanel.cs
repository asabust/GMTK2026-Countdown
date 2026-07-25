using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CampfireRequest
{
    public CampfireRequest(
        int minimumRestore,
        int maximumRestore,
        NumberResource numberResource,
        Action rest,
        Action leave
    )
    {
        MinimumRestore = minimumRestore;
        MaximumRestore = maximumRestore;
        NumberResource = numberResource;
        Rest = rest;
        Leave = leave;
    }

    public int MinimumRestore { get; }
    public int MaximumRestore { get; }
    public NumberResource NumberResource { get; }
    public Action Rest { get; }
    public Action Leave { get; }
}

public sealed class CampfirePanel : UIPanel
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button restButton;
    [SerializeField] private TMP_Text restButtonText;
    [SerializeField] private Button leaveButton;

    private CampfireRequest request;
    private bool awaitingFullNumberConfirmation;
    private bool submitted;

    public override void OnInit()
    {
        restButton?.onClick.AddListener(HandleRest);
        leaveButton?.onClick.AddListener(HandleLeave);
    }

    public override void OnOpen(object data = null)
    {
        request = data as CampfireRequest;
        awaitingFullNumberConfirmation = false;
        submitted = false;
        SetButtonsInteractable(true);

        if (request == null)
        {
            Debug.LogError("CampfirePanel received invalid data.", this);
            SetButtonsInteractable(false);
            return;
        }

        if (titleText != null)
        {
            titleText.text = "篝火";
        }

        if (descriptionText != null)
        {
            int current = request.NumberResource != null
                ? request.NumberResource.CurrentValue
                : 0;
            int maximum = request.NumberResource != null
                ? request.NumberResource.MaximumValue
                : 0;
            descriptionText.text =
                $"休息后恢复 {request.MinimumRestore}～" +
                $"{request.MaximumRestore}\n当前数字：{current}/{maximum}";
        }

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        if (restButtonText != null)
        {
            restButtonText.text = "休息";
        }
    }

    public override void OnClose()
    {
        request = null;
        awaitingFullNumberConfirmation = false;
        submitted = false;
    }

    private void HandleRest()
    {
        if (request == null || submitted)
        {
            return;
        }

        NumberResource resource = request.NumberResource;
        bool isAtMaximum =
            resource != null &&
            resource.CurrentValue >= resource.MaximumValue;
        if (isAtMaximum && !awaitingFullNumberConfirmation)
        {
            awaitingFullNumberConfirmation = true;
            if (feedbackText != null)
            {
                feedbackText.text = "数字已满，仍要消耗这处篝火吗？";
            }

            if (restButtonText != null)
            {
                restButtonText.text = "仍要休息";
            }

            return;
        }

        submitted = true;
        SetButtonsInteractable(false);
        request.Rest?.Invoke();
    }

    private void HandleLeave()
    {
        if (request == null || submitted)
        {
            return;
        }

        submitted = true;
        SetButtonsInteractable(false);
        request.Leave?.Invoke();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (restButton != null)
        {
            restButton.interactable = interactable;
        }

        if (leaveButton != null)
        {
            leaveButton.interactable = interactable;
        }
    }

    private void OnDestroy()
    {
        restButton?.onClick.RemoveListener(HandleRest);
        leaveButton?.onClick.RemoveListener(HandleLeave);
    }
}
