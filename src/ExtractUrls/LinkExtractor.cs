using AngleSharp;
using AngleSharp.Dom;
using LanguageExt;
using static LanguageExt.Prelude;
using static ExtractUrls.LinkUtils;

namespace ExtractUrls;

public static class LinkExtractor
{
    public static async Task<IDocument?> LoadHtmlDocumentFromUrl(string url)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.37");

        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var pageContent = await response.Content.ReadAsStringAsync();
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(pageContent));
            return document;
        }

        return null;
    }

    public static List<Link> ExtractLinks(IDocument document, Uri rootUri)
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

    public static async Task WriteLinksToFileAsync(IEnumerable<Link> links, string fileName)
    {
        await Console.Out.WriteLineAsync($"\nWriting links to file");

        await using var writer = new StreamWriter(fileName);
        
        foreach (var link in links)
            await WriteLinkToFileAsync(link.Href, link.Title, link.Description, writer);
    }
    
    public static async Task<IEnumerable<Link>> ProcessLinks(IEnumerable<Link> links)
    {
        var updatedLinks = await Task.WhenAll(links.Select(link => ProcessLinkAsync(link.Href, link.Title)));
        
        return updatedLinks;
    }

    private static async Task<Link> ProcessLinkAsync(string url, string? title)
    {
        try
        {
            var linkDoc = await LoadHtmlDocumentFromUrl(url);
            await Console.Out.WriteAsync(".");
            if(linkDoc == null)
                return new Link
                {
                    Title = title ?? url,
                    Description = "",
                    Href = url
                };
            
            return new Link
            {
                Title = title ?? GetTitle(linkDoc).IfNone(url),
                Description = GetDescription(linkDoc),
                Href = url
            };
        }
        catch (Exception e)
        {
            await Console.Out.WriteAsync(".");

            return new Link
            {
                Title = title ?? url,
                Description = e.Message,
                Href = url
            };
        }
            



    }

    private static Option<string> GetTitle(IParentNode linkDoc)
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

    private static string? GetDescription(IParentNode linkDoc) =>
        linkDoc.QuerySelectorAll("meta")
            .Where(m => m.GetAttribute("name") == "description")
            .Select(m => m.GetAttribute("content"))
            .FirstOrDefault();

    private static async Task WriteLinkToFileAsync(string url, string title, string? description, StreamWriter writer)
    {
        var markdownLink = $"[{title}]({url})";
        await writer.WriteLineAsync(markdownLink);

        if (!string.IsNullOrWhiteSpace(description))
            await writer.WriteLineAsync(description);

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
    public string? Description { get; set; }
}
