using System;

namespace Byndyusoft.Messaging.RabbitMq.Rpc
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RpcFromServicesAttribute : Attribute
    {
    }
}