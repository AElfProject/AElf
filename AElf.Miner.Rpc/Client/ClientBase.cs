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

namespace AElf.Miner.Rpc.Client
{
    public abstract class ClientBase<TResponse> : ClientBase where TResponse : IResponseIndexingMessage
    {
        private readonly ILogger _logger;
        private ulong _next;
        private readonly Hash _targetChainId;
        private readonly int _interval;

        private BlockingCollection<IBlockInfo> IndexedInfoQueue { get; } = new BlockingCollection<IBlockInfo>(new ConcurrentQueue<IBlockInfo>());

        protected ClientBase(ILogger logger, Hash targetChainId, int interval)
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
        private Task ReadResponse(AsyncDuplexStreamingCall<RequestBlockInfo, TResponse> call)
        {
            var responseReaderTask = Task.Run(async () =>
            {
                while (await call.ResponseStream.MoveNext())
                {
                    var responseStreamCurrent = call.ResponseStream.Current;

                    // request failed or useless response
                    if (!responseStreamCurrent.Success || responseStreamCurrent.Height != _next)
                        continue;
                    if (IndexedInfoQueue.TryAdd(responseStreamCurrent.BlockInfoResult))
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
        private async Task RequestLoop(AsyncDuplexStreamingCall<RequestBlockInfo, TResponse> call, CancellationToken cancellationToken)
        {
            // send request every second until cancellation
            while (!cancellationToken.IsCancellationRequested)
            {
                var request = new RequestBlockInfo
                {
                    ChainId = ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId),
                    NextHeight = IndexedInfoQueue.Count == 0 ? _next : IndexedInfoQueue.Last().Height + 1
                };
                _logger.Debug($"New {typeof(TResponse).Name} for height {request.NextHeight} to chain {_targetChainId.ToHex()}");
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
        /// <param name="blockInfo"></param>
        /// <returns></returns>
        public bool TryTake(int interval, out IBlockInfo blockInfo)
        {
            return IndexedInfoQueue.TryTake(out blockInfo, interval);
        }

        /// <summary>
        /// return first element in cached queue
        /// </summary>
        /// <returns></returns>
        public IBlockInfo First()
        {
            return IndexedInfoQueue.First();
        }
            
        /// <summary>
        /// cached count
        /// </summary>
        public int IndexedInfoQueueCount => IndexedInfoQueue.Count;

        protected abstract AsyncDuplexStreamingCall<RequestBlockInfo, TResponse> Call();
    }

    public abstract class ClientBase
    {
    }
}