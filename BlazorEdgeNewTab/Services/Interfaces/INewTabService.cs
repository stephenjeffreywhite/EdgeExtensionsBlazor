using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorEdgeNewTab.Models;
using WebExtensions.Net;

namespace BlazorEdgeNewTab.Services.Interfaces;

public interface INewTabService
{
    public Task<BingImageOfTheDayDto> GetImageOfDayDto();

    public Task<List<QuickLink>> SetUpQuickLinks(IWebExtensionsApi webExtensions);
    public Task SwapQuickLinks(IWebExtensionsApi webExtensions, string droppedQuickLinkId, string destinationQuickLinkId);
}