using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BlazorEdgeNewTab.Constants;
using BlazorEdgeNewTab.Models;
using WebExtensions.Net;
using WebExtensions.Net.Bookmarks;

namespace BlazorEdgeNewTab.Services;

public class NewTabService
{
    private readonly string PeaPixUri =
        "https://peapix.com/bing/feed?country=us&n=7";
    private readonly string BingUri =
        "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=7&mkt=en-US";

    private HttpClient _httpClient;
    public SettingsService SettingsService { get; }
    public NewTabService(SettingsService settingsService)
    {
        SettingsService = settingsService;
    }

    public async Task<BingImageOfTheDayDto?> GetImageOfDayDto(CancellationToken ct)
    {
        var cachedDto = GetCachedImageOfDayDto();
        var reQueryTimeString =
            SettingsService.GetSettingsValue<string>(SettingsValues.ReQueryImagesAfterTimeSetting);
        var hasFetchTime = DateTime.TryParse(reQueryTimeString, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var lastFetchTime);

        if (cachedDto != null && hasFetchTime && lastFetchTime.Date == DateTime.Now.Date)
        {
            Console.WriteLine("Using cached Bing Image Of Day.");
            return cachedDto;
        }

        try
        {
            var usePeaPix = SettingsService.GetSettingsValue<bool>(SettingsValues.UsePeaPixSetting);
            var dto = usePeaPix
                ? await GetBingImagesFromPeaPix(ct)
                : await GetBingImages(ct);

            if (!HasUsableImages(dto))
                throw new JsonException("The image feed contained no usable images.");

            // Refresh scheduling follows the successful fetch, not the image publication date.
            SettingsService.UpdateSetting(SettingsValues.ImageOfTheDayCacheSetting, JsonSerializer.Serialize(dto));
            SettingsService.UpdateSetting(SettingsValues.ReQueryImagesAfterTimeSetting,
                DateTime.Now.ToString(CultureInfo.InvariantCulture));
            await SettingsService.SaveAsync();
            return dto;
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine($"Bing image fetch canceled or timed out: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to refresh Bing images: {ex}");
        }

        // A stale feed is preferable to losing the background during an outage.
        // Do not advance the refresh time after a failed request.
        return cachedDto;
    }

    private BingImageOfTheDayDto? GetCachedImageOfDayDto()
    {
        var cachedJson = SettingsService.GetSettingsValue<string>(SettingsValues.ImageOfTheDayCacheSetting);
        if (string.IsNullOrWhiteSpace(cachedJson)) return null;

        try
        {
            var dto = JsonSerializer.Deserialize<BingImageOfTheDayDto>(cachedJson);
            return HasUsableImages(dto) ? dto : null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Cached Bing JSON was invalid: {ex.Message}");
            return null;
        }
    }

    private static bool HasUsableImages(BingImageOfTheDayDto? dto) =>
        dto?.images != null && dto.images.Count > 0 &&
        dto.images.All(image => image != null && !string.IsNullOrWhiteSpace(image.url));

    private async Task<BingImageOfTheDayDto?> GetBingImages(CancellationToken ct)
    {
        Console.WriteLine($"Getting Bing images directly: {BingUri}");
        _httpClient ??= new HttpClient();
        return await _httpClient.GetFromJsonAsync<BingImageOfTheDayDto>(BingUri, cancellationToken: ct);
    }

    private async Task<BingImageOfTheDayDto> GetBingImagesFromPeaPix(CancellationToken ct)
    {
        BingImageOfTheDayDto dto = new();
        _httpClient ??= new HttpClient();
        var responseDto = await _httpClient.GetFromJsonAsync<List<BingImageOfDayPeapixDto>>(PeaPixUri, cancellationToken: ct);
        if (responseDto != null)
        {
            foreach (var image in responseDto)
            {
                if (image == null)
                    throw new JsonException("PeaPix returned a null image entry.");
                dto.images.Add(BingImageOfTheDay.FromPeaPixDto(image));
            }
        }
        return dto;
    }

