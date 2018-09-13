using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NLog;

namespace AElf.Miner.Rpc.Client
{
    public abstract class ClientBase<TRequest, TResponse> where TRequest : IRequestIndexingMessage, new()
        where TResponse : IResponseIndexingMessage
    {
        private readonly ILogger _logger;
        private ulong _next;
        private readonly string _targetChainId;
        private readonly int _interval;

        private BlockingCollection<TResponse> IndexedInfoQueue { get; } = new BlockingCollection<TResponse>(new ConcurrentQueue<TResponse>());

        protected ClientBase(ILogger logger, string targetChainId, int interval)
        {
            _logger = logger;
            _targetChainId = targetChainId;
            _interval = interval;
        }

        /// <summary>
        /// task to read response in loop 
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        private Task ReadResponse(AsyncDuplexStreamingCall<TRequest, TResponse> call)
        {
            var responseReaderTask = Task.Run(async () =>
            {
                while (await call.ResponseStream.MoveNext())
                {
                    var indexedInfo = call.ResponseStream.Current;

                    // request failed or useless response
                    if (!indexedInfo.Success || indexedInfo.Height != _next)
                        continue;
                    if (IndexedInfoQueue.TryAdd(indexedInfo))
                    {
                        _next++;
                    }
                }
            });

            return responseReaderTask;
        }

        /// <summary>
        /// task to create request in loop
        /// </summary>
        /// <param name="call"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task RequestLoop(AsyncDuplexStreamingCall<TRequest, TResponse> call, CancellationToken cancellationToken)
        {
            // send request every second until cancellation
            while (!cancellationToken.IsCancellationRequested)
            {
                var request = new TRequest
                {
                    NextHeight = IndexedInfoQueue.Count == 0 ? _next : IndexedInfoQueue.Last().Height + 1
                };
                _logger.Log(LogLevel.Trace,
                    $"Request IndexedInfo message of height {request.NextHeight} from chain \"{_targetChainId}\"");
                await call.RequestStream.WriteAsync(request);
                await Task.Delay(_interval);
            }
        }

        /// <summary>
        /// start to request
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task Start(CancellationToken cancellationToken, ulong next)
        {
            _next = next;
            try
            {
                using (var call = Call())
                {
                    // response reader task
                    var responseReaderTask = ReadResponse(call);

                    // request in loop
                    await RequestLoop(call, cancellationToken);
                    await call.RequestStream.CompleteAsync();
                    await responseReaderTask;
                }
            }
            catch (RpcException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// take element from cached queue
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="responseIndexingInfo"></param>
        /// <returns></returns>
        public bool TryTake(int interval, out TResponse responseIndexingInfo)
        {
            return IndexedInfoQueue.TryTake(out responseIndexingInfo, interval);
        }

        /// <summary>
        /// cached count
        /// </summary>
        public int IndexedInfoQueueCount => IndexedInfoQueue.Count;

        protected abstract AsyncDuplexStreamingCall<TRequest, TResponse> Call();
    }
}