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
    public static void WriteLinksToFile(List<Link> links, string fileName)
    {
        using StreamWriter writer = new StreamWriter(fileName);

        foreach (var link in links)
        {
            ProcessLink(link, writer);
        }
    }
    private static void ProcessLink(Link link, StreamWriter writer)
    {
        Console.WriteLine($"Processing '{link.Title}' {link.Href}");

        try
        {
            HtmlDocument linkDoc = GetHtmlDocument(link.Href);
            string title = link.Title ?? GetTitle(linkDoc).IfNone(link.Href);
            string? description = GetDescription(linkDoc);

            Console.WriteLine($"Title: {title}");

            WriteLinkToFile(link, title, description, writer);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            WriteErrorToFile(link, e.Message, writer);
        }
    }
    
    private static string? GetDescription(HtmlDocument linkDoc)
    {
        return linkDoc.DocumentNode.Descendants("meta")
            .Where(m => m.GetAttributeValue("name", "") == "description")
            .Select(m => m.GetAttributeValue("content", ""))
            .FirstOrDefault();
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

    private static void WriteLinkToFile(Link link, string title, string? description, StreamWriter writer)
    {
        string markdownLink = $"[{title}]({link.Href})";
        writer.WriteLine(markdownLink);

        if (!string.IsNullOrWhiteSpace(description))
            writer.WriteLine(description);

        writer.WriteLine();
    }

    private static void WriteErrorToFile(Link link, string errorMessage, StreamWriter writer)
    {
        string title = link.Title ?? link.Href;
        string markdownLink = $"[{title}]({link.Href})";

        writer.WriteLine(markdownLink);
        writer.WriteLine("Error: " + errorMessage);
        writer.WriteLine();
    }

    private static HtmlDocument GetHtmlDocument(string link)
    {
        var linkRequest = (HttpWebRequest) WebRequest.Create(link);
        linkRequest.UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3 Edge/16.16299";
        var linkResponse = (HttpWebResponse) linkRequest.GetResponse();
        var linkStream = linkResponse.GetResponseStream();

        var linkDoc = new HtmlDocument();
        linkDoc.Load(linkStream);
        return linkDoc;
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