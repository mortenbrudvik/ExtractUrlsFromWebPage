using AngleSharp;
using LanguageExt;
using static LanguageExt.Prelude;
using static ExtractUrls.LinkUtils;

namespace ExtractUrls;

public static class LinkExtractor
{
    public static async Task<AngleSharp.Dom.IDocument> LoadHtmlDocumentFromUrl(string url)
    {
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(url);
        return document;
    }

    public static List<Link> ExtractLinks(AngleSharp.Dom.IDocument document, Uri rootUri)
    {
        return document.QuerySelectorAll("a")
            .Where(l => IsValidLink(l.GetAttribute("href"), rootUri))
            .Select(a => new Link
            {
                Href = a.GetAttribute("href"),
                Title = TrimTitle(a.TextContent.Trim())
            })
            .ToList();
    }

    public static async Task WriteLinksToFileAsync(List<Link> links, string fileName)
    {
        await using var writer = new StreamWriter(fileName);

        var tasks = links.Select(link => ProcessLinkAsync(link, writer));
        await Task.WhenAll(tasks);
    }

    private static async Task ProcessLinkAsync(Link link, StreamWriter writer)
    {
        Console.WriteLine($"Processing '{link.Title}' {link.Href}");

        try
        {
            var linkDoc = await LoadHtmlDocumentFromUrl(link.Href);
            var title = link.Title ?? GetTitle(linkDoc).IfNone(link.Href);
            var description = GetDescription(linkDoc);

            await WriteLinkToFileAsync(link.Href, title, description, writer);
        }
        catch (Exception e)
        {
            var title = link.Title ?? link.Href;
            await WriteErrorToFileAsync(link.Href, title, e.Message, writer);
        }
    }

    private static Option<string> GetTitle(AngleSharp.Dom.IDocument linkDoc)
    {
        var tags = new[] { "title", "h1", "h2", "h3", "h4", "h5", "h6" };

        foreach (var tag in tags)
        {
            var node = linkDoc.QuerySelector(tag);

            if (node != null && !string.IsNullOrWhiteSpace(node.TextContent))
                return TrimTitle(node.TextContent);
        }

        return None;
    }

    private static string? GetDescription(AngleSharp.Dom.IDocument linkDoc) =>
        linkDoc.QuerySelectorAll("meta")
            .Where(m => m.GetAttribute("name") == "description")
            .Select(m => m.GetAttribute("content"))
            .FirstOrDefault();

    private static async Task WriteLinkToFileAsync(string url, string title, string? description, StreamWriter writer)
    {
        await Console.Out.WriteLineAsync($"\nCompleted: '{title}' {url} {description ?? ""}");
        var markdownLink = $"[{title}]({url})";
        await writer.WriteLineAsync(markdownLink);

        if (!string.IsNullOrWhiteSpace(description))
            await writer.WriteLineAsync(description);

        await writer.WriteLineAsync();
    }

    private static async Task WriteErrorToFileAsync(string url, string title, string errorMessage, StreamWriter writer)
    {
        await Console.Out.WriteLineAsync($"\nCompleted with error: '{title}' {url} \nError msg: {errorMessage}");

        var markdownLink = $"[{title}]({url})";

        await writer.WriteLineAsync(markdownLink);
        await writer.WriteLineAsync("Error: " + errorMessage);
        await writer.WriteLineAsync();
    }

    private static bool IsValidLink(string url, Uri rootUri) =>
        !string.IsNullOrEmpty(url) &&
        url.StartsWith("http") &&
        !new Uri(url).Host.Equals(rootUri.Host);
}

public class Link
{
    public string Href { get; init; } = null!;
    public string? Title { get; set; }
}
