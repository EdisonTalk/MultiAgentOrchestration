using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace HandoffModeDemo.Monitors;

public sealed class OrchestrationMonitor
{
    public ChatHistory History { get; } = new ChatHistory();
    public ValueTask ResponseCallback(ChatMessageContent response)
    {
        History.Add(response);
        return ValueTask.CompletedTask;
    }
}