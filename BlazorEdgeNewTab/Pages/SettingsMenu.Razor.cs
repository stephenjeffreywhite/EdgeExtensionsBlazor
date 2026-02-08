using System;
using BlazorEdgeNewTab.Constants;
using BlazorEdgeNewTab.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorEdgeNewTab.Pages;

public partial class SettingsMenu
{
    [Parameter]
    public EventCallback OnQuickLinksFolderChangedCallBack { get; set; }

    [Parameter]
    public EventCallback OnRefreshImageOfDayCallBack { get; set; }


    [Parameter]
    public bool QuickLinksVisible { get; set; }
    [Parameter]
    public EventCallback<bool> QuickLinksVisibleChanged { get; set; }


    [Parameter]
    public bool SearchVisible { get; set; }
    [Parameter]
    public EventCallback<bool> SearchVisibleChanged { get; set; }

    private void ResetSettings()
    {
        SettingsService.ResetSettings();
        SettingsService.SaveAsync();
    }

    private void RefreshImageOfDay()
    {
        OnRefreshImageOfDayCallBack.InvokeAsync();
    }
}