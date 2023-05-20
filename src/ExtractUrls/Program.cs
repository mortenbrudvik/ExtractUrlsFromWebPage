using static ExtractUrls.LinkExtractor;

if (args.Length == 0)
{
    Console.WriteLine("Please provide a URL as a command-line argument.");
    return;
}

Console.Out.WriteLine("Processing {0}", args[0]);

var url = args[0];

var document = await LoadHtmlDocumentFromUrl(url);

var rootUri = new Uri(url);
var links = ExtractLinks(document, rootUri);

Console.WriteLine($"Found {links.Count} links");

await WriteLinksToFileAsync(links, "links.md");
 