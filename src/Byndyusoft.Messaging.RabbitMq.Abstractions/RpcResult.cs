using System;
using System.Net.Http;

namespace Byndyusoft.Messaging.RabbitMq
{
    public abstract class RpcResult
    {
        public static RpcResult Success(HttpContent response) => new RpcSuccessResult(response);

        public static RpcResult Error(Exception exception) => new RpcErrorResult(exception);
    }

    public sealed class RpcSuccessResult : RpcResult
    {
        public HttpContent Response { get; }

        public RpcSuccessResult(HttpContent response)
        {
            Response = response;
        }
    }

    public sealed class RpcErrorResult : RpcResult
    {
        public Exception Exception { get; }

        public RpcErrorResult(Exception exception)
        {
            Exception = exception;
        }
    }
}