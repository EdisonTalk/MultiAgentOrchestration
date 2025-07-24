using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GroupChatModeDemo;

/// <summary>
/// Define a custom group chat manager that enables user input.
/// </summary>
/// <remarks>
/// User input is achieved by overriding the default round robin manager
/// to allow user input after the reviewer agent's message.
/// </remarks>
public class CustomGroupChatManager : RoundRobinGroupChatManager
{
    public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = default)
    {
        // Custom logic to filter or summarize chat results
        return ValueTask.FromResult(new GroupChatManagerResult<string>("Summary") { Reason = "Custom summary logic." });
    }

    public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default)
    {
        // Randomly select an agent from the team
        var random = new Random();
        int index = random.Next(team.Count);
        string nextAgent = team.ElementAt(index).Key;
        return ValueTask.FromResult(new GroupChatManagerResult<string>(nextAgent) { Reason = "Custom selection logic." });
    }

    public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = default)
    {
        // Custom logic to decide if user input is needed
        return ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "No user input required." });
    }

    public override ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, CancellationToken cancellationToken = default)
    {
        // Optionally call the base implementation to check for default termination logic
        var baseResult = base.ShouldTerminate(history, cancellationToken).Result;
        if (baseResult.Value)
        {
            // If the base logic says to terminate, respect it
            return ValueTask.FromResult(baseResult);
        }

        // Custom logic to determine if the chat should terminate
        bool shouldEnd = history.Count > 10; // Example: end after 10 messages
        return ValueTask.FromResult(new GroupChatManagerResult<bool>(shouldEnd) { Reason = "Custom termination logic." });
    }
}
