using System;
using BlazorEdgeNewTab.Constants;
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
        get => Settings.GetSettingValue(SettingsValues.QuickLinkBookMarkFolder);
        set
        {
            var currentFolderName = Settings.GetSettingValue(SettingsValues.QuickLinkBookMarkFolder);
            if (string.IsNullOrWhiteSpace(value) || currentFolderName == value) return;
            Console.WriteLine($"Setting QuickLinks Folder To {value}");
            Settings.UpdateSetting(SettingsValues.QuickLinkBookMarkFolder, value);
            Settings.SaveAsync();
            OnQuickLinksFolderChangedCallBack.InvokeAsync();
        }
    }

    private void ResetSettings()
    {
        Settings.ResetSettings();
        Settings.SaveAsync();
    }

    private void RefreshImageOfDay()
    {
        OnRefreshImageOfDayCallBack.InvokeAsync();
    }
}