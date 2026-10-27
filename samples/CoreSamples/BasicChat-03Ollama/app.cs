#:package OllamaSharp@5.4.18
#:package Microsoft.Extensions.Configuration.UserSecrets@10.0.3
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:package Microsoft.Extensions.AI@10.0.3

using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Text;

IChatClient client =
    new OllamaApiClient(new Uri("http://localhost:11434/"), "phi4-mini");

#region General Examples
// here we're building the prompt
// StringBuilder prompt = new StringBuilder();
// // prompt.AppendLine("You will analyze the sentiment of the following product reviews. Each line is its own review. Output the sentiment of each review in a bulleted list including the original text and the sentiment, and then provide a generate sentiment of all reviews. ");
// // prompt.AppendLine("I bought this product and it's amazing. I love it!");
// // prompt.AppendLine("This product is terrible. I hate it.");
// // prompt.AppendLine("I'm not sure about this product. It's okay.");
// // prompt.AppendLine("I found this product based on the other reviews. It worked for a bit, and then it didn't.");
// prompt.AppendLine("What is AI and summarize about yourself?");

// // send the prompt to the model and wait for the text completion
// var response = await client.GetResponseAsync(prompt.ToString());

// string reviews = @"I bought this product and it's amazing. I very love it!";
// var response = await client.GetResponseAsync<Sentiment>($"What's the sentiment of this review? {reviews}");

// Console.WriteLine(response.Result);

// string[] reviews = [
//     "Best purchase ever!",
//     "Returned it immediately.",
//     "Hello",
//     "It works as advertised.",
//     "The packaging was damaged but otherwise okay."
// ];

// foreach (var review in reviews)
// {
//     var response = await client.GetResponseAsync<Sentiment>(
//         $"What's the sentiment of this review? {review}");
//     Console.WriteLine($"Review: {review} | Sentiment: {response.Result}");
// }
// var review = "This product exceeded my expectations in every way!";
// var response = await client.GetResponseAsync<SentimentAnalysis>(
//     $"Analyze this review: {review}");

// Console.WriteLine($"Text: {response.Result.ResponseText}");
// Console.WriteLine($"Sentiment: {response.Result.ReviewSentiment}");
// Console.WriteLine($"Confidence: {response.Result.ConfidenceScore}");
// Console.WriteLine($"Key phrases: {string.Join(", ", response.Result.KeyPhrases)}");

// public enum Sentiment
// {
//     Positive,
//     Negative,
//     Neutral
// }

// record SentimentAnalysis(
//     string ResponseText,
//     Sentiment ReviewSentiment,
//     double ConfidenceScore,
//     string[] KeyPhrases
// );
#endregion

#region Extracting Contact Information 
// var text = @"
//     Hi, I'm Sarah Johnson from Contoso Ltd. 
//     You can reach me at sarah.johnson@contoso.com 
//     or call my office at 555-0123.
// ";

// var response = await client.GetResponseAsync<ContactInfo>(
//     $"Extract contact information from the following text: {text}");

// var contact = response.Result;
// Console.WriteLine($"Name: {contact.Name}");
// Console.WriteLine($"Email: {contact.Email}");
// Console.WriteLine($"Phone Number: {contact.PhoneNumber}");
// Console.WriteLine($"Company: {contact.Company}");
// record ContactInfo(
//     string Name,
//     string Email,
//     string PhoneNumber,
//     string Company
// );
#endregion

#region Lists and Collections

// var meetingNotes = @"
//     Discussed Q1 roadmap. John will prepare the budget by Friday (high priority).
//     Sarah needs to review the marketing plan next week (medium priority).
//     Team should update documentation ongoing (low priority).
// ";

// var response = await client.GetResponseAsync<List<ActionItem>>(
//     $"Extract all action items from these meeting notes: {meetingNotes}");

// foreach (var item in response.Result)
// {
//     Console.WriteLine($"- {item.Task} | {item.Assignee} | {item.Priority}");
// }

// record ActionItem(
//     string Task,
//     string? Assignee,
//     string? DueDate,
//     string Priority
// );
#endregion

#region Combining Streaming and Structured Output

// Console.Write("You: ");
// var userQuery = Console.ReadLine();

// if (string.IsNullOrWhiteSpace(userQuery))
// {
//     Console.WriteLine("Please enter a question.");
//     return;
// }

// Console.WriteLine("AI is analyzing...");
// string fullText = "";
// await foreach (var update in client.GetStreamingResponseAsync(userQuery))
// {
//     Console.Write(update.Text);
//     fullText += update.Text;
// }

// // Then extract structured data from the response
// var structured = await client.GetResponseAsync<AnalysisResult>(
//     $"Extract key data from: {fullText}");
// Console.WriteLine($"Topic: {structured.Result.Topic}");
// Console.WriteLine($"Summary: {structured.Result.Summary}");
// Console.WriteLine($"Key Points: {string.Join(", ", structured.Result.KeyPoints)}");

// public class AnalysisResult
// {
//     public string Topic { get; set; } = "";
//     public string Summary { get; set; } = "";
//     public List<string> KeyPoints { get; set; } = [];
// }
#endregion

#region ChatOptions for Fine-Tuning Responses
// string prompt = """
// Explain what Dependency Injection is in ASP.NET Core.
// """;

// var options = new ChatOptions
// {
//     Temperature = 0.2f,
//     MaxOutputTokens = 500
// };

// Console.WriteLine("AI is answering...\n");

// await foreach (var update in client.GetStreamingResponseAsync(
//     prompt,
//     options))
// {
//     Console.Write(update.Text);
// }
#endregion

