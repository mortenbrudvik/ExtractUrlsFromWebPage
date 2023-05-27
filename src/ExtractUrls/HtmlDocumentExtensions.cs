using AngleSharp.Dom;
using LanguageExt;

using static LanguageExt.Prelude;

namespace ExtractUrls;

public static class HtmlDocumentExtensions
{
    public static IEnumerable<Link> ExtractLinks(this IDocument document, Uri rootUri)
    {
        var links =  document.QuerySelectorAll("a")
            .Where(l => IsValidLink(l.GetAttribute("href")!, rootUri))
            .Select(a => new Link(a.GetAttribute("href")!, a.GetTextContent()))
            .ToList();
        
        Console.WriteLine($"Found {links.Count} links");
        
        return links;
    }
    public static Option<string> GetTitle(this IParentNode doc) =>
        new[] {"title", "h1", "h2", "h3", "h4", "h5", "h6"}
            .Aggregate((Option<string>)None, (current, tag) =>
                current.Match(
                    None: () => doc.QuerySelector(tag).GetTextContent().ToOption(),
                    Some: _ => current
                )
            );


    public static string GetDescription(this IParentNode linkDoc) =>
        linkDoc.QuerySelectorAll("meta")
            .Where(m => m.GetAttribute("name") == "description")
            .Select(m => m.GetAttribute("content"))
            .FirstOrDefault() ?? string.Empty;
    
    private static bool IsValidLink(string url, Uri rootUri) =>
        !string.IsNullOrEmpty(url) &&
        url.StartsWith("http") &&
        !new Uri(url).Host.Equals(rootUri.Host);
    
    private static Option<string> GetTextContent(this IElement? element) =>
        element != null && !string.IsNullOrWhiteSpace(element.TextContent) ? TrimTitle(element.TextContent) : None;
    
    // NOTE: Will insert spaces where line breaks are found to ensure that words are separated by spaces.
    private static Option<string> TrimTitle(string? title) => 
        !string.IsNullOrWhiteSpace(title) ? title.Trim().Replace("\r", " ").Replace("\n", " ") : null;
}