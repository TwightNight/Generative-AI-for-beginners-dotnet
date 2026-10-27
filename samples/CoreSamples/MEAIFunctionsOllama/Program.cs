using Microsoft.Extensions.AI;
using OllamaSharp;
using System.ComponentModel;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;

var ollamaEndpoint = "http://localhost:11434";
var chatModel = "phi4-mini";

// Configure OpenTelemetry
string sourceName = "MyChatApp";
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()  // Or AddOtlpExporter for production
    .Build();

var cache = new MemoryDistributedCache(
    Options.Create(new MemoryDistributedCacheOptions()));

IChatClient client = ((IChatClient)new OllamaApiClient(ollamaEndpoint, chatModel))
    .AsBuilder()
    .UseDistributedCache(cache) 
    .UseFunctionInvocation()
    .UseOpenTelemetry(
        sourceName: sourceName,
        configure: c => c.EnableSensitiveData = true)  // Include prompt/response in traces
    .Build();

ChatOptions options = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(GetTheWeather),
        AIFunctionFactory.Create(GetCurrentTime),
        AIFunctionFactory.Create(Calculate),
        AIFunctionFactory.Create(SearchProducts)
    ]
};

// var question = "Solve 2+2. Provide an accurate and short answer";
// Console.WriteLine($"question: {question}");
// var response = await client.GetResponseAsync(question, options);
// Console.WriteLine($"response: {response}");

// Console.WriteLine();

// question = "Do I need an umbrella today?. Provide an accurate and short answer";
// Console.WriteLine($"question: {question}");
// response = await client.GetResponseAsync(question, options);
// Console.WriteLine($"response: {response}");

// var question = "What is the current time in Tokyo?";
// Console.WriteLine($"question: {question}");
// var response = await client.GetResponseAsync(question, options);
// Console.WriteLine($"response: {response}");

// var question = "Find me a few budget-friendly laptops—no more than two models.";
// Console.WriteLine($"question: {question}");
// var response = await client.GetResponseAsync<List<Product>>(question, options);
// Console.WriteLine($"response: {string.Join(", ", response.Result.Select( p => $"{p.Name} - {p.Category} - {p.Price}"))}");

string[] prompts = ["What is AI?", "What is .NET?", "What is AI?"];

foreach (var prompt in prompts)
{
    Console.WriteLine($"Prompt: {prompt}");
    var response = await client.GetResponseAsync(prompt);
    Console.WriteLine($"Response: {response.Text}\n");
}

#region Additional functions for demonstration
[Description("Get the weather")]
static string GetTheWeather()
{
    Console.WriteLine("\tGetTheWeather function invoked.");

    var temperature = Random.Shared.Next(5, 20);
    var conditions = Random.Shared.Next(0, 2) == 0 ? "sunny" : "rainy";
    var weather = $"The weather is {temperature} degrees C and {conditions}.";
    Console.WriteLine($"\tGetTheWeather result: {weather}.");
    return weather;
}

[Description("Get the current time in a given IANA time zone")]
static string GetCurrentTime(
    [Description("IANA time zone id, e.g. 'Asia/Tokyo', 'Asia/Ho_Chi_Minh'")] string timeZone = "UTC")
{
    try
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return $"{timeZone}: {now:yyyy-MM-dd HH:mm:ss}";
    }
    catch (TimeZoneNotFoundException)
    {
        return $"Unknown time zone '{timeZone}'.";
    }
}

[Description("Perform basic arithmetic on two numbers")]
static string Calculate(
    [Description("First number")] double a,
    [Description("Operator: '+', '-', '*' or '/'")] string op,
    [Description("Second number")] double b)
{
    return op switch
    {
        "+" => $"{a + b}",
        "-" => $"{a - b}",
        "*" => $"{a * b}",
        "/" when b == 0 => "Error: division by zero.",
        "/" => $"{a / b}",
        _ => $"Unsupported operator '{op}'."
    };
}

[Description("Search products in the catalog")]
static List<Product> SearchProducts(
    [Description("Category of the product")] Category category,
    [Description("Maximum number of results (1-10)")] int maxResults = 3,
    [Description("Price range filter")] PriceRange priceRange = PriceRange.All)
{
    Console.WriteLine($"\t[Tool] SearchProducts({category}, {maxResults}, {priceRange})");

    List<Product> all =
    [
        new("Laptop Basic", Category.Laptop, 500),
        new("Laptop Pro", Category.Laptop, 1800),
        new("Phone X", Category.Phone, 900),
        new("Headphone Z", Category.Audio, 120)
    ];

    return all
        .Where(p => p.Category == category)
        .Where(p => priceRange switch
        {
            PriceRange.Low => p.Price < 600,
            PriceRange.Medium => p.Price is >= 600 and < 1500,
            PriceRange.High => p.Price >= 1500,
            _ => true
        })
        .Take(maxResults)
        .ToList();
}

enum Category { Laptop, Phone, Audio }
enum PriceRange { All, Low, Medium, High }
record Product(string Name, Category Category, decimal Price);

#endregion