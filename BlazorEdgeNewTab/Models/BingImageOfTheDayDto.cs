using System.Collections.Generic;

namespace BlazorEdgeNewTab.Models;

public class BingImageOfTheDayDto
{
    public List<BingImageOfTheDay> images   { get; set; } = new();
    public BingImageToolTip    tooltips { get; set; }
}