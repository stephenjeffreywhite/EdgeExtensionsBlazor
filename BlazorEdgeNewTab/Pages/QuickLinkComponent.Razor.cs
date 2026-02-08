using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorEdgeNewTab.Pages;

public partial class QuickLinkComponent
{
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
    public  string DropOverCssClass      { get; set; }

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
        if (!string.IsNullOrWhiteSpace(DropOverCssClass))
        {
            DropOverCssClass = "";
        }
        if (NewTabPage.DraggedQuickLinkId != null && QuickLinkId != NewTabPage.DraggedQuickLinkId)
        {
            Console.WriteLine($"Dropped {NewTabPage.DraggedQuickLinkId} on {QuickLinkId}");
            //Get the id of the dragged quick link from the parent component, Blazor currently does not let us set data in the DataTransfer object that can be read in the drop event handler,
            await NewTabPage.OnQuickLinkDragged(NewTabPage.DraggedQuickLinkId, QuickLinkId);
        }
    }

    private Task OnDragStart(DragEventArgs arg)
    {
        Console.WriteLine($"Started dragging {QuickLinkId}");
        // Store the dragged quick link's id in the parent component, Blazor currently does not let us set data in the DataTransfer object that can be read in the drop event handler,
        // so we are using a property in the parent component to store the dragged quick link's id. In the future if Blazor allows us to set data in the DataTransfer object,
        // we can remove the DraggedQuickLinkId property from the parent component and use the DataTransfer object instead.
        NewTabPage.DraggedQuickLinkId = QuickLinkId;
        return Task.CompletedTask;
    }

    private Task OnDragOver(DragEventArgs arg)
    {
        DropOverCssClass = "QuickLinkContentDragOver";
        return Task.CompletedTask;
    }

    private Task OnDragLeave(DragEventArgs arg)
    {
        if (!string.IsNullOrWhiteSpace(DropOverCssClass))
        {
            DropOverCssClass = "";
        }
        return Task.CompletedTask;
    }

    private Task OnDragEnd(DragEventArgs arg)
    {
        if (!string.IsNullOrWhiteSpace(DropOverCssClass))
        {
            DropOverCssClass = "";
        }
        return Task.CompletedTask;
    }
}