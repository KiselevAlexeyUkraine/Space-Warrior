using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool IsPressed { get; private set; }

    public void OnPointerDown(PointerEventData eventData) => IsPressed = true;
    
    public void OnPointerUp(PointerEventData eventData) => IsPressed = false;

    // Если палец ушел с кнопки в сторону - сбрасываем спринт
    public void OnPointerExit(PointerEventData eventData) => IsPressed = false;
    
    private void OnDisable() => IsPressed = false;
}