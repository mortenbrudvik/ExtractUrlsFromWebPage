using System.Net;
using System.Text;
using HtmlAgilityPack;
using static ExtractUrls.LinkUtils;

if (args.Length == 0)
{
    Console.WriteLine("Please provide a URL as a command-line argument.");
    return;
}

Console.Out.WriteLine("Processing {0}", args[0]);

var url = args[0];


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
var doc = web.Load(url);

var rootUri = new Uri(url);
var links = doc.DocumentNode.Descendants("a")
    .Select(a => 
        new
        {
            Href = DecodeUrl(a.GetAttributeValue("href", null)), 
            Title = TrimTitle(a.InnerText.Trim())
        })
    .Where(a => !string.IsNullOrEmpty(a.Href) && a.Href.StartsWith("http") &&
                !new Uri(a.Href).Host.Equals(rootUri.Host))
    .ToList();
Console.Out.WriteLine("Found {0} links", links.Count);
using var writer = new StreamWriter("links.md");

foreach (var link in links)
{
    Console.Out.WriteLine("Processing {0}", link.Href);
    try
    {
        var linkDoc = GetHtmlDocument(link.Href);
        var title = link.Title ?? GetTitle(linkDoc) ?? link.Href;
        var description = linkDoc.DocumentNode.Descendants("meta")
            .Where(m => m.GetAttributeValue("name", "") == "description")
            .Select(m => m.GetAttributeValue("content", ""))
            .FirstOrDefault();

        Console.Out.WriteLine("Title: {0}", title);

        var mdLink = $"[{title}]({link.Href})";
        writer.WriteLine(mdLink);
        if (!string.IsNullOrWhiteSpace(description))
            writer.WriteLine(description);

        writer.WriteLine();
    }
    catch (Exception e)
    {
        Console.Out.WriteLine("");
        
        var title = link.Title ?? link.Href;
        var mdLink = $"[{title}]({link.Href})";
        
        writer.WriteLine(mdLink);
        writer.WriteLine("Error: " + e.Message);
        writer.WriteLine();
        
        Console.WriteLine(e);
        Console.Out.WriteLine("");
    }

    static HtmlDocument GetHtmlDocument(string link)
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

    static string? GetTitle(HtmlDocument linkDoc)
    {
        var title = linkDoc.DocumentNode.Descendants("title").FirstOrDefault()?.InnerText;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = linkDoc.DocumentNode.Descendants("h1").FirstOrDefault()?.InnerText;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = linkDoc.DocumentNode.Descendants("h2").FirstOrDefault()?.InnerText;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = linkDoc.DocumentNode.Descendants("h3").FirstOrDefault()?.InnerText;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = linkDoc.DocumentNode.Descendants("h4").FirstOrDefault()?.InnerText;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = linkDoc.DocumentNode.Descendants("h5").FirstOrDefault()?.InnerText;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = linkDoc.DocumentNode.Descendants("h6").FirstOrDefault()?.InnerText;
        }

        return  TrimTitle(title);
    }
}


