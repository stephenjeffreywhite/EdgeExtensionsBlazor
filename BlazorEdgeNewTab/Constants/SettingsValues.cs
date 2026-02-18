using System;
using System.Collections.Generic;
using System.Globalization;
using BlazorEdgeNewTab.Models;
using WebExtensions.Net.Storage;

namespace BlazorEdgeNewTab.Constants;

/// <summary>
/// Settings values and their default values for the application. The settings values are used as keys for storing and retrieving settings from the extension storage.
/// </summary>
public static class SettingsValues
{
    // Suggested Defaults
    public const string DefaultBookmarkFolderName = "Edge Quick Links";
    // Keys for settings values
    private const string SearchRegion              = "SearchRegion";
    private const string DisplayMode               = "DisplayMode";
    private const string ShowWebSearch             = "ShowWebSearch";
    private const string ShowQuickLinks            = "ShowQuickLinks";
    private const string ShowRandomImages          = "ShowRandomImages";
    private const string QuickLinkBookMarkFolder   = "QuickLinkBookmarkFolder";
    private const string ImageOfTheDayCache        = "ImageOfTheDayCache";
    private const string ReQueryImagesAfterTime    = "ReQueryImagesAfterTime";
    private const string UseQuickLinksFilter       = "UseQuickLinksFilter";
    private const string UsePeaPix                 = "UsePeaPix";
    //Settings Values with default values
    public static readonly SettingsValue SearchRegionSetting       = new(SearchRegion, "US");
    public static readonly SettingsValue DisplayModeSetting        = new(DisplayMode, "1");
    public static readonly SettingsValue ShowWebSearchSetting      = new(ShowWebSearch, "true");
    public static readonly SettingsValue ShowQuickLinksSetting     = new(ShowQuickLinks, "true");
    public static readonly SettingsValue ShowRandomImagesSetting   = new(ShowRandomImages, "false");
    public static readonly SettingsValue QuickLinkBookMarkSetting  = new(QuickLinkBookMarkFolder, null);
    public static readonly SettingsValue ImageOfTheDayCacheSetting = new(ImageOfTheDayCache, null);
    public static readonly SettingsValue ReQueryImagesAfterTimeSetting =
        new(ReQueryImagesAfterTime, DateTime.Now.ToString(CultureInfo.InvariantCulture));
    public static readonly SettingsValue UseQuickLinksFilterSetting = new(UseQuickLinksFilter, "false");
    public static readonly SettingsValue UsePeaPixSetting = new(UsePeaPix, "true");

    /// <summary>
    /// The list of storage keys used for retrieving settings from storage
    /// </summary>
    public static readonly StorageAreaGetKeys StorageKeys = new(
        new List<string>
        {
            SearchRegionSetting.Key,
            DisplayModeSetting.Key,
            ShowWebSearchSetting.Key,
            ShowQuickLinksSetting.Key,
            ShowRandomImagesSetting.Key,
            QuickLinkBookMarkSetting.Key,
            ImageOfTheDayCacheSetting.Key,
            ReQueryImagesAfterTimeSetting.Key,
            UseQuickLinksFilterSetting.Key,
            UsePeaPixSetting.Key,
        }
    );
    /// <summary>
    /// Initializes the settings dictionary with default values
    /// </summary>
    /// <returns>A dictionary containing the default settings key-value pairs</returns>
    public static Dictionary<string, string> InitializeSettingsDictionary()
    {
        return new Dictionary<string, string>
        {
            { SettingsValues.SearchRegionSetting.Key, SettingsValues.SearchRegionSetting.Value },
            { SettingsValues.DisplayModeSetting.Key, SettingsValues.DisplayModeSetting.Value },
            { SettingsValues.ShowWebSearchSetting.Key, SettingsValues.ShowWebSearchSetting.Value },
            { SettingsValues.ShowQuickLinksSetting.Key, SettingsValues.ShowQuickLinksSetting.Value },
            { SettingsValues.ShowRandomImagesSetting.Key, SettingsValues.ShowRandomImagesSetting.Value },
            { SettingsValues.QuickLinkBookMarkSetting.Key, SettingsValues.QuickLinkBookMarkSetting.Value },
            { SettingsValues.ImageOfTheDayCacheSetting.Key, SettingsValues.ImageOfTheDayCacheSetting.Value },
            { SettingsValues.ReQueryImagesAfterTimeSetting.Key, SettingsValues.ReQueryImagesAfterTimeSetting.Value },
            { SettingsValues.UseQuickLinksFilterSetting.Key, SettingsValues.UseQuickLinksFilterSetting.Value },
            { SettingsValues.UsePeaPixSetting.Key, SettingsValues.UsePeaPixSetting.Value },
        };
    }

}