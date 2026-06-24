using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CustomTabButton : MonoBehaviour, IPointerClickHandler
{
    public event Action<CustomTabButton> OnTabClicked;
    public event Action<CustomTabButton> OnTabSelected;


    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Tab Clicked");
        OnTabClicked?.Invoke(this);
    }

    public void SetSelected(bool value)
    {
        // cambiar color, escala, etc
    }

}
