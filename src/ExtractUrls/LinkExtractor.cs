using System.Net;
using System.Text;
using HtmlAgilityPack;
using LanguageExt;
using static LanguageExt.Prelude;
using static ExtractUrls.LinkUtils;

namespace ExtractUrls;

public static class LinkExtractor
{
    public static HtmlDocument LoadHtmlDocument(string url)
    {
        var web = new HtmlWeb
        {
            AutoDetectEncoding = false,
            OverrideEncoding = Encoding.UTF8
        };

        web.PreRequest += request =>
        {
            request.Headers["User-Agent"] =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3 Edge/16.16299";
            return true;
        };

        return web.Load(url);
    }

    public static List<Link> ExtractLinks(HtmlDocument document, Uri rootUri)
    {
        return document.DocumentNode.Descendants("a")
            .Where(l => IsValidLink(l.GetAttributeValue("href", null), rootUri))
            .Select(a => new Link
            {
                Href = DecodeUrl(a.GetAttributeValue("href", null)),
                Title = TrimTitle(a.InnerText.Trim())
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
            var linkDoc = await GetHtmlDocumentAsync(link.Href);
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

    private static async Task<HtmlDocument> GetHtmlDocumentAsync(string link)
    {
        var linkRequest = (HttpWebRequest) WebRequest.Create(link);
        linkRequest.UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3 Edge/16.16299";

        using var linkResponse = (HttpWebResponse) await linkRequest.GetResponseAsync();
        using var linkStream = linkResponse.GetResponseStream();
        var linkDoc = new HtmlDocument();
        linkDoc.Load(linkStream);
        return linkDoc;
    }

    private static Option<string> GetTitle(HtmlDocument linkDoc)
    {
        var tags = new[] {"title", "h1", "h2", "h3", "h4", "h5", "h6"};

        foreach (var tag in tags)
        {
            var node = linkDoc.DocumentNode.Descendants(tag).FirstOrDefault();

            if (node != null && !string.IsNullOrWhiteSpace(node.InnerText))
                return TrimTitle(node.InnerText);
        }

        return None;
    }


    private static string? GetDescription(HtmlDocument linkDoc) =>
        linkDoc.DocumentNode.Descendants("meta")
            .Where(m => m.GetAttributeValue("name", "") == "description")
            .Select(m => m.GetAttributeValue("content", ""))
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