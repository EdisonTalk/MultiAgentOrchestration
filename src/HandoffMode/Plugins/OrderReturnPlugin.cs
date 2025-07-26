using Microsoft.SemanticKernel;

namespace HandoffModeDemo.Plugins;

public sealed class OrderReturnPlugin
{
    [KernelFunction]
    public string ProcessReturn(string orderId, string reason) => $"订单 {orderId} 的退货申请已通过！退货理由：{reason} ";
}