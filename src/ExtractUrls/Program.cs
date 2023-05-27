using ExtractUrls;
using static ExtractUrls.LinkExtractor;

if (args.Length == 0)
{
    Console.WriteLine("Please provide a URL as a command-line argument.");
    return;
}

Console.Out.WriteLine("Processing {0}", args[0]);

var url = args[0];

var doc = await LoadHtmlDocumentFromUrl(url);
await doc.IfSomeAsync(async d =>
{
    var links = d.ExtractLinks(new Uri(url));

    var updatedLinks = await ProcessLinks(links);


    await WriteLinksToFileAsync(updatedLinks, "links.md");
});



 