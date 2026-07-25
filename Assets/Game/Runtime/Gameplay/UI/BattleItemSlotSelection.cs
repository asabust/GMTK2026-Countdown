using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BattleItemSlotSelection :
    MonoBehaviour,
    IPointerEnterHandler,
    ISelectHandler
{
    private Action selected;

    public void Bind(Action callback) => selected = callback;

    public void OnPointerEnter(PointerEventData eventData) => selected?.Invoke();

    public void OnSelect(BaseEventData eventData) => selected?.Invoke();
}
