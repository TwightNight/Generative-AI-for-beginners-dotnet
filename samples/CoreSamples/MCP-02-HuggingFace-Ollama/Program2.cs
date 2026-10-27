using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.RegularExpressions;

public class Program2
{
    public static async Task RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var config = builder.Configuration
            .AddEnvironmentVariables()
            .AddUserSecrets<Program2>()
            .Build();
        var hfApiKey = config["HF_API_KEY"]
            ?? throw new InvalidOperationException("Set HF_API_KEY to a Hugging Face access token.");

        var clientTransport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Name = "Hugging Face MCP",
                Endpoint = new Uri("https://huggingface.co/mcp"),
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {hfApiKey}"
                }
            });

        await using var mcpClient = await McpClient.CreateAsync(clientTransport);

        const string imageToolName = "gr1_z_image_turbo_generate";
        var tools = await mcpClient.ListToolsAsync();
        var imageTool = tools.FirstOrDefault(tool => tool.Name == imageToolName)
            ?? throw new InvalidOperationException(
                $"The Hugging Face MCP server does not expose '{imageToolName}'.");

        Console.WriteLine($"Calling Hugging Face MCP tool directly: {imageTool.Name}");
        var result = await mcpClient.CallToolAsync(
            imageTool.Name,
            new Dictionary<string, object?>
            {
                ["prompt"] = "A pixelated puppy, retro 16-bit pixel art"
            });

        if (result.IsError is true)
        {
            var errorDetails = string.Join(
                Environment.NewLine,
                result.Content.OfType<TextContentBlock>().Select(text => text.Text));
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errorDetails)
                    ? "The Hugging Face image-generation tool returned an error."
                    : $"The Hugging Face image-generation tool returned an error: {errorDetails}");
        }

        var imageSaved = false;
        var imageUrlReturned = false;
        foreach (var content in result.Content)
        {
            switch (content)
            {
                case TextContentBlock text:
                    Console.WriteLine(text.Text);
                    foreach (Match match in Regex.Matches(text.Text, @"https?://\S+"))
                    {
                        Console.WriteLine($"Image URL: {match.Value.TrimEnd('.', ',', ')', ']')}");
                        imageUrlReturned = true;
                    }
                    break;
                case ResourceLinkBlock link:
                    Console.WriteLine($"Image URL: {link.Uri}");
                    imageUrlReturned = true;
                    break;
                case ImageContentBlock image:
                    var extension = image.MimeType switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/webp" => ".webp",
                        _ => ".png"
                    };
                    var imagePath = Path.GetFullPath(
                        $"generated-image-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}{extension}");
                    await File.WriteAllBytesAsync(imagePath, image.DecodedData.ToArray());
                    Console.WriteLine($"Image saved to: {imagePath}");
                    imageSaved = true;
                    break;
            }
        }

        if (!imageSaved && !imageUrlReturned)
        {
            throw new InvalidOperationException(
                "The MCP tool returned neither an image nor an image URL.");
        }
    }
}
