using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorEdgeNewTab.Constants;
using BlazorEdgeNewTab.Models;
using WebExtensions.Net;
using WebExtensions.Net.Storage;

namespace BlazorEdgeNewTab.Services;

public class SettingsService
{
    private Dictionary<string, string> SettingsDictionary = SettingsValues.InitializeSettingsDictionary();

    public StorageArea Storage { get; set; }


    /// <summary>
    /// Resets all application settings to their default values.
    /// </summary>
    /// <remarks>Call this method to restore the original configuration of the application. This is useful if
    /// settings have been modified during runtime and need to be reverted to their initial state.</remarks>
    public void ResetSettings()
    {
        SettingsDictionary = SettingsValues.InitializeSettingsDictionary();
    }

    /// <summary>
    /// Gets the setting value indicated by the provided key. Returns the default value if the setting is not found
    /// </summary>
    /// <typeparam name="T">Type of the setting value</typeparam>
    /// <param name="settingValue">The setting value to retrieve</param>
    /// <returns>The actual stored setting value. If the setting is not found, returns the default value</returns>
    public T GetSettingsValue<T>(SettingsValue settingValue)
    {
        var foundSetting = SettingsDictionary.GetValueOrDefault(settingValue.Key);
        if (foundSetting == null)
            return (T)Convert.ChangeType(settingValue.Value, typeof(T));
        return (T)Convert.ChangeType(foundSetting, typeof(T));
    }

    /// <summary>
    ///     Updates the setting indicated by the provided key with the provided value
    /// </summary>
    /// <param name="settingValue"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void UpdateSetting(SettingsValue settingValue, object value)
    {
        if (settingValue != null)
            SettingsDictionary[settingValue.Key] = value.ToString();
        else
            throw new ArgumentNullException(nameof(settingValue), "Setting value cannot be null");
    }

    /// <summary>
    ///     Saves the settings dictionary back to the browser locals storage
    /// </summary>
    /// <returns>A completed ValueTask object</returns>
    public ValueTask SaveAsync()
    {
        Console.WriteLine("Saving settings...");
        return Storage?.Set(SettingsDictionary) ?? ValueTask.CompletedTask;
    }    

    /// <summary>
    ///     Loads the settings dictionary for the settings class from the browser storage
    /// </summary>
    /// <param name="webApi"></param>
    /// <returns></returns>
    public async Task LoadSettingsAsync(IWebExtensionsApi webApi)
    {
        Console.WriteLine("Loading settings...");
        Storage ??= webApi.Storage.Local;
        var jsonElement = await Storage.Get(SettingsValues.StorageKeys);
        if (jsonElement.ToString() != "{}")
        {
            Console.WriteLine("Settings found");
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonElement.GetRawText());
            UpdateDictionary(dictionary);
        }
        else
        {
            Console.WriteLine("Settings do not exist exist, setting and saving defaults....");
            await SaveAsync();
        }
    }
    /// <summary>
    /// Updates the internal settings dictionary with the provided key value pairs
    /// </summary>
    /// <param name="keyValuePairs"></param>
    private void UpdateDictionary(Dictionary<string, string> keyValuePairs)
    {
        SettingsDictionary = keyValuePairs;
    }
}