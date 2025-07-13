using CommonShared;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

Console.WriteLine("Now loading the configuration...");
var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
    .AddJsonFile($"appsettings.Secrets.json", optional: true, reloadOnChange: true)
#endif
    .Build();

Console.WriteLine("Now loading the chat client...");
var chattingApiConfiguration = new OpenAiConfiguration(
            config.GetSection("LLM:MODEL_ID").Value,
            config.GetSection("LLM:BASE_URL").Value,
            config.GetSection("LLM:API_KEY").Value);
var openAiChattingClient = new HttpClient(new OpenAiHttpHandler(chattingApiConfiguration.EndPoint));
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(chattingApiConfiguration.ModelId, chattingApiConfiguration.ApiKey, httpClient: openAiChattingClient)
    .Build();

// Initialize AI Agents
Console.WriteLine("Now loading the AI Agents...");
var analystAgent = new ChatCompletionAgent()
{
    Name = "Analyst",
    Instructions = """
                You are a marketing analyst. Given a product description, identify:
                - Key features
                - Target audience
                - Unique selling points
                """,
    Description = "A agent that extracts key concepts from a product description.",
    Kernel = kernel
};
var writerAgent = new ChatCompletionAgent()
{
    Name = "CopyWriter",
    Instructions = """
                You are a marketing copywriter. Given a block of text describing features, audience, and USPs,
                compose a compelling marketing copy (like a newsletter section) that highlights these points.
                Output should be short (around 150 words), output just the copy as a single text block.
                """,
    Description = "An agent that writes a marketing copy based on the extracted concepts.",
    Kernel = kernel
};
var editorAgent = new ChatCompletionAgent()
{
    Name = "Editor",
    Instructions = """
                You are an editor. Given the draft copy, correct grammar, improve clarity, ensure consistent tone,
                give format and make it polished. Output the final improved copy as a single text block.
                """,
    Description = "An agent that formats and proofreads the marketing copy.",
    Kernel = kernel
};

// Set up the Sequential Orchestration
ChatHistory history = [];
ValueTask responseCallback(ChatMessageContent response)
{
    history.Add(response);
    return ValueTask.CompletedTask;
}
var orchestration = new SequentialOrchestration(analystAgent, writerAgent, editorAgent)
{
    ResponseCallback = responseCallback
};

// Start the Runtime
var runtime = new InProcessRuntime();
await runtime.StartAsync();

// Start the Chat
Console.WriteLine("----------Agents are Ready. Let's Start Working!----------");
while (true)
{
    Console.WriteLine("User> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        continue;
    input = input.Trim();
    if (input.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
    {
        // Stop the Runtime
        await runtime.RunUntilIdleAsync();
        break;
    }

    try
    {
        // Invoke the Orchestration
        var result = await orchestration.InvokeAsync(input, runtime);
        // Collect Results from multi Agents
        var output = await result.GetValueAsync(TimeSpan.FromSeconds(10 * 2));
        // Print the Results
        Console.WriteLine($"{Environment.NewLine}# RESULT: {output}");
        Console.WriteLine($"{Environment.NewLine}ORCHESTRATION HISTORY");
        foreach (var message in history)
        {
            Console.WriteLine($"#{message.Role} - {message.AuthorName}:");
            Console.WriteLine($"{message.Content.Replace("---", string.Empty)}{Environment.NewLine}");
        }
    }
    catch (HttpOperationException ex)
    {
        Console.WriteLine($"Exception: {ex.Message}");
    }
    finally
    {
        Console.ResetColor();
        Console.WriteLine();
    }
}

Console.ResetColor();
Console.WriteLine("----------See you next time!----------");
Console.ReadKey();