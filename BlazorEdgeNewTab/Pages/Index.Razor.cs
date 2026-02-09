using BlazorEdgeNewTab.Constants;
using BlazorEdgeNewTab.Models;
using BlazorEdgeNewTab.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WebExtensions.Net.Bookmarks;
using WebExtensions.Net.Downloads;
using WebExtensions.Net.Search;

namespace BlazorEdgeNewTab.Pages;

public partial class Index
{
    //Bing Image of the Day
    private List<BingImageOfTheDay> _bingImages;
    private bool                    _filterMode;
    private int                     _imageArchiveIndex = -1;
    private BingImageOfTheDay       _imageOfTheDay;
    //Quick Links
    private List<QuickLink>         QuickLinks        { get; set; }
    private bool                    QuickLinksVisible { get; set; }
    //Setup Modal
    private bool         ShowSetupModal                 { get; set; }
    private string       NewQuickLinkBookMarkFolderName { get; set; }
    private List<string> BookmarkFolders                { get; set; } = new List<string>();
    //Search Box
    private bool             SearchVisible;
    private ElementReference SearchBoxElement;
    private string           SearchQuery       { get; set; }
    //Download Image URL
    private string                  ImageDownloadLink { get; set; }
    //Museum Card
    private string                  MuseumCardText    { get; set; }
    private string                  MuseumCardText2   { get; set; }
    private string                  MuseumLink        { get; set; }
    private string                  MuseumLink2       { get; set; }
    private bool FilterMode
    {
        get => _filterMode;
        set
        {
            _filterMode = value;
            UpdateSearchEmptyText();
            SettingsService.UpdateSetting(SettingsValues.UseQuickLinksFilterSetting, value.ToString());
            SettingsService.SaveAsync();
        }
    }

