using System;
using System.Linq;
using System.Threading.Tasks;
using BlazorEdgeNewTab.Constants;
using BlazorEdgeNewTab.Models;
using BlazorEdgeNewTab.Services;
using Microsoft.AspNetCore.Components;
using WebExtensions.Net.Bookmarks;

namespace BlazorEdgeNewTab.Pages;

public partial class EmptyQuickLinkComponent
{
    [Parameter]
    public EventCallback OnSaveEventCallback { get; set; }
    [Inject]
    public SettingsService SettingsService { get; set; }

    public string NewQuickLinkTitle { get; set; }
    public string NewQuickLinkUrl   { get; set; }

    public bool IsDisabled
    {
        get
        {
            if (string.IsNullOrWhiteSpace(NewQuickLinkTitle) || string.IsNullOrWhiteSpace(NewQuickLinkUrl))
                return true;
            return !Uri.IsWellFormedUriString(NewQuickLinkUrl, UriKind.Absolute);
        }
    }

    private async Task SaveNewBookmark()
    {
        var bookMarkNode =
            await WebExtensions.Bookmarks.Search(SettingsService.GetSettingsValue<string>(SettingsValues.QuickLinkBookMarkSetting));
        var bookmarkTreeNodes = bookMarkNode.ToList();
        if (bookmarkTreeNodes.Count() == 1)
        {
            await WebExtensions.Bookmarks.Create(new CreateDetails
            {
                Title = NewQuickLinkTitle,
                Url = NewQuickLinkUrl,
                ParentId = bookmarkTreeNodes.First().Id
            });
            NewQuickLinkTitle = string.Empty;
            NewQuickLinkUrl = string.Empty;
            await OnSaveEventCallback.InvokeAsync();
        }
    }
}