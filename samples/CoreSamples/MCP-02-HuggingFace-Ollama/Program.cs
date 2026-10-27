using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using OllamaSharp;
using System.Text.RegularExpressions;
//5908bcedd0734503be0dff2aedeee16c.xeBCLaeVcHmqtygpN_ZssUlr
// To run the sample, you need to set the following environment variables or user secrets:
// "HF_API_KEY": " your HF token"
// "OLLAMA_API_KEY": " your Ollama token"
// "deploymentName" : "llama3.2" // Optional, defaults to "llama3.2" if not specified

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();
var ollamaKey = "OLLAMA_API_KEY";

var http = new HttpClient { BaseAddress = new Uri("https://ollama.com") };
http.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ollamaKey);

var deploymentName = config["deploymentName"] ?? "gemma4:31b";

// create MCP Client using Hugging Face endpoint
var hfApiKey = config["HF_API_KEY"] ?? throw new InvalidOperationException("Set HF_API_KEY to a Hugging Face access token.");
var hfHeaders = new Dictionary<string, string>
{
    { "Authorization", $"Bearer {hfApiKey}" }
};
var clientTransport = new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Name = "HF Server",
        Endpoint = new Uri("https://huggingface.co/mcp"),
        AdditionalHeaders = hfHeaders
    });
await using var mcpClient = await McpClient.CreateAsync(clientTransport);

// Display the available server tools
var tools = await mcpClient.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"Connected to server with tools: {tool.Name}");
}
Console.WriteLine("Press Enter to continue...");
Console.ReadLine();
Console.WriteLine();

// create an IChatClient using the MCP tools
var client = ((IChatClient)new OllamaApiClient(http, deploymentName))
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var chatOptions = new ChatOptions
{
    Tools = [.. tools],
    ModelId = deploymentName
};

// Create image
Console.WriteLine("Starting the process to generate an image of a pixelated puppy...");
var query = "Create an image of a pixelated puppy.";
var result = await client.GetResponseAsync(query, chatOptions);

var imageSaved = false;
var imageUrlReturned = false;
foreach (var content in result.Messages.SelectMany(message => message.Contents))
{
    switch (content)
    {
        case TextContent text:
            Console.WriteLine(text.Text);
            foreach (Match match in Regex.Matches(text.Text, @"https?://\S+"))
            {
                Console.WriteLine($"Image URL: {match.Value.TrimEnd('.', ',', ')', ']')}");
                imageUrlReturned = true;
            }
            break;
        case UriContent uri:
            Console.WriteLine($"Image URL: {uri.Uri}");
            imageUrlReturned = true;
            break;
        case DataContent data when data.MediaType.StartsWith(
            "image/",
            StringComparison.OrdinalIgnoreCase):
            var extension = data.MediaType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/webp" => ".webp",
                _ => ".png"
            };
            var imagePath = Path.GetFullPath(
                $"generated-image-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}{extension}");
            await File.WriteAllBytesAsync(imagePath, data.Data.ToArray());
            Console.WriteLine($"Image saved to: {imagePath}");
            imageSaved = true;
            break;
    }
}

if (!imageSaved && !imageUrlReturned)
{
    throw new InvalidOperationException(
        "The model response contained neither an image nor an image URL.");
}

Console.WriteLine($"AI response: {result.Text}");
