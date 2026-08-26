using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Clicks de UI (botones y pestañas) y música del menú principal.
public class UIAudioFeedback : MonoBehaviour
{
    [SerializeField] private Button[] buttons = Array.Empty<Button>();

    [Tooltip("Opcional: grupo de pestañas cuyos clicks sonarán.")]
    [SerializeField] private TabGroup tabGroup;

    [Header("Música")]
    [Tooltip("Reproduce MenuTheme en loop mientras esta escena esté activa.")]
    [SerializeField] private bool playMenuTheme = false;

    private readonly HashSet<Button> subscribedButtons = new HashSet<Button>();
    private readonly List<CustomTabButton> subscribedTabs = new List<CustomTabButton>();

    private void OnEnable()
    {
        SubscribeButtons();
        SubscribeTabs();
    }

    private void Start()
    {
        if (playMenuTheme)
            AudioManager.PlayLoop(AudioType.MenuTheme);
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
        UnsubscribeTabs();

        if (playMenuTheme)
            AudioManager.StopLoop(AudioType.MenuTheme);
    }

    private void SubscribeButtons()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null || !subscribedButtons.Add(button))
                continue;

            button.onClick.AddListener(HandleButtonClick);
        }
    }

    private void SubscribeTabs()
    {
        if (tabGroup == null)
            return;

        List<CustomTabButton> tabs = tabGroup.tabs;

        for (int i = 0; i < tabs.Count; i++)
        {
            CustomTabButton tab = tabs[i];

            if (tab == null || subscribedTabs.Contains(tab))
                continue;

            tab.OnTabClicked += HandleTabClicked;
            subscribedTabs.Add(tab);
        }
    }

    private void UnsubscribeButtons()
    {
        foreach (Button button in subscribedButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(HandleButtonClick);
        }

        subscribedButtons.Clear();
    }

    private void UnsubscribeTabs()
    {
        for (int i = 0; i < subscribedTabs.Count; i++)
        {
            CustomTabButton tab = subscribedTabs[i];

            if (tab != null)
                tab.OnTabClicked -= HandleTabClicked;
        }

        subscribedTabs.Clear();
    }

    private void HandleButtonClick()
    {
        AudioManager.Play(AudioType.ButtonClick);
    }

    private void HandleTabClicked(CustomTabButton tab)
    {
        AudioManager.Play(AudioType.ButtonClick);
    }
}
