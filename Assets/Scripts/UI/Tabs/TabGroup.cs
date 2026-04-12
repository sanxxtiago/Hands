using System;
using System.Collections.Generic;
using UnityEngine;

public class TabGroup : MonoBehaviour
{

    public List<CustomTabButton> tabs = new List<CustomTabButton>();
    public List<GameObject> contents = new List<GameObject>();
    private CustomTabButton currentSelectedTab;
    void OnEnable()
    {
        foreach (var tab in tabs)
        {
            tab.OnTabClicked += HandleOnTabSelected;
        }
    }
    void OnDisable()
    {
        foreach (var tab in tabs)
        {
            tab.OnTabClicked -= HandleOnTabSelected;
        }
    }
    void Awake()
    {
        if (tabs.Count != contents.Count)
        {
            Debug.LogError("Tabs y contents deben tener el mismo tamaño");
        }
    }
    void Start()
    {
        SelectTab(tabs[0]);
    }

    public void HandleOnTabSelected(CustomTabButton selectedTab)
    {
        if (currentSelectedTab == selectedTab) return;

        SelectTab(selectedTab);
    }

    void SelectTab(CustomTabButton selectedTab)
    {
        currentSelectedTab = selectedTab;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isSelected = tabs[i] == selectedTab;

            contents[i].SetActive(isSelected);
            tabs[i].SetSelected(isSelected);

            if (isSelected)
            {
                var resultUIs = contents[i].GetComponentsInChildren<ArmResultUI>(true);

                foreach (var resultUI in resultUIs)
                {
                    resultUI.PlayAnimation();
                }
            }
        }
    }
}
