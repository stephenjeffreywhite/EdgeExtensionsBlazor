using System;

namespace BlazorEdgeNewTab.Models;

public class BingImageOfDayPeapixDto
{
    public string   title     { get; set; }
    public string   copyright { get; set; }
    public string   fullUrl   { get; set; }
    public string   imageUrl  { get; set; }
    public string   pageUrl   { get; set; }
    public DateOnly date      { get; set; }
}