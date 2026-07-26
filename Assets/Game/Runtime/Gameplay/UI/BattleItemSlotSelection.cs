using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BattleItemSlotSelection :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler
{
    private Action selected;
    private Action deselected;

    public void Bind(Action onSelected, Action onDeselected = null)
    {
        selected = onSelected;
        deselected = onDeselected;
    }

    public void OnPointerEnter(PointerEventData eventData) => selected?.Invoke();

    public void OnPointerExit(PointerEventData eventData) =>
        deselected?.Invoke();

    public void OnSelect(BaseEventData eventData) => selected?.Invoke();

    private void OnDisable() => deselected?.Invoke();
}
