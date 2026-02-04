using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorEdgeNewTab.Pages;

public partial class QuickLinkComponent
{
    [Inject]
    IJSRuntime JsRuntime { get; set; }
    [Inject] 
    private NavigationManager NavigationManager { get; set; }
    [Parameter]
    public string QuickLinkUrl { get; set; }

    [Parameter]
    public string QuickLinkImageUrl { get; set; }

    [Parameter]
    public string QuickLinkTitle { get; set; }

    [Parameter]
    public string QuickLinkId { get; set; }

    [CascadingParameter]
    public Index NewTabPage { get; set; }

    

    public string DeleteModalName => $"DeleteQuickLinkModalFor{QuickLinkId}";
    public string EditModalName => $"EditQuickLinkModalFor{QuickLinkId}";

    private string NewQuickLinkTitle { get; set; }
    private string NewQuickLinkUrl   { get; set; }

    private void OnQuickLinkClicked()
    {
        NavigationManager.NavigateTo(QuickLinkUrl);
    }

    private async Task DeleteQuickLink()
    {
        await NewTabPage.DeleteQuickLink(QuickLinkId);
    }

    private async Task EditQuickLink()
    {
        await NewTabPage.EditQuickLink(QuickLinkId, NewQuickLinkTitle, NewQuickLinkUrl);
        QuickLinkTitle = NewQuickLinkTitle;
        QuickLinkUrl = NewQuickLinkUrl;
        StateHasChanged();
    }

    private Task ResetEditQuickLink()
    {
        NewQuickLinkTitle = QuickLinkTitle;
        NewQuickLinkUrl = QuickLinkUrl;
        return Task.CompletedTask;
    }

    private async Task HandleDrop(DragEventArgs arg)
    {
        if (NewTabPage.DraggingQuickLinkId != null && QuickLinkId != NewTabPage.DraggingQuickLinkId)
        {
            Console.WriteLine($"Dropped {NewTabPage.DraggingQuickLinkId} on {QuickLinkId}");
            await NewTabPage.OnQuickLinkDragged(NewTabPage.DraggingQuickLinkId, QuickLinkId);
        }
    }

    private Task OnDragStart(DragEventArgs arg)
    {
        Console.WriteLine($"Started dragging {QuickLinkId}");
        NewTabPage.DraggingQuickLinkId = QuickLinkId;
        return Task.CompletedTask;
    }
}