using CommonShared;
using HandoffModeDemo.Monitors;
using HandoffModeDemo.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
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

// Initialize Multiple Agents
Console.WriteLine("Now loading the Agents...");
// 分流客服智能体：负责初步分流客户问题
var triageAgent = new ChatCompletionAgent()
{
    Name = "TriageAgent",
    Description = "处理客户请求",
    Instructions = "一个负责分流客户问题的客服智能体",
    Kernel = kernel.Clone()
};
// 订单状态查询智能体：负责处理订单状态相关请求
var statusAgent = new ChatCompletionAgent()
{
    Name = "OrderStatusAgent",
    Description = "一个负责查询订单状态的客服智能体",
    Instructions = "处理订单状态请求",
    Kernel = kernel.Clone()
};
statusAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new OrderStatusPlugin()));
// 订单退货智能体：负责处理订单退货相关请求
var returnAgent = new ChatCompletionAgent()
{
    Name = "OrderReturnAgent",
    Description = "一个负责处理订单退货的客服智能体",
    Instructions = "处理订单退货并记录退货原因（用户需确认原因：不想要了 或 7天无理由退换 或 没有时间消费）",
    Kernel = kernel.Clone()
};
returnAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new OrderReturnPlugin()));
// 订单退款智能体：负责处理订单退款相关请求
var refundAgent = new ChatCompletionAgent()
{
    Name = "OrderRefundAgent",
    Description = "一个负责处理订单退款的客服智能体",
    Instructions = "处理订单退款请求并记录退款原因（用户需确认原因：不想要了 或 7天无理由退换 或 没有时间消费）",
    Kernel = kernel.Clone()
};
refundAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new OrderRefundPlugin()));

// Set up the Orchestration
// -- Initialize one Monitor to track the Orchestration
var monitor = new OrchestrationMonitor();
// -- Define the handoff flow
// -- 交接流程：首先由分流客服智能体处理，然后根据问题类型交接给对应的智能体
var handoffs = OrchestrationHandoffs
        .StartWith(triageAgent)
        .Add(source: triageAgent, targets: [statusAgent, returnAgent, refundAgent]) // 分流客服可交接给状态、退货、退款智能体
        .Add(source: statusAgent, target: triageAgent, "如非订单状态相关问题则交回分流客服")
        .Add(source: returnAgent, target: triageAgent, "如非退货相关问题则交回分流客服")
        .Add(source: refundAgent, target: triageAgent, "如非退款相关问题则交回分流客服");
// -- Create the HandoffOrchestration
var orchestration = new HandoffOrchestration(handoffs, members: [triageAgent, statusAgent, returnAgent, refundAgent])
{
    Name = "CustomerSupportOrchestration",
    Description = "处理客户请求并根据问题类型交接给对应的智能体",
    InteractiveCallback = () =>
    {
        var lastMessage = monitor.History.LastOrDefault();
        Console.WriteLine($"# Agent: \n{lastMessage?.Content}\n");
        Console.WriteLine($"# User:");
        var userInput = Console.ReadLine();
        Console.WriteLine();
        var message = new ChatMessageContent(AuthorRole.User, userInput);
        monitor.History.Add(message);
        return ValueTask.FromResult(message);
    },
    ResponseCallback = monitor.ResponseCallback
};

// Start the Runtime
var runtime = new InProcessRuntime();
await runtime.StartAsync();

// Start the Chat
Console.WriteLine($"Welcome to use CustomerSupport!\n");
var task = "你好，我需要订单上的帮助";
Console.WriteLine($"# User: \n{task}\n");
try
{
    // Invoke the Orchestration
    var result = await orchestration.InvokeAsync(task, runtime);
    // Collect Results from multi Agents
    var output = await result.GetValueAsync(TimeSpan.FromSeconds(100 * 3));
    // Print the Results
    Console.WriteLine($"# 处理结果总结: \n{output}\n");
}
catch (HttpOperationException ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}
finally
{
    await runtime.RunUntilIdleAsync();
    Console.WriteLine();
    Console.WriteLine("----------See you next time!----------");
    Console.ReadKey();
}