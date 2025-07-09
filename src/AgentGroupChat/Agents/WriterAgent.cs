using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace AgentGroupChatDemo.Agents;

public class WriterAgent
{
    public const string AgentName = "Writer";
    public static ChatCompletionAgent Build(Kernel kernel)
    {
        var writerAgent = new ChatCompletionAgent()
        {
            Name = AgentName,
            Instructions =
                """
                Your sole responsibility is to rewrite content according to review suggestions.

                - Always apply all review direction.
                - Always revise the content in its entirety without explanation.
                - Never address the user.
                """,
            Kernel = kernel
        };

        return writerAgent;
    }
}
