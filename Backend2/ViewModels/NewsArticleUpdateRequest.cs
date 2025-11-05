// BusinessObject/NewsArticleUpdateRequest.cs
using System.Collections.Generic;

public class NewsArticleUpdateRequest
{
    public int NewsArticleId { get; set; }
    public string NewsTitle { get; set; }
    public string Headline { get; set; }
    public string NewsContent { get; set; }
    public string NewsSource { get; set; }
    public int? CategoryId { get; set; }
    public bool? NewsStatus { get; set; }
    public List<int> TagIds { get; set; } 
}