    public async Task<List<string>> GetBookmarkFolders(IWebExtensionsApi webExtensions)
    {
        var bookmarkTreeNodes  = await webExtensions.Bookmarks.GetTree() as List<BookmarkTreeNode>;
        var bookMarkFolderlist = new List<string>();        

        if (bookmarkTreeNodes != null && bookmarkTreeNodes.Any())
        {
            foreach (var bookmarkTreeNode in bookmarkTreeNodes)
            {
                CollectBookmarkFoldersRecursively(bookmarkTreeNode, bookMarkFolderlist);
            }
        }

        return bookMarkFolderlist;
    }

    public async Task CreateNewBookmarkFolder(IWebExtensionsApi webExtensions, string folderName)
    {
        var bookmarkFolders = await GetBookmarkFolders(webExtensions);
        if (!bookmarkFolders.Contains(folderName))
        {
            await webExtensions.Bookmarks.Create(new CreateDetails { Title = folderName });
        }
        SettingsService.UpdateSetting(SettingsValues.QuickLinkBookMarkSetting, folderName);
        await SettingsService.SaveAsync();
    }

    private void CollectBookmarkFoldersRecursively(BookmarkTreeNode node, List<string> folderList)
    {
        if (string.IsNullOrWhiteSpace(node.Url) && !string.IsNullOrWhiteSpace(node.Title))
        {
            folderList.Add(node.Title);
        }

        if (node.Children != null && node.Children.Any())
        {
            foreach (var child in node.Children)
            {
                CollectBookmarkFoldersRecursively(child, folderList);
            }
        }
    }

    public async Task<List<QuickLink>> SetUpQuickLinks(IWebExtensionsApi webExtensions)
    {
        var quickLinks         = new List<QuickLink>();
        var extensionId        = webExtensions.Runtime.Id;
        var bookmarkTreeNodes  = await GetBookmarkTreeNodes(webExtensions);
        if (!bookmarkTreeNodes.Any()) return quickLinks;
        var quickLinkBookMarks = await webExtensions.Bookmarks.GetChildren(bookmarkTreeNodes.First().Id);
        quickLinks.AddRange(quickLinkBookMarks.Select(quickLinkBookMark => new QuickLink
        {
            QuickLinkTitle = quickLinkBookMark.Title,
            QuickLinkImageUrl = GetFaviconUrl(extensionId, quickLinkBookMark.Url),
            QuickLinkUrl = quickLinkBookMark.Url,
            QuickLinkId = quickLinkBookMark.Id,
            Visible = true
        }));

        return quickLinks;
    }

    public async Task SwapQuickLinks(IWebExtensionsApi webExtensions, string droppedQuickLinkId, string destinationQuickLinkId)
    {
        var parent             = await GetBookmarkTreeNodes(webExtensions);
        var quickLinkBookMarks = await webExtensions.Bookmarks.GetChildren(parent.First().Id);
        if (quickLinkBookMarks == null)
        {
            return;
        }
        var quickLink1 = quickLinkBookMarks.FirstOrDefault(node => node.Id == droppedQuickLinkId);
        var quickLink2 = quickLinkBookMarks.FirstOrDefault(node => node.Id == destinationQuickLinkId);
        if (quickLink1 != null && quickLink2 != null)
        {
            await webExtensions.Bookmarks.Move(droppedQuickLinkId, new Destination() { Index = quickLink2.Index });
            await webExtensions.Bookmarks.Move(destinationQuickLinkId, new Destination() { Index = quickLink1.Index });
        }
    }

    private string GetFaviconUrl(string extensionId, string quickLinkUrl)
    {
        var encodedQuickLinkUrl = UrlEncoder.Default.Encode(quickLinkUrl);
        var url                 = $"chrome-extension://{extensionId}/_favicon/?pageUrl={encodedQuickLinkUrl}&size=64";
        return url;
    }
    private async Task<List<BookmarkTreeNode>> GetBookmarkTreeNodes(IWebExtensionsApi webExtensions)
    {
        var bookMarkNode =
            await webExtensions.Bookmarks.Search(SettingsService.GetSettingsValue<string>(SettingsValues.QuickLinkBookMarkSetting));
        if (bookMarkNode == null) return [];
        var bookmarkTreeNodes = bookMarkNode.ToList();
        return bookmarkTreeNodes;
    }
}