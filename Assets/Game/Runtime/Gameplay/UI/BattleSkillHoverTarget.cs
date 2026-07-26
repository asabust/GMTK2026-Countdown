using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BattleSkillHoverTarget :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private Action entered;
    private Action exited;

    public void Bind(Action onEntered, Action onExited)
    {
        entered = onEntered;
        exited = onExited;
    }

    public void OnPointerEnter(PointerEventData eventData) =>
        entered?.Invoke();

    public void OnPointerExit(PointerEventData eventData) =>
        exited?.Invoke();

    private void OnDisable() => exited?.Invoke();
}
