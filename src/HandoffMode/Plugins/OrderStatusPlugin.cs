using Microsoft.SemanticKernel;

namespace HandoffModeDemo.Plugins;

public sealed class OrderStatusPlugin
{
    [KernelFunction]
    public string CheckOrderStatus(string orderId) => $"订单 {orderId} 已发货 并将于 2-3日内送达！";
}