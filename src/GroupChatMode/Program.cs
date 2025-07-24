using CommonShared;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
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
var writerAgent = new ChatCompletionAgent()
{
    Name = "CopyWriter",
    Instructions = """
                You are a copywriter with ten years of experience and are known for brevity and a dry humor.
                The goal is to refine and decide on the single best copy as an expert in the field.
                Only provide a single proposal per response.
                You're laser focused on the goal at hand.
                Don't waste time with chit chat.
                Consider suggestions when refining an idea.
                """,
    Description = "A copy writer.",
    Kernel = kernel
};
var editorAgent = new ChatCompletionAgent()
{
    Name = "Reviewer",
    Instructions = """
                You are an art director who has opinions about copywriting born of a love for David Ogilvy.
                The goal is to determine if the given copy is acceptable to print.
                If so, state that it is approved.
                If not, provide insight on how to refine suggested copy without example.
                """,
    Description = "An editor.",
    Kernel = kernel
};

// Set up the GroupChat Orchestration
ChatHistory history = [];
ValueTask responseCallback(ChatMessageContent response)
{
    history.Add(response);
    return ValueTask.CompletedTask;
}
// Use RoundRobinGroupChatManager to manage the conversation flow
const string topic = "Create a slogan for a new electric SUV that is affordable and fun to drive.";
var orchestration = new GroupChatOrchestration(
    new RoundRobinGroupChatManager { MaximumInvocationCount = 5 }, // Maximum 5 rounds of conversation
    writerAgent,
    editorAgent)
{
    ResponseCallback = responseCallback
};
// Use AIGroupChatManager to manage the conversation flow
//const string topic = "What does a good life mean to you personally?";
//var orchestration = new GroupChatOrchestration(
//    new AIGroupChatManager(
//     topic,
//     kernel.GetRequiredService<IChatCompletionService>())
//    {
//        MaximumInvocationCount = 5
//    },
//    writerAgent,
//    editorAgent)
//{
//    ResponseCallback = responseCallback
//};

// Start the Runtime
var runtime = new InProcessRuntime();
await runtime.StartAsync();

// Start the Chat
Console.WriteLine($"# INPUT: {topic}{Environment.NewLine}");
try
{
    // Invoke the Orchestration
    var result = await orchestration.InvokeAsync(topic, runtime);
    // Collect Results from multi Agents
    var output = await result.GetValueAsync(TimeSpan.FromSeconds(10 * 3));
    // Print the Results
    Console.WriteLine($"{Environment.NewLine}# RESULT: {output}");
    Console.WriteLine($"{Environment.NewLine}#ORCHESTRATION HISTORY:{Environment.NewLine}");
    foreach (var message in history)
    {
        Console.WriteLine($"#{message.Role} - {message.AuthorName}:");
        Console.WriteLine($"{message.Content}{Environment.NewLine}");
    }
}
catch (HttpOperationException ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}
finally
{
    await runtime.RunUntilIdleAsync();
    Console.ResetColor();
    Console.WriteLine();
}

Console.ResetColor();
Console.WriteLine("----------See you next time!----------");
Console.ReadKey();