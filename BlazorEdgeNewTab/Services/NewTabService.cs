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
    private readonly string AllOriginsUri =
        "https://api.allorigins.win/get?url=https%3A//www.bing.com/HPImageArchive.aspx%3Fformat%3Djs%26idx%3D0%26n%3D7";

    private HttpClient _httpClient;
    public SettingsService SettingsService { get; }
    public NewTabService(SettingsService settingsService)
    {
        SettingsService = settingsService;
    }

    public async Task<BingImageOfTheDayDto?> GetImageOfDayDto(CancellationToken ct)
    {
        var reQueryTimeString =
            SettingsService.GetSettingsValue<string>(SettingsValues.ReQueryImagesAfterTimeSetting);

        var reQueryTime =
            Convert.ToDateTime(reQueryTimeString ?? DateTime.Now.AddDays(-1).ToString(CultureInfo.InvariantCulture));

        try
        {
            var cachedJson = SettingsService.GetSettingsValue<string>(SettingsValues.ImageOfTheDayCacheSetting);

            var shouldFetch =
                DateTime.Now.Date > reQueryTime.Date || string.IsNullOrWhiteSpace(cachedJson);

            if (shouldFetch)
            {
                Console.WriteLine("Getting Bing Image of Day from Bing");

                // IMPORTANT: pass ct down into the actual HTTP call
                var usePeaPix = SettingsService.GetSettingsValue<bool>(SettingsValues.UsePeaPixSetting);
                if (usePeaPix)
                {
                    Console.WriteLine($"Using URL to get image of the day: {PeaPixUri}");
                    return await GetBingImagesFromPeaPix(ct);
                }
                else
                {
                    Console.WriteLine($"Using URL to get image of the day: {AllOriginsUri}");
                    return await GetBingImagesFromAllOrigins(ct);
                }
                
            }

            Console.WriteLine("Using cached Bing Image Of Day.");

            // Cache could be corrupt; handle that gracefully
            return JsonSerializer.Deserialize<BingImageOfTheDayDto>(cachedJson);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            Console.WriteLine($"Bing image fetch canceled: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Cached Bing JSON was invalid: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get Bing image DTO: {ex}");
            return null;
        }
    }

    private async Task<BingImageOfTheDayDto> GetBingImagesFromAllOrigins(CancellationToken ct)
    {
        BingImageOfTheDayDto dto = new BingImageOfTheDayDto();
        _httpClient ??= new HttpClient();
        var responseDto = await _httpClient.GetFromJsonAsync<AllOriginsResultDto>(AllOriginsUri, cancellationToken: ct);
        if (responseDto != null)
        {
            var correctedJson = responseDto.contents.Replace("\\", "");
            dto = JsonSerializer.Deserialize<BingImageOfTheDayDto>(correctedJson, new JsonSerializerOptions());
            Console.WriteLine($"Dto from bing: {dto.images[0].fullstartdate}");
            var imageDate = DateTime.ParseExact(dto.images[0].fullstartdate, "yyyyMMddHHmm",
                CultureInfo.InvariantCulture);
            SettingsService.UpdateSetting(SettingsValues.ImageOfTheDayCacheSetting, JsonSerializer.Serialize(dto));
            SettingsService.UpdateSetting(SettingsValues.ReQueryImagesAfterTimeSetting,
                imageDate.ToString(CultureInfo.InvariantCulture));
            await SettingsService.SaveAsync();
        }
        else
        {
            Console.WriteLine("Received no or a null response back from All Origins. :(");
        }
        return dto;
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
                dto.images.Add(BingImageOfTheDay.FromPeaPixDto(image));
            }
            var imageDate = DateTime.ParseExact(dto.images[0].fullstartdate, "yyyyMMdd",
                CultureInfo.InvariantCulture);
            SettingsService.UpdateSetting(SettingsValues.ImageOfTheDayCacheSetting, JsonSerializer.Serialize(dto));
            SettingsService.UpdateSetting(SettingsValues.ReQueryImagesAfterTimeSetting,
                imageDate.ToString(CultureInfo.InvariantCulture));
            await SettingsService.SaveAsync();
        }
        else
        {
            Console.WriteLine("Received no or a null response back from PeaPix. :(");
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