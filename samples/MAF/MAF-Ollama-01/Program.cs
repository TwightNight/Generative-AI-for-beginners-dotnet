using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

using Microsoft.Extensions.AI;
using OllamaSharp;

var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11434/"),
    Timeout = TimeSpan.FromMinutes(5)
};

IChatClient chatClient =
    new OllamaApiClient(httpClient, "gemma4:e4b");

#region Agent Creation
// AIAgent writer = chatClient.AsAIAgent(
//     name: "Writer",
//     instructions: "Write short stories that are engaging and creative, and always add bad jokes to them.");

// AgentResponse response = await writer.RunAsync("Write a story about King Arthur and the Knights of the Round Table.");

// Console.WriteLine(response.Text);
#endregion

#region Agent with Conversation Thread
// // Create an agent
// AIAgent assistant = chatClient.AsAIAgent(
//     name: "Assistant",
//     instructions: "You are a helpful assistant that remembers our conversation.");

// // Create a session to maintain context
// var thread = await assistant.CreateSessionAsync();

// // First message
// var response1 = await assistant.RunAsync("My name is Bruno.", thread);
// Console.WriteLine($"Agent: {response1.Text}");

// // Second message - the agent remembers the name
// var response2 = await assistant.RunAsync("What's my name?", thread);
// Console.WriteLine($"Agent: {response2.Text}");  // Should include "Bruno"

// // Third message - still in context
// var response3 = await assistant.RunAsync("Can you spell it backwards?", thread);
// Console.WriteLine($"Agent: {response3.Text}");  // Should spell "onurB"
#endregion

#region Agent with Tools

// AIAgent assistant = chatClient.AsAIAgent(
//     name: "MultiToolAssistant",
//     instructions: "You are a helpful assistant with access to weather, time, and currency tools.",
//     tools: [
//         AIFunctionFactory.Create(GetWeather),
//         AIFunctionFactory.Create(GetTime),
//         AIFunctionFactory.Create(ConvertCurrency)
//     ]);

// // The agent figures out which tool to use
// var weatherResponse = await assistant.RunAsync("What's the weather in Tokyo?");
// Console.WriteLine(weatherResponse.Text);  // Uses GetWeather

// var timeResponse = await assistant.RunAsync("What time is it in London?");
// Console.WriteLine(timeResponse.Text);  // Uses GetTime

// var currencyResponse = await assistant.RunAsync("Convert 100 USD to EUR");
// Console.WriteLine(currencyResponse.Text);

// [Description("Get the current weather for a given location")]
// static string GetWeather(
//     [Description("The city name to get weather for")] string location)
// {
//     // In a real app, this would call a weather API
//     return $"The weather in {location} is cloudy with a high of 15°C.";
// }

// [Description("Get the current time in a timezone")]
// static string GetTime(string timezone)
//     => $"The current time in {timezone} is {DateTime.UtcNow:HH:mm} UTC.";

// [Description("Convert currency from one type to another")]
// static string ConvertCurrency(string from, string to, decimal amount)
//     => $"{amount} {from} is approximately {amount * 0.85m} {to}.";
#endregion

#region Tools with Complex Parameters
// AIAgent travelAgent = chatClient.AsAIAgent(
//     name: "TravelAgent",
//     instructions: "You are a travel booking assistant.",
//     tools: [AIFunctionFactory.Create(SearchFlights)]);

// // var response = await travelAgent.RunAsync(
// //     "Find me a flight from Seattle to New York for 2 people on March 15th, 2026");

// // Console.WriteLine(response.Text);

// // Agent: streaming
// await foreach (var update in travelAgent.RunStreamingAsync("Find me a flight from Seattle to New York for 2 people on March 15th, 2026"))
// {
//     Console.Write(update.Text);
// }

// [Description("Search for available flights")]
// static string SearchFlights(FlightSearchRequest request)
// {

//     Console.WriteLine($"Searching flights from {request.From} to {request.To} on {request.Date} for {request.Passengers} passengers...");
//     return $"""
//         Found 3 flights from {request.From} to {request.To} on {request.Date}:
//         1. UA123 - Departs 8:00 AM - $350/person
//         2. AA456 - Departs 12:30 PM - $420/person
//         3. DL789 - Departs 6:15 PM - $380/person
//         """;
// }

// public class FlightSearchRequest
// {
//     [Description("Departure airport code (e.g., SEA)")]
//     public string From { get; set; } = "";

//     [Description("Arrival airport code (e.g., JFK)")]
//     public string To { get; set; } = "";

