using Microsoft.SemanticKernel;

namespace HandoffModeDemo.Plugins;

public sealed class OrderRefundPlugin
{
    [KernelFunction]
    public string ProcessReturn(string orderId, string reason) => $"订单 {orderId} 的退款申请已通过！退款理由：{reason}";
}