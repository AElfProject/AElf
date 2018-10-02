using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Kernel;
using Grpc.Core;
using NLog;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Miner.Rpc.Client
{
    public abstract class ClientBase<TResponse> : ClientBase where TResponse : IResponseIndexingMessage
    {
        private readonly ILogger _logger;
        private ulong _next;
        private readonly Hash _targetChainId;
        private readonly int _interval;

        private BlockingCollection<IBlockInfo> IndexedInfoQueue { get; } =
            new BlockingCollection<IBlockInfo>(new ConcurrentQueue<IBlockInfo>());

        protected ClientBase(ILogger logger, Hash targetChainId, int interval)
        {
            _logger = logger;
            _targetChainId = targetChainId;
            _interval = interval;
        }

        /// <summary>
        /// Task to read response in loop.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        private Task ReadResponse(AsyncDuplexStreamingCall<RequestBlockInfo, TResponse> call)
        {
            var responseReaderTask = Task.Run(async () =>
            {
                while (await call.ResponseStream.MoveNext())
                {
                    var response = call.ResponseStream.Current;

                    // request failed or useless response
                    if (!response.Success || response.Height != _next)
                        continue;
                    if (IndexedInfoQueue.TryAdd(response.BlockInfoResult))
                    {
                        _next++;
                        _logger.Debug(
                            $"Received response from chain {response.BlockInfoResult.ChainId} at height {response.Height}");
                    }
                }
            });

            return responseReaderTask;
        }

        /// <summary>
        /// Task to create request in loop.
        /// </summary>
        /// <param name="call"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task RequestLoop(AsyncDuplexStreamingCall<RequestBlockInfo, TResponse> call, 
            CancellationToken cancellationToken)
        {
            // send request every second until cancellation
            while (!cancellationToken.IsCancellationRequested)
            {
                
                var request = new RequestBlockInfo
                {
                    ChainId = Hash.Loads(NodeConfig.Instance.ChainId),
                    NextHeight = IndexedInfoQueue.Count == 0 ? _next : IndexedInfoQueue.Last().Height + 1
                };
                _logger.Trace($"New request for height {request.NextHeight} to chain {_targetChainId.Dumps()}");
                await call.RequestStream.WriteAsync(request);
                await Task.Delay(_interval);
            }
        }

        /// <summary>
        /// Start to request one by one and also response one bye one.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task StartDuplexStreamingCall(CancellationToken cancellationToken, ulong next)
        {
            _next = Math.Max(next, IndexedInfoQueue.LastOrDefault()?.Height?? -1 + 1);
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
                var status = e.Status.StatusCode;
                if (status == StatusCode.Unavailable)
                {
                    var detail = e.Status.Detail;
                    _logger.Error(detail + $" exception during request to chain {_targetChainId.Dumps()}.");
                    StartDuplexStreamingCall(cancellationToken, _next);
                    return;
                }
                _logger.Error(e, "Miner client stooped with exception.");
                throw;
            }
        }

        /// <summary>
        /// Start to request only once but response many (one by one)
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task StartServerStreamingCall(ulong next)
        {
            _next = Math.Max(next, IndexedInfoQueue.Last()?.Height?? -1 + 1);
            try
            {
                var request = new RequestBlockInfo
                {
                    ChainId = Hash.Loads(NodeConfig.Instance.ChainId),
                    NextHeight = IndexedInfoQueue.Count == 0 ? _next : IndexedInfoQueue.Last().Height + 1
                };
                
                using (var call = Call(request))
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var response = call.ResponseStream.Current;

                        // request failed or useless response
                        if (!response.Success || response.Height != _next)
                            continue;
                        if (IndexedInfoQueue.TryAdd(response.BlockInfoResult))
                        {
                            _next++;
                        }
                    }
                }

            }
            catch (RpcException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Try Take element from cached queue.
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="blockInfo"></param>
        /// <returns></returns>
        public bool TryTake(int interval, out IBlockInfo blockInfo)
        {
            return IndexedInfoQueue.TryTake(out blockInfo, interval);
        }
        
        /// <summary>
        /// Take element from cached queue.
        /// </summary>
        /// <returns></returns>
        public IBlockInfo Take()
        {
            return IndexedInfoQueue.Take();
        }

        /// <summary>
        /// Return first element in cached queue.
        /// </summary>
        /// <returns></returns>
        public IBlockInfo First()
        {
            return IndexedInfoQueue.FirstOrDefault();
        }

        public bool Empty()
        {
            return IndexedInfoQueueCount == 0;
        }
            
        /// <summary>
        /// Get cached count.
        /// </summary>
        public int IndexedInfoQueueCount => IndexedInfoQueue.Count;

        protected abstract AsyncDuplexStreamingCall<RequestBlockInfo, TResponse> Call();
        protected abstract AsyncServerStreamingCall<TResponse> Call(RequestBlockInfo requestBlockInfo);
    }

    public abstract class ClientBase
    {
    }
}