using AngleSharp;
using AngleSharp.Dom;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ExtractUrls;

public static class LinkExtractor
{
    public static async Task<Option<IDocument>> LoadHtmlDocumentFromUrl(string url)
    {
        try
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.37");

            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return null;
        
            var pageContent = await response.Content.ReadAsStringAsync();
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(pageContent));
            return Some(document);
        }
        catch (Exception)
        {
            return None;
        }
    }

    public static async Task WriteLinksToFileAsync(IEnumerable<Link> links, string fileName)
    {
        await Console.Out.WriteLineAsync($"\nWriting links to file");

        await using var writer = new StreamWriter(fileName);
        
        foreach (var link in links)
            await WriteLinkToFileAsync(link.Href, link.Title.IfNone("No title"), link.Description, writer);
        
        await Console.Out.WriteLineAsync($"Links written to {fileName}");
    }
    
    public static async Task<IEnumerable<Link>> ProcessLinks(IEnumerable<Link> links)
    {
        var updatedLinks = await Task.WhenAll(links.Select(link => ProcessLinkAsync(link.Href, link.Title)));

        Console.Out.WriteLine("\nProcessed {0} links", updatedLinks.ToList().Count);
        
        return updatedLinks;
    }

    private static async Task<Link> ProcessLinkAsync(string url, Option<string> title)
    {
        var doc = await LoadHtmlDocumentFromUrl(url);

        var link = doc.Match(
            d => new Link(
                url, 
                title.IfNone( 
                    d.GetTitle().IfNone(url)), 
                d.GetDescription()),
            () => new Link(url, title.IfNone(url)));
        
        await Console.Out.WriteAsync(".");
        return link;
    }

    private static async Task WriteLinkToFileAsync(string url, string title, string? description, StreamWriter writer)
    {
        var markdownLink = $"[{title}]({url})";
        await writer.WriteLineAsync(markdownLink);

        if (!string.IsNullOrWhiteSpace(description))
            await writer.WriteLineAsync(description);

        await writer.WriteLineAsync();
    }
}