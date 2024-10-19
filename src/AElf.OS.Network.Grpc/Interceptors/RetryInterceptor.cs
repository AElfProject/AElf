using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel.SmartContract;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network.Grpc;

public partial class RetryInterceptor : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var currentRetry = 0;

        var headerTimeout = context.GetHeaderStringValue(GrpcConstants.TimeoutMetadataKey, true);
        var timeout = headerTimeout == null ? GrpcConstants.DefaultRequestTimeout : int.Parse(headerTimeout);
        var timeoutSpan = TimeSpan.FromMilliseconds(timeout);

        var headerRetryCount = context.GetHeaderStringValue(GrpcConstants.RetryCountMetadataKey, true);
        var retryCount = headerRetryCount == null
            ? NetworkConstants.DefaultRequestRetryCount
            : int.Parse(headerRetryCount);

        async Task<TResponse> RetryCallback(Task<TResponse> responseTask)
        {
            var response = responseTask;

            // if no problem occured return
            if (!response.IsFaulted) return response.Result;

            // if a problem occured but reached the max retries
            if (currentRetry == retryCount) return response.Result;

            currentRetry++;

            // try again
            var retryContext = BuildNewContext(context, timeoutSpan);

            var result = GetResponseAsync(continuation(request, retryContext), timeoutSpan)
                .ContinueWith(RetryCallback).Unwrap();

            return await result;
        }

        var newContext = BuildNewContext(context, timeoutSpan);
        var responseContinuation = continuation(request, newContext);

        var responseAsync = GetResponseAsync(responseContinuation, timeoutSpan).ContinueWith(RetryCallback)
            .Unwrap();

        return new AsyncUnaryCall<TResponse>(
            responseAsync,
            responseContinuation.ResponseHeadersAsync,
            responseContinuation.GetStatus,
            responseContinuation.GetTrailers,
            responseContinuation.Dispose);
    }

    [ExceptionHandler(typeof(OperationCanceledException), TargetType = typeof(RetryInterceptor),
        MethodName = nameof(HandleExceptionWhileGettingResponse))]
    private async Task<TResponse> GetResponseAsync<TResponse>(AsyncUnaryCall<TResponse> responseContinuation,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource();
        // Ensure that under normal circumstances, the timeout is no earlier than on the server side.
        cts.CancelAfter(timeout.Add(TimeSpan.FromSeconds(1)));
        return await responseContinuation.ResponseAsync.WithCancellation(cts.Token);
    }

    private ClientInterceptorContext<TRequest, TResponse> BuildNewContext<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> oldContext, TimeSpan timeout)
        where TRequest : class
        where TResponse : class
    {
        return new ClientInterceptorContext<TRequest, TResponse>(oldContext.Method, oldContext.Host,
            oldContext.Options.WithDeadline(DateTime.UtcNow.Add(timeout)));
    }
}