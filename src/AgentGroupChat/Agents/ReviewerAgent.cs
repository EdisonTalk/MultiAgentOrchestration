using AgentGroupChatDemo.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AgentGroupChatDemo.Agents;

public class ReviewerAgent
{
    public const string AgentName = "Reviewer";

    public static ChatCompletionAgent Build(Kernel kernel)
    {
        var toolKernel = kernel.Clone();
        toolKernel.Plugins.AddFromType<ClipboardAccessPlugin>();

        var reviewerAgent = new ChatCompletionAgent()
        {
            Name = AgentName,
            Instructions =
                """
                Your responsibility is to review and identify how to improve user provided content.
                If the user has providing input or direction for content already provided, specify how to address this input.
                Never directly perform the correction or provide example.
                Once the content has been updated in a subsequent response, you will review the content again until satisfactory.
                Always copy satisfactory content to the clipboard using available tools and inform user.

                RULES:
                - Only identify suggestions that are specific and actionable.
                - Verify previous suggestions have been addressed.
                - Never repeat previous suggestions.
                """,
            Kernel = toolKernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };

        return reviewerAgent;
    }
}
