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

    public string QuickLinksFolder
    {
        get => SettingsService.GetSettingsValue<string>(SettingsValues.QuickLinkBookMarkSetting);
        set
        {
            var currentFolderName = SettingsService.GetSettingsValue<string>(SettingsValues.QuickLinkBookMarkSetting);
            if (string.IsNullOrWhiteSpace(value) || currentFolderName == value) return;
            Console.WriteLine($"Setting QuickLinks Folder To {value}");
            SettingsService.UpdateSetting(SettingsValues.QuickLinkBookMarkSetting, value);
            SettingsService.SaveAsync();
            OnQuickLinksFolderChangedCallBack.InvokeAsync();
        }
    }

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