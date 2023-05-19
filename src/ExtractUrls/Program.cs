using System.Net;
using HtmlAgilityPack;

if (args.Length == 0)
{
    Console.WriteLine("Please provide a URL as a command-line argument.");
    return;
}

var url = args[0];

var web = new HtmlWeb();
web.PreRequest += request =>
{
    request.Headers["User-Agent"] =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3 Edge/16.16299";
    return true;
};
var doc = web.Load(url);

var rootUri = new Uri(url);
var links = doc.DocumentNode.Descendants("a")
    .Select(a => a.GetAttributeValue("href", null))
    .Where(href => !string.IsNullOrEmpty(href) && href.StartsWith("http") && !new Uri(href).Host.Equals(rootUri.Host))
    .ToList();
Console.Out.WriteLine("Found {0} links", links.Count);
using var writer = new StreamWriter("links.md");

foreach (var link in links)
{
    Console.Out.WriteLine("Processing {0}", link);
    try
    {
        var linkRequest = (HttpWebRequest) WebRequest.Create(link);
        linkRequest.UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3 Edge/16.16299";
        var linkResponse = (HttpWebResponse) linkRequest.GetResponse();
        var linkStream = linkResponse.GetResponseStream();
        var linkDoc = new HtmlDocument();
        linkDoc.Load(linkStream);
        var title = linkDoc.DocumentNode.Descendants("title").FirstOrDefault()?.InnerText;
        var description = linkDoc.DocumentNode.Descendants("meta")
            .Where(m => m.GetAttributeValue("name", "") == "description")
            .Select(m => m.GetAttributeValue("content", ""))
            .FirstOrDefault();

        Console.Out.WriteLine("Title: {0}", string.IsNullOrWhiteSpace(title) ? "No title" : title);

        var mdLink = string.IsNullOrWhiteSpace(title) ? $"{link}" : $"[{title}]({link})";
        writer.WriteLine(mdLink);
        if (!string.IsNullOrWhiteSpace(description))
            writer.WriteLine(description);

        writer.WriteLine();
    }
    catch (Exception e)
    {
        Console.Out.WriteLine("");
        writer.WriteLine($"{link}");
        writer.WriteLine();
        Console.WriteLine(e);
        Console.Out.WriteLine("");
    }
}