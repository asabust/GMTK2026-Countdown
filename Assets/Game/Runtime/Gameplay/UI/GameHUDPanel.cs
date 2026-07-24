using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUDPanel : UIPanel
{
    [Header("Number")]
    [SerializeField] private TMP_Text currentNumberText;
    [SerializeField] private RectTransform deltaPopupRoot;
    [SerializeField] private NumberDeltaPopup numberDeltaPopupPrefab;
    [SerializeField] private Vector3 popupWorldOffset = new(0f, 1.1f, 0f);

    [Header("Controls")]
    [SerializeField] private Button settingsButton;

    private readonly Queue<NumberDeltaPopup> popupPool = new();
    private readonly List<NumberDeltaPopup> activePopups = new();
    private NumberResource numberResource;
    private Canvas parentCanvas;

    public event Action SettingsRequested;

    public override void OnInit()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (deltaPopupRoot == null)
        {
            deltaPopupRoot = transform as RectTransform;
        }

        settingsButton?.onClick.AddListener(HandleSettingsClicked);
    }

    public override void OnOpen(object data = null)
    {
        BindNumberResource(NumberResource.Instance);
    }

    public override void OnClose()
    {
        BindNumberResource(null);
        RecycleAllPopups();
    }

    private void OnDestroy()
    {
        settingsButton?.onClick.RemoveListener(HandleSettingsClicked);
        BindNumberResource(null);
    }

    private void BindNumberResource(NumberResource resource)
    {
        if (numberResource != null)
        {
            numberResource.Changed -= HandleNumberChanged;
        }

        numberResource = resource;
        if (numberResource == null)
        {
            return;
        }

        numberResource.Changed += HandleNumberChanged;
        RefreshNumber(numberResource.CurrentValue);
    }

    private void HandleNumberChanged(NumberChange change)
    {
        RefreshNumber(change.CurrentValue);
        if (change.Delta != 0)
        {
            ShowDelta(change);
        }
    }

    private void RefreshNumber(int value)
    {
        if (currentNumberText != null)
        {
            currentNumberText.text = value.ToString();
        }
    }

    private void ShowDelta(NumberChange change)
    {
        if (numberDeltaPopupPrefab == null || deltaPopupRoot == null)
        {
            return;
        }

        NumberDeltaPopup popup = popupPool.Count > 0
            ? popupPool.Dequeue()
            : Instantiate(numberDeltaPopupPrefab, deltaPopupRoot);

        activePopups.Add(popup);
        popup.Play(
            change.Delta,
            WorldToPopupPosition(change.WorldPosition + popupWorldOffset),
            RecyclePopup
        );
    }

    private Vector2 WorldToPopupPosition(Vector3 worldPosition)
    {
        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            return Vector2.zero;
        }

        Vector2 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        Camera uiCamera = parentCanvas != null &&
                          parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            deltaPopupRoot,
            screenPosition,
            uiCamera,
            out Vector2 localPosition
        )
            ? localPosition
            : Vector2.zero;
    }

    private void RecyclePopup(NumberDeltaPopup popup)
    {
        if (popup == null)
        {
            return;
        }

        activePopups.Remove(popup);
        popup.gameObject.SetActive(false);
        popupPool.Enqueue(popup);
    }

    private void RecycleAllPopups()
    {
        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            NumberDeltaPopup popup = activePopups[i];
            if (popup == null)
            {
                continue;
            }

            popup.StopAndHide();
            popupPool.Enqueue(popup);
        }

        activePopups.Clear();
    }

    private void HandleSettingsClicked()
    {
        SettingsRequested?.Invoke();
    }
}
