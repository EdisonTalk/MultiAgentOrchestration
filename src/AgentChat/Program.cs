using AgentGroupChatDemo.Agents;
using CommonShared;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

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

// Initialize Reviewer Agent
Console.WriteLine("Now loading the Reviewer Agent...");
var reviewerAgent = ReviewerAgent.Build(kernel);
// Initialize Writer Agent
Console.WriteLine("Now loading the Writer Agent...");
var writerAgent = WriterAgent.Build(kernel);

// Define Selection Policy
var selectionFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
        Examine the provided RESPONSE and choose the next participant.
        State only the name of the chosen participant without explanation.
        Never choose the participant named in the RESPONSE.

        Choose only from these participants:
        - {{{ReviewerAgent.AgentName}}}
        - {{{WriterAgent.AgentName}}}

        Always follow these rules when choosing the next participant:
        - If RESPONSE is user input, it is {{{ReviewerAgent.AgentName}}}'s turn.
        - If RESPONSE is by {{{ReviewerAgent.AgentName}}}, it is {{{WriterAgent.AgentName}}}'s turn.
        - If RESPONSE is by {{{WriterAgent.AgentName}}}, it is {{{ReviewerAgent.AgentName}}}'s turn.

        RESPONSE:
        {{${{{KernelFunctionTerminationStrategy.DefaultHistoryVariableName}}}}}
        """);
// Define Termination Policy
const string TerminationToken = "yes";
var terminationFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
        Examine the RESPONSE and determine whether the content has been deemed satisfactory.
        If content is satisfactory, respond with a single word without explanation: {{{TerminationToken}}}.
        If specific suggestions are being provided, it is not satisfactory.
        If no correction is suggested, it is satisfactory.

        RESPONSE:
        {{${{{KernelFunctionTerminationStrategy.DefaultHistoryVariableName}}}}}
        """);

// Initialize AgentGroupChat
var historyReducer = new ChatHistoryTruncationReducer(1);
var groupChat = new AgentGroupChat(reviewerAgent, writerAgent)
{
    ExecutionSettings = new AgentGroupChatSettings()
    {
        SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernel)
        {
            InitialAgent = reviewerAgent,
            HistoryReducer = historyReducer,
            HistoryVariableName = KernelFunctionTerminationStrategy.DefaultHistoryVariableName,
            ResultParser = (result) =>
            {
                var val = result.GetValue<string>() ?? ReviewerAgent.AgentName;
                return val.ReplaceLineEndings("\n").Trim();
            }
        },
        TerminationStrategy = new KernelFunctionTerminationStrategy(terminationFunction, kernel)
        {
            Agents = [reviewerAgent],
            HistoryReducer = historyReducer,
            HistoryVariableName = KernelFunctionTerminationStrategy.DefaultHistoryVariableName,
            MaximumIterations = 10,
            ResultParser = (result) =>
            {
                var val = result.GetValue<string>() ?? string.Empty;
                return val.Contains(TerminationToken, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
};

// Start Working!
Console.WriteLine("----------Agents are Ready. Let's Start Working!----------");
while (true)
{
    Console.WriteLine("User> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        continue;
    input = input.Trim();
    if (input.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
        break;
    if (input.Equals("RESET", StringComparison.OrdinalIgnoreCase))
    {
        await groupChat.ResetAsync();
        Console.ResetColor();
        Console.WriteLine("System> Conversation has been reset!");
        continue;
    }
    groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
    groupChat.IsComplete = false;

    try
    {
        await foreach (var response in groupChat.InvokeAsync())
        {
            if (string.IsNullOrWhiteSpace(response.Content))
                continue;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine($"{response.AuthorName} ({response.Role})> ");
            Console.WriteLine($"{response.Content.ReplaceLineEndings("\n").Trim()}");
        }

        Console.ResetColor();
        Console.WriteLine();
    }
    catch (HttpOperationException ex)
    {
        Console.ResetColor();
        Console.WriteLine(ex.Message);
        if (ex.InnerException != null)
        {
            Console.WriteLine(ex.InnerException.Message);
            if (ex.InnerException.Data.Count > 0)
                Console.WriteLine(JsonSerializer.Serialize(ex.InnerException.Data, new JsonSerializerOptions() { WriteIndented = true }));
        }
    }
}

Console.ResetColor();
Console.WriteLine("----------See you next time!----------");
Console.ReadKey();