//     [Description("Departure date in YYYY-MM-DD format")]
//     public string Date { get; set; } = "";

//     [Description("Number of passengers")]
//     public int Passengers { get; set; } = 1;
// }
#endregion

#region Sequential Workflow
// AIAgent researcher = chatClient.AsAIAgent(
//     name: "Researcher",
//     instructions: "Research and gather key facts about the topic.");

// AIAgent writer = chatClient.AsAIAgent(
//     name: "Writer",
//     instructions: "Write content based on the research provided.");

// AIAgent editor = chatClient.AsAIAgent(
//     name: "Editor",
//     instructions: "Polish the writing for clarity and engagement.");

// AIAgent factChecker = chatClient.AsAIAgent(
//     name: "FactChecker",
//     instructions: "Verify claims and add [citation needed] where facts cannot be confirmed.");

// // Four-step pipeline
// Workflow pipeline = AgentWorkflowBuilder.BuildSequential(
//     researcher, writer, editor, factChecker);

// var workflowAgent = pipeline.AsAIAgent();

// await foreach (var update in workflowAgent.RunStreamingAsync("Create an article about the history of quantum computing."))
// {
//     Console.Write(update.Text);
// }
#endregion

#region Concurrent Workflow
// AIAgent historyAnalyst = chatClient.AsAIAgent(
//     name: "HistoryAnalyst",
//     instructions: "Brief describe the historical development of the topic, highlighting important milestones.");

// AIAgent applicationsAnalyst = chatClient.AsAIAgent(
//     name: "ApplicationsAnalyst",
//     instructions: "Brief describe current and potential real-world applications of the topic.");

// AIAgent challengesAnalyst = chatClient.AsAIAgent(
//     name: "ChallengesAnalyst",
//     instructions: "Brief describe the key technical and societal challenges related to the topic.");

// Workflow concurrentWorkflow = AgentWorkflowBuilder.BuildConcurrent(
//     "ConcurrentAnalysis",
//     [historyAnalyst, applicationsAnalyst, challengesAnalyst]);

// AIAgent concurrentWorkflowAgent = concurrentWorkflow.AsAIAgent();
// string? currentAuthor = null;

// await foreach (var update in concurrentWorkflowAgent.RunStreamingAsync(
//     "Analyze the history, applications, and challenges of quantum computing."))
// {
//     if (update.AuthorName is { Length: > 0 } author && author != currentAuthor)
//     {
//         currentAuthor = author;
//         Console.WriteLine();
//         Console.WriteLine($"=== {author} ===");
//     }

//     Console.Write(update.Text);
// }

// Console.WriteLine();
#endregion

#region Hand-off Workflow
// AIAgent triageAgent = chatClient.AsAIAgent(
//     name: "Triage",
//     instructions: """
//         Route each request to exactly one specialist:
//         - TravelAgent: travel planning and destination questions.
//         - GeneralAgent: all other questions.
//         Do not answer the user yourself; hand off to the appropriate specialist.
//         """);

// AIAgent travelAgent = chatClient.AsAIAgent(
//     name: "TravelAgent",
//     instructions: "You are a travel assistant. Give concise, practical travel suggestions.");

// AIAgent generalAgent = chatClient.AsAIAgent(
//     name: "GeneralAgent",
//     instructions: "You are a helpful assistant. Answer the user's question clearly and concisely.");

// #pragma warning disable MAAIW001 // Handoff workflows are currently marked for evaluation.
// Workflow handoffWorkflow = AgentWorkflowBuilder
//     .CreateHandoffBuilderWith(triageAgent)
//     .EmitAgentResponseUpdateEvents(true)
//     .EmitAgentResponseEvents(true)
//     .WithHandoffs(triageAgent, [travelAgent, generalAgent])
//     .Build();
// #pragma warning restore MAAIW001

// AIAgent handoffWorkflowAgent = handoffWorkflow.AsAIAgent(
//     includeWorkflowOutputsInResponse: true);
// const string travelQuestion = "I'm planning a weekend trip to Kyoto. What should I do?";
// AgentResponse handoffResponse = await handoffWorkflowAgent.RunAsync(travelQuestion);

// if (string.IsNullOrWhiteSpace(handoffResponse.Text))
// {
//     Console.WriteLine("The workflow returned no text; asking the travel specialist directly.");
//     handoffResponse = await travelAgent.RunAsync(travelQuestion);
// }

// if (string.IsNullOrWhiteSpace(handoffResponse.Text))
// {
//     throw new InvalidOperationException("The agent did not return a text response.");
// }

// Console.WriteLine(handoffResponse.Text);
#endregion