    private string SearchEmptyText    { get; set; } = "Search the web...";
    public  string DraggedQuickLinkId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        //NOTE - Do NOT get settings until the quick links are set up. The elements need to exist in the dom first or the
        //QuickLinksVisible property will cause a nasty exception because it's trying to work on an HTML element that isn't drawn yet.
        await SettingsService.LoadSettingsAsync(WebExtensions).ContinueWith(_ => SetUpQuickLinks());
        await GetBingImage();
        _filterMode = Convert.ToBoolean(SettingsService.GetSettingsValue<bool>(SettingsValues.UseQuickLinksFilterSetting));
        SearchVisible = Convert.ToBoolean(SettingsService.GetSettingsValue<bool>(SettingsValues.ShowWebSearchSetting));
        QuickLinksVisible = Convert.ToBoolean(SettingsService.GetSettingsValue<bool>(SettingsValues.ShowQuickLinksSetting));
        if (_filterMode) UpdateSearchEmptyText();
        await JsRunTime.InvokeVoidAsync("SetFocusToElement", SearchBoxElement);
    }

    private void UpdateSearchEmptyText()
    {
        SearchEmptyText = _filterMode
            ? "Type in a filter and hit enter to apply it. Blank filter resets the filter."
            : "Search the web...";
    }

    private void FilterQuickLinks()
    {
        if (_filterMode)
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                foreach (var quickLink in QuickLinks)
                    quickLink.Visible =
                        quickLink.QuickLinkTitle.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);

                StateHasChanged();
            }
            else
            {
                QuickLinks.ForEach(x => x.Visible = true);
            }
        }
    }

    public async Task SetUpQuickLinks()
    {
        var bookMarkFolder = SettingsService.GetSettingsValue<string>(SettingsValues.QuickLinkBookMarkSetting);
        if (string.IsNullOrWhiteSpace(bookMarkFolder))
        {
            BookmarkFolders = await NewTabService.GetBookmarkFolders(WebExtensions);
            ShowSetupModal = true;
            StateHasChanged();
        }
        else
        {
            QuickLinks = await NewTabService.SetUpQuickLinks(WebExtensions);
            if (QuickLinks.Any())
            {
                StateHasChanged();
            }
        }
    }

    private async Task SetUpQuickLinksFolder()
    {
        if (string.IsNullOrWhiteSpace(NewQuickLinkBookMarkFolderName)) return;
        await NewTabService.CreateNewBookmarkFolder(WebExtensions, NewQuickLinkBookMarkFolderName);   
        await SetUpQuickLinks();
        ShowSetupModal = false;
        StateHasChanged();
    }



    private async Task GetBingImage()
    {
        try
        {
            var dto = await NewTabService.GetImageOfDayDto();
            if (dto != null)
            {
                _imageOfTheDay = dto.images[0];
                if (_imageOfTheDay != null)
                {
                    await ApplyImageOfTheDay(dto);
                    _bingImages = dto.images.Skip(1).Take(7).ToList();
                }
                else
                    Console.WriteLine(
                        "We did not set the image, there was no first image in the json that came back. ☹");
            }
            else
            {
                Console.WriteLine("The image collection was null, we probably screwed up deserializing the JSON ☹");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("We got an error. 😡");
            Console.WriteLine(e);
        }
    }

    private async Task ApplyImageOfTheDay(BingImageOfTheDayDto dto)
    {
        await SetBackgroundImage(_imageOfTheDay.url);
        UpdateMuseumCardForImageOfDay();
        StateHasChanged();
    }

    private async Task GetNextImage()
    {
        if (_imageArchiveIndex >= _bingImages.Count - 1)
        {
            _imageArchiveIndex = -1;
            await SetBackgroundImage(_imageOfTheDay.url);
            UpdateMuseumCardForImageOfDay();
        }
        else
        {
            _imageArchiveIndex++;
            await SetBackgroundImage(GetImageUrlForIndex(_imageArchiveIndex));
            UpdateMuseumCardForArchiveImage();
        }
    }

    private async Task GetPreviousImage()
    {
        switch (_imageArchiveIndex)
        {
            case -1:
                _imageArchiveIndex = _bingImages.Count - 1;
                await SetBackgroundImage(GetImageUrlForIndex(_imageArchiveIndex));
                UpdateMuseumCardForArchiveImage();
                break;
            case 0:
                _imageArchiveIndex = -1;
                await SetBackgroundImage(_imageOfTheDay.url);
                UpdateMuseumCardForImageOfDay();
                break;
            default:
                _imageArchiveIndex--;
                await SetBackgroundImage(GetImageUrlForIndex(_imageArchiveIndex));
                UpdateMuseumCardForArchiveImage();
                break;
        }
    }

    private void UpdateMuseumCardForImageOfDay()
    {
        MuseumCardText = _imageOfTheDay.title;
        MuseumLink = _imageOfTheDay.copyrightlink;
        MuseumCardText2 = _imageOfTheDay.copyright;
        MuseumLink2 = $"https://bing.com{_imageOfTheDay.quiz}";
    }

    private void UpdateMuseumCardForArchiveImage()
    {
        MuseumCardText = _bingImages[_imageArchiveIndex].title;
        MuseumLink = _bingImages[_imageArchiveIndex].copyrightlink;
        MuseumCardText2 = _bingImages[_imageArchiveIndex].copyright;
        MuseumLink2 = $"https://bing.com{_bingImages[_imageArchiveIndex].quiz}";
    }

    private string GetImageUrlForIndex(int index)
    {
        return _bingImages[index].url;
    }

    private string GetImageNameForIndex(int index)
    {
        return index < 0
            ? $"BingImageOfTheDay_{_imageOfTheDay.startdate}"
            : $"BingImageOfTheDay_{_bingImages[index].startdate}";
    }

    private async Task SetBackgroundImage(string url)
    {
        var fullImagePath = "https://bing.com" + url;
        await JsRunTime.InvokeVoidAsync("setBackgroundImage1", fullImagePath);
        ImageDownloadLink = fullImagePath.Replace("&pid=hp", "");
    }

    private void Enter()
    {
        if (_filterMode)
        {
            FilterQuickLinks();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery)) FireSearchEngineQuery();
        }
    }

    private Task FireSearchEngineQuery()
    {
        try
        {
            if (_filterMode)
            {
                FilterQuickLinks();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(SearchQuery)) return Task.CompletedTask;
                Console.WriteLine($"Performing a search with query : {SearchQuery}");
                WebExtensions.Search.Query(new QueryInfo
                    {Text = SearchQuery, Disposition = Disposition.CURRENTTAB, AdditionalData = null, TabId = null});
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("We had an error doing the search query.");
            Console.WriteLine(e);
        }

        return Task.CompletedTask;
    }
    
    
    private Task UseCopilot(MouseEventArgs arg)
    {
        Console.WriteLine("Use Co-Pilot Clicked");
        NavigationManager.NavigateTo("https://copilot.microsoft.com/");
        return Task.CompletedTask;
    }

    private async Task OnDownloadImageClick() => await WebExtensions.Downloads.Download(new DownloadOptions
    {
        Url = ImageDownloadLink,
        SaveAs = true,
        Filename = $"{GetImageNameForIndex(_imageArchiveIndex)}.jpeg"
    });

    private async Task EditQuickLinkFolder()
    {
        ShowSetupModal = true;
        BookmarkFolders = await NewTabService.GetBookmarkFolders(WebExtensions);
        NewQuickLinkBookMarkFolderName =  SettingsService.GetSettingsValue<string>(SettingsValues.QuickLinkBookMarkSetting);
        await InvokeAsync(StateHasChanged);
    }

    public async Task EditQuickLink(string quickLinkId, string newTitle, string newUrl)
    {
        await WebExtensions.Bookmarks.Update(quickLinkId, new Changes { Title = newTitle, Url = newUrl });
        await SetUpQuickLinks();
    }

    public async Task OnQuickLinkDragged(string droppedQuickLinkId, string destinationQuickLinkId)
    {
        await NewTabService.SwapQuickLinks(WebExtensions, droppedQuickLinkId, destinationQuickLinkId);
        await SetUpQuickLinks();
    }

    public async Task DeleteQuickLink(string quickLinkId)
    {
        await WebExtensions.Bookmarks.Remove(quickLinkId);
        await SetUpQuickLinks();
    }

    private void SearchVisibleChanged(bool visible)
    {
        SearchVisible = visible;
        SettingsService.UpdateSetting(SettingsValues.ShowWebSearchSetting, visible);
        SettingsService.SaveAsync();
    }

    private void QuickLinksVisibleChanged(bool value)
    {
        QuickLinksVisible = value;
        SettingsService.UpdateSetting(SettingsValues.ShowQuickLinksSetting, value);
        SettingsService.SaveAsync();
    }

    private async Task RefreshImageOfDayHandler()
    {
        SettingsService.UpdateSetting(SettingsValues.ReQueryImagesAfterTimeSetting,
            DateTime.Now.AddDays(-1).ToString(CultureInfo.InvariantCulture));
        await SettingsService.SaveAsync();
        await GetBingImage();
        StateHasChanged();
    }

    private async Task ResetSettings()
    {
        SettingsService.ResetSettings();
        await SettingsService.SaveAsync();
        NavigationManager.Refresh();

    }
}