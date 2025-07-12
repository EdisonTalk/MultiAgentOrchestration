using CommonShared;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

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

// Initialize Chemistry Expert Agent
Console.WriteLine("Now loading the Chemistry Expert Agent...");
var chemist = new ChatCompletionAgent()
{
    Name = "ChemistryExpert",
    Instructions = "You're an expert in chemistry, you can answer questions from a chemistry expert perspective.",
    Description = "Chemistry expert agent for answering questions in the perspective of a chemist.",
    Kernel = kernel
};
// Initialize Physics Expert Agent
Console.WriteLine("Now loading the Physics Expert Agent...");
var physicist = new ChatCompletionAgent()
{
    Name = "PhysicsExpert",
    Instructions = "You're an expert in physics, you can answer questions from a physics expert perspective.",
    Description = "Physics expert agent for answering questions in the perspective of a physicst.",
    Kernel = kernel
};

// Set up the Concurrent Orchestration
var orchestration = new ConcurrentOrchestration(physicist, chemist);

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
        var texts = output.Select(text => $"{text}");
        for (int i = 0; i < texts.Count(); i++)
        {
            Console.WriteLine($"# RESULT {i+1}:{Environment.NewLine}");
            Console.WriteLine($"{texts.ElementAt(i).Trim()}{Environment.NewLine}");
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