using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Diagnostics;

namespace AgentGroupChatDemo.Plugins;

public sealed class ClipboardAccessPlugin
{
    [KernelFunction]
    [Description("Copies the provided content to the clipboard.")]
    public static void SetClipboard(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        using var clipProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "clip",
            RedirectStandardInput = true,
            UseShellExecute = false,
        });
        clipProcess.StandardInput.Write(content);
        clipProcess.StandardInput.Close();
    }
}