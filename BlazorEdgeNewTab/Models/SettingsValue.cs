namespace BlazorEdgeNewTab.Models
{
    /// <summary>
    /// A settings value that is used to store key-value pairs for application settings.
    /// </summary>
    /// <param name="Key">The key of the setting</param>
    /// <param name="Value">The value of the setting. This will be used for the default value if the setting is not found in storage.</param>
    public record SettingsValue(string Key, string Value);
}
