using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorEdgeNewTab.Constants;
using BlazorEdgeNewTab.Models;
using BlazorEdgeNewTab.Services.Interfaces;
using WebExtensions.Net;
using WebExtensions.Net.Bookmarks;

namespace BlazorEdgeNewTab.Services;

public class NewTabService : INewTabService
{
    private readonly string BingImageOfDayUri =
        "https://api.allorigins.win/get?url=https%3A//www.bing.com/HPImageArchive.aspx%3Fformat%3Djs%26idx%3D0%26n%3D7";

    private HttpClient _httpClient;

    public async Task<BingImageOfTheDayDto> GetImageOfDayDto()
    {
        var dto = new BingImageOfTheDayDto();
        var reQueryTime = Convert.ToDateTime(Settings.GetSettingValue(SettingsValues.ReQueryImagesAfterTime)
                                             ?? DateTime.Now.AddDays(-1).ToString(CultureInfo.InvariantCulture));
        Console.WriteLine($"Re-Query Date for Bing Image Of Day In Cache Is: {reQueryTime.Date}");
        Console.WriteLine($"Current Date Is: {DateTime.Now.Date}");
        if (DateTime.Now.Date > reQueryTime.Date || Settings.GetSettingValue(SettingsValues.ImageOfTheDayCache) == null)
        {
            Console.WriteLine("Getting Bing Image of Day from Bing");
            Console.WriteLine($"Using URL to get image of the day: {BingImageOfDayUri}");
            _httpClient ??= new HttpClient();
            var responseDto = await _httpClient.GetFromJsonAsync<AllOriginsResultDto>(BingImageOfDayUri);
            if (responseDto != null)
            {
                var correctedJson = responseDto.contents.Replace("\\", "");
                dto = JsonSerializer.Deserialize<BingImageOfTheDayDto>(correctedJson, new JsonSerializerOptions());
                Console.WriteLine($"Dto from bing: {dto.images[0].fullstartdate}");
                var imageDate = DateTime.ParseExact(dto.images[0].fullstartdate, "yyyyMMddHHmm",
                    CultureInfo.InvariantCulture);
                Settings.UpdateSetting(SettingsValues.ImageOfTheDayCache, JsonSerializer.Serialize(dto));
                Settings.UpdateSetting(SettingsValues.ReQueryImagesAfterTime,
                    imageDate.ToString(CultureInfo.InvariantCulture));
                await Settings.SaveAsync();
                return dto;
            }

            Console.WriteLine("Received no or a null response back from All Origins. :(");
        }
        else
        {
            Console.WriteLine("Using cached Bing Image Of Day.");
            dto = JsonSerializer.Deserialize<BingImageOfTheDayDto>(
                Settings.GetSettingValue(SettingsValues.ImageOfTheDayCache));
        }

        return dto;
    }

    public async Task<List<QuickLink>> SetUpQuickLinks(IWebExtensionsApi webExtensions)
    {
        var                    quickLinks        = new List<QuickLink>();
        var                    extensionId       = webExtensions.Runtime.Id;
        var bookmarkTreeNodes = await GetBookmarkTreeNodes(webExtensions);
        if (!bookmarkTreeNodes.Any())
        {
            var folderName = Settings.GetSettingValue(SettingsValues.QuickLinkBookMarkFolder);
            await webExtensions.Bookmarks.Create(new CreateDetails
                {Title = folderName});
        }
        else
        {
            var quickLinkBookMarks = await webExtensions.Bookmarks.GetChildren(bookmarkTreeNodes.First().Id);
            quickLinks.AddRange(quickLinkBookMarks.Select(quickLinkBookMark => new QuickLink
            {
                QuickLinkTitle = quickLinkBookMark.Title,
                QuickLinkImageUrl = GetFaviconUrl(extensionId, quickLinkBookMark.Url),
                QuickLinkUrl = quickLinkBookMark.Url,
                QuickLinkId = quickLinkBookMark.Id,
                Visible = true
            }));
        }

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
            await webExtensions.Bookmarks.Search(Settings.GetSettingValue(SettingsValues.QuickLinkBookMarkFolder));
        var bookmarkTreeNodes = bookMarkNode.ToList();
        return bookmarkTreeNodes;
    }
}