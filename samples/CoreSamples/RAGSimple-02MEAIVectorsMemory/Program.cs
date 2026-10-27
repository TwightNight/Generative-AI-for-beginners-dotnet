// This sample demonstrates semantic search using the OFFICIAL .NET VectorData abstraction
// (VectorStoreCollection + VectorData attributes) with an ElBruno sqlite-vec backed store
// instead of Semantic Kernel connectors or a hand-rolled cosine-similarity loop. It is the
// *same* abstraction the chat app uses — swap the backing store with no changes to the
// search code.
//
// Keyless: uses your `az login` credentials (Microsoft Entra ID). Reuses the standard secrets:
//      dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-endpoint>.services.ai.azure.com/"
//      dotnet user-secrets set "AzureOpenAI:EmbeddingDeployment" "text-embedding-3-small"
// Then sign in with: az login

using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using ElBruno.Connectors.SqliteVec;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using OllamaSharp;

#region Config Azure AI OpenAI
// var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
// var endpoint = config["AzureOpenAI:Endpoint"]
//     ?? throw new InvalidOperationException("Set AzureOpenAI:Endpoint in User Secrets.");
// var embeddingDeployment = config["AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-3-small";

// // Keyless Azure OpenAI embeddings (Microsoft Entra ID via `az login`) — no API key in config.
// IEmbeddingGenerator<string, Embedding<float>> generator =
//     new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
//     .GetEmbeddingClient(embeddingDeployment)
//     .AsIEmbeddingGenerator();
#endregion

#region Config Ollama
var ollamaEndpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
const string embeddingModel = "all-minilm";
const string chatModel = "phi4-mini";

// OllamaApiClient implement cả IEmbeddingGenerator và IChatClient
IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaApiClient(new Uri(ollamaEndpoint), embeddingModel);

IChatClient chat = new OllamaApiClient(new Uri(ollamaEndpoint), chatModel);
#endregion

// --- The official VectorData abstraction: a typed collection over a supported local store ---
var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "movie-vectors.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
Console.WriteLine($"Vector store path: {vectorStorePath}\n");
var movies = new SqliteVecVectorStoreCollection<string, MovieVectorRecord>(
    "movies",
    vectorStoreConnectionString,
    generator);

await movies.EnsureCollectionDeletedAsync();
await movies.EnsureCollectionExistsAsync();

// Show the catalog we're searching over (titles only) so the search results have context.
var movieList = MovieFactory<string>.GetMovieList();
Console.WriteLine("Movies in the vector store:");
foreach (var movie in movieList)
{
    Console.WriteLine($"  - {movie.Title}");
}
Console.WriteLine();

// Embed each movie description and upsert it into the collection.
foreach (var movie in movieList)
{
    var embedding = await generator.GenerateVectorAsync(movie.Description!);
    await movies.UpsertAsync(new MovieVectorRecord
    {
        Key = movie.Key,
        Title = movie.Title!,
        Description = movie.Description!,
        Embedding = embedding
    });
}

// Search the collection — embeddings + similarity handled by the VectorData building block.
// The same query path works for any natural-language question: embed it, then search.
string[] queries =
[
    "A family friendly movie that includes ogres and dragons",
    "A movie about a hacker who discovers reality is a simulation"
];

foreach (var query in queries)
{
    var queryEmbedding = await generator.GenerateVectorAsync(query);

    Console.WriteLine($"Query: {query}\n");

    //Retrieve: keep the matched records so they can be used for generation
    var found = new List<MovieVectorRecord>();
    await foreach (var result in movies.SearchAsync(queryEmbedding, top: 1))
    {
        Console.WriteLine($"Score: {result.Score:F3}");
        Console.WriteLine($"  {result.Record.Title}");
        Console.WriteLine($"  {result.Record.Description}\n");
        found.Add(result.Record);
    }

    //Generate: create a quiz from the movies that were just found
    Console.WriteLine($"Generating quiz from {found.Count} movies with {chatModel}...\n");
    var report = await GenerateQuizAsync(chat, query, found, count: 5);

    if (report is null)
    {
        Console.WriteLine("Could not generate a quiz after 3 attempts.\n");
    }
    else
    {
        PrintQuiz(query, report);
    }

    Console.WriteLine(new string('-', 60) + "\n");
}


#region Cosine Similarity
// // Finding similarity for each movie in the catalog.
// string query2 = "A movie about a hacker who discovers reality is a simulation";
// var queryEmbedding2 = await generator.GenerateVectorAsync(query2);

// var movieList2 = MovieFactory<string>.GetMovieList().Select(m => m.Description!).ToList();
// var movieEmbeddings = await generator.GenerateAsync(movieList2);

// // Calculate similarity for each movie in the catalog (all vs all) using the same query path: embed it, then search.
// Console.WriteLine($"Finding similarity for each movie in the catalog (all vs all) for query: {query2}\n");
// var resultsimilars = movieList2
//     .Zip(movieEmbeddings)
//     .Select(m => new
//     {
//         Movie = m.First,
//         Similarity = CosineSimilarity(queryEmbedding2, m.Second.Vector)
//     })
//     .OrderByDescending(m => m.Similarity).ToList();
 
// foreach (var result in resultsimilars)
// {
//     Console.WriteLine($"{result.Similarity:F3} - {result.Movie}");
// }

// static float CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
// {
//     var spanA = a.Span;
//     var spanB = b.Span;
    
//     float dotProduct = 0, normA = 0, normB = 0;
    
//     for (int i = 0; i < spanA.Length; i++)
//     {
//         dotProduct += spanA[i] * spanB[i];
//         normA += spanA[i] * spanA[i];
//         normB += spanB[i] * spanB[i];
//     }
    
//     return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
// }
#endregion


#region ===== Helper functions =====
static async Task<QuizReport?> GenerateQuizAsync(
    IChatClient chat, string topic, List<MovieVectorRecord> records, int count)
{
    if (records.Count == 0) return null;

    var context = string.Join("\n", records.Select(r => $"- {r.Title}: {r.Description}"));

    var prompt = $"""
        Using only the movies listed below (they are the search results for the request "{topic}"),
        create exactly {count} multiple-choice quiz questions.
        Requirements:
        - Each question has exactly 4 options and only 1 correct answer.
        - CorrectIndex is the position of the correct answer in Options, counted from 0 to 3.
        - Explanation briefly explains why that answer is correct.
        - Movie is the title of the movie the question is about.
        - Do not use any knowledge outside the list. Write the questions in English.

        Movies:
        {context}
        """;

    for (int attempt = 1; attempt <= 3; attempt++)
    {
        try
        {
            var response = await chat.GetResponseAsync<QuizReport>(prompt);
            if (!response.TryGetResult(out var parsed)) continue;

            // Filter out malformed questions produced by the small model
            var valid = parsed.Quizzes
                .Where(q => q.Options.Length == 4 && q.CorrectIndex is >= 0 and < 4)
                .ToList();

            if (valid.Count > 0) return new QuizReport(valid);
        }
        catch (JsonException)
        {
            Console.WriteLine($"Attempt {attempt}: invalid JSON, retrying...");
        }
    }

    return null;
}

static void PrintQuiz(string topic, QuizReport report)
{
    Console.WriteLine($"=== Movie Quiz: {topic} ===\n");

    for (int i = 0; i < report.Quizzes.Count; i++)
    {
        var q = report.Quizzes[i];
        Console.WriteLine($"Question {i + 1} ({q.Movie})");
        Console.WriteLine(q.Question);

        for (int k = 0; k < q.Options.Length; k++)
        {
            Console.WriteLine($"  {(char)('A' + k)}. {q.Options[k]}");
        }

        Console.WriteLine($"Answer: {(char)('A' + q.CorrectIndex)}. {q.Options[q.CorrectIndex]}");
        Console.WriteLine($"Explanation: {q.Explanation}\n");
    }
}
#endregion


// all-minilm produces 384-dimension vectors, so the vector field must use the same size.
internal sealed class MovieVectorRecord
{
    [VectorStoreKey]
    public string Key { get; set; } = "";

    [VectorStoreData]
    public string Title { get; set; } = "";

    [VectorStoreData]
    public string Description { get; set; } = "";

    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
internal sealed record QuizItem(
    string Question,
    string[] Options,
    int CorrectIndex,
    string Explanation,
    string Movie);

internal sealed record QuizReport(List<QuizItem> Quizzes);
