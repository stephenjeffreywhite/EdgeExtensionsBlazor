using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorEdgeNewTab.Constants;
using BlazorEdgeNewTab.Models;
using WebExtensions.Net;
using WebExtensions.Net.Bookmarks;

namespace BlazorEdgeNewTab.Services;

public class NewTabService
{
    private readonly string AllOriginsUri =
        "https://api.allorigins.win/get?url=https%3A//www.bing.com/HPImageArchive.aspx%3Fformat%3Djs%26idx%3D0%26n%3D7";

    private HttpClient _httpClient;
    public SettingsService SettingsService { get; }
    public NewTabService(SettingsService settingsService)
    {
        SettingsService = settingsService;
    }

    public async Task<BingImageOfTheDayDto> GetImageOfDayDto()
    {
        var dto = new BingImageOfTheDayDto();
        var reQueryTime = Convert.ToDateTime(SettingsService.GetSettingsValue<string>(SettingsValues.ReQueryImagesAfterTimeSetting)
                                             ?? DateTime.Now.AddDays(-1).ToString(CultureInfo.InvariantCulture));
        Console.WriteLine($"Re-Query Date for Bing Image Of Day In Cache Is: {reQueryTime.Date}");
        Console.WriteLine($"Current Date Is: {DateTime.Now.Date}");
        if (DateTime.Now.Date > reQueryTime.Date || SettingsService.GetSettingsValue<string>(SettingsValues.ImageOfTheDayCacheSetting) == null)
        {
            Console.WriteLine("Getting Bing Image of Day from Bing");
            Console.WriteLine($"Using URL to get image of the day: {AllOriginsUri}");
            return await GetBingImagesFromAllOrigins();
        }
        else
        {
            Console.WriteLine("Using cached Bing Image Of Day.");
            dto = JsonSerializer.Deserialize<BingImageOfTheDayDto>(
                SettingsService.GetSettingsValue<string>(SettingsValues.ImageOfTheDayCacheSetting));
        }

        return dto;
    }

    private async Task<BingImageOfTheDayDto> GetBingImagesFromAllOrigins()
    {
        BingImageOfTheDayDto dto = new BingImageOfTheDayDto();
        _httpClient ??= new HttpClient();
        var responseDto = await _httpClient.GetFromJsonAsync<AllOriginsResultDto>(AllOriginsUri);
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