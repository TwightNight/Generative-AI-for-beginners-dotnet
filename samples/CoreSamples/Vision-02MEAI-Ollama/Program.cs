using Microsoft.Extensions.AI;
using OllamaSharp;

var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11434/"),
    Timeout = TimeSpan.FromMinutes(5)
};

IChatClient chatClient =
    new OllamaApiClient(httpClient, "gemma4:e4b");

// images
string imgRunningShoes = "running-shoes.jpg";
string imgCarLicense = "license.jpg";
string imgReceipt = "german-receipt.jpg";

// prompts
var promptDescribe = "Describe the image";
var promptAnalyze = "How many red shoes are in the picture? and what other shoes colors are there?";
var promptOcr = "What is the text in this picture? Is there a theme for this?";
var promptReceipt = "I bought the coffee and the sausage. How much do I owe? Add a 18% tip.";
var promptReceipt2 = @"            Extract the following from this receipt:
            - Vendor name
            - Date
            - Total amount
            - List of items with prices
            
            Return as JSON.";


// prompts
string systemPrompt = @"You are a useful assistant that describes images using a direct style.";
string systemPrompt2 = @"You are a useful assistant that analyzes images and provides detailed information.";

var prompt = promptDescribe;
string imageFileName = imgReceipt;
string image = Path.Combine(AppContext.BaseDirectory, "images", imageFileName);

#region Image Analysis
// read the image bytes, create a new image content part and add it to the messages
AIContent imageContent = new DataContent(File.ReadAllBytes(image), "image/jpeg");
List<ChatMessage> messages =
[
    new ChatMessage(ChatRole.System, systemPrompt2),
    new ChatMessage(ChatRole.User, [new TextContent(promptReceipt), imageContent])
];
#endregion

#region Image Analysis with Image URL
// // create a new image content part and add it to the messages
// var imageUrl = "https://th.bing.com/th/id/OIGP.YkWY75xbagTrqsOmKgfl?r=0&pid=ImgGn";

// // Download the image from the URL
// using var client = new HttpClient();
// var imageBytes = await client.GetByteArrayAsync(imageUrl);


// var imageContent = new DataContent(imageBytes, "image/jpeg");

// var messages = new List<ChatMessage>
// {
//     new ChatMessage(ChatRole.System, systemPrompt),
//     new ChatMessage(ChatRole.User, new List<AIContent> { new TextContent(prompt), imageContent })
// };
#endregion
// send the messages to the assistant
var imageAnalysis = await chatClient.GetResponseAsync(messages);

Console.WriteLine($"Prompt: {prompt}");
Console.WriteLine($"Image: {imageFileName}");
Console.WriteLine($"Response: {imageAnalysis.Text}");