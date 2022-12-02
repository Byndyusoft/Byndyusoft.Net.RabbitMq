using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq.Rpc.Internal
{
    internal static class ExtensionMethods
    {
        public static async Task<object> InvokeAsync(this MethodInfo @this, object obj, params object?[] parameters)
        {
            try
            {
                dynamic awaitable = @this.Invoke(obj, parameters)!;
                await awaitable;
                return awaitable.GetAwaiter().GetResult();
            }
            catch (TargetInvocationException ex) when(ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
    }
}