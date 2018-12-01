using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
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
        private int _interval;
        private int _realInterval;
        private const int UnavailableConnectionInterval = 1_000;
        private readonly int _cachedBoundedCapacity;
        
        private BlockingCollection<IBlockInfo> ToBeIndexedInfoQueue { get; } =
            new BlockingCollection<IBlockInfo>(new ConcurrentQueue<IBlockInfo>());
        private Queue<IBlockInfo> CachedInfoQueue { get; } = new Queue<IBlockInfo>();
        private Channel _channel;
        protected ClientBase(Channel channel, ILogger logger, Hash targetChainId, int interval, int cachedBoundedCapacity)
        {
            _channel = channel;
            _logger = logger;
            _targetChainId = targetChainId;
            _interval = interval;
            _realInterval = _interval;
            _cachedBoundedCapacity = cachedBoundedCapacity;
        }

        public void UpdateRequestInterval(int interval)
        {
            _interval = interval;
            _realInterval = _interval;
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
                    if (!response.Success)
                    {
                        _realInterval = AdjustInterval();
                        continue;
                    }
                    if(response.Height != _next || !ToBeIndexedInfoQueue.TryAdd(response.BlockInfoResult))
                        continue;
                    
                    _next++;
                    _realInterval = _interval;
                    _logger?.Trace($"Received response from chain {response.BlockInfoResult.ChainId} at height {response.Height}");
                }
            });

            return responseReaderTask;
        }

        private int AdjustInterval()
        {
            return Math.Min(_realInterval * 2, UnavailableConnectionInterval);
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
            while (!cancellationToken.IsCancellationRequested)
            {
                var request = new RequestBlockInfo
                {
                    ChainId = Hash.LoadHex(ChainConfig.Instance.ChainId),
                    NextHeight = ToBeIndexedInfoQueue.Count == 0 ? _next : ToBeIndexedInfoQueue.Last().Height + 1
                };
                //_logger.Trace($"New request for height {request.NextHeight} to chain {_targetChainId.DumpHex()}");
                await call.RequestStream.WriteAsync(request);
                await Task.Delay(_realInterval);
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
            _next = Math.Max(next, ToBeIndexedInfoQueue.LastOrDefault()?.Height?? -1 + 1);
            
            using (var call = Call())
            {
                try
                {
                    // response reader task
                    var responseReaderTask = ReadResponse(call);

                    // request in loop
                    await RequestLoop(call, cancellationToken);
                    await responseReaderTask;
                }
                catch (RpcException e)
                {
                    var status = e.Status.StatusCode;
                    if (status == StatusCode.Unavailable || status == StatusCode.DeadlineExceeded)
                    {
                        var detail = e.Status.Detail;
                        _logger?.Warn($"{detail} exception during request to chain {_targetChainId.DumpHex()}.");
                        while (_channel.State != ChannelState.Ready && _channel.State != ChannelState.Idle)
                        {
                            //_logger?.Warn($"Channel state: {_channel.State}");
                            await Task.Delay(UnavailableConnectionInterval);
                        }

                        StartDuplexStreamingCall(cancellationToken, _next).ConfigureAwait(false);
                        return;
                    }

                    _logger?.Error(e, "Miner client stooped with exception.");
                    throw;
                }
                finally
                {
                    await call.RequestStream.CompleteAsync();
                }
                
            }
        }

        /// <summary>
        /// Start to request only once but response many (one by one)
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task StartServerStreamingCall(ulong next)
        {
            _next = Math.Max(next, ToBeIndexedInfoQueue.Last()?.Height?? -1 + 1);
            try
            {
                var request = new RequestBlockInfo
                {
                    ChainId = Hash.LoadHex(ChainConfig.Instance.ChainId),
                    NextHeight = ToBeIndexedInfoQueue.Count == 0 ? _next : ToBeIndexedInfoQueue.Last().Height + 1
                };
                
                using (var call = Call(request))
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var response = call.ResponseStream.Current;

                        // request failed or useless response
                        if (!response.Success || response.Height != _next)
                            continue;
                        if (ToBeIndexedInfoQueue.TryAdd(response.BlockInfoResult))
                        {
                            _next++;
                        }
                    }
                }
            }
            catch (RpcException e)
            {
                _logger.Error(e);
                throw;
            }
        }

        /// <summary>
        /// Try Take element from cached queue.
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        /// <param name="height">the height of block info needed</param>
        /// <param name="blockInfo"></param>
        /// <param name="cachingThreshold">Use <see cref="_cachedBoundedCapacity"/> as cache count threshold if true.</param>
        /// <returns></returns>
        public bool TryTake(int millisecondsTimeout, ulong height, out IBlockInfo blockInfo, bool cachingThreshold = false)
        {
            var first = First();
            if (first != null && first.Height == height && (!cachingThreshold || ToBeIndexedInfoQueue.Count >= _cachedBoundedCapacity))
            {
                var res = ToBeIndexedInfoQueue.TryTake(out blockInfo, millisecondsTimeout);
                if(res)
                    CacheBlockInfo(blockInfo);
                else
                {
                    _logger?.Trace($"Timeout to get cached data from chain {_targetChainId}");
                }
                return res;
            }
            
            blockInfo = CachedInfoQueue.FirstOrDefault(c => c.Height == height);
            if (blockInfo != null)
                return !cachingThreshold ||
                       ToBeIndexedInfoQueue.Count + CachedInfoQueue.Count(ci => ci.Height >= height) >=
                       _cachedBoundedCapacity;
            
            //_logger?.Trace($"Not found cached data from chain {_targetChainId} at height {height}");
            return false;
        }

        /// <summary>
        /// Cache block info lately removed.
        /// Dequeue one element if the cached count reaches <see cref="_cachedBoundedCapacity"/>
        /// </summary>                                                   
        /// <param name="blockInfo"></param>
        private void CacheBlockInfo(IBlockInfo blockInfo)
        {
            CachedInfoQueue.Enqueue(blockInfo);
            if (CachedInfoQueue.Count <= _cachedBoundedCapacity)
                return;
            CachedInfoQueue.Dequeue();
        }

        
        /// <summary>
        /// Return first element in cached queue.
        /// </summary>
        /// <returns></returns>
        private IBlockInfo First()
        {
            return ToBeIndexedInfoQueue.FirstOrDefault();
        }
            
        /// <summary>
        /// Get cached count.
        /// </summary>
        private int IndexedInfoQueueCount => ToBeIndexedInfoQueue.Count;

        protected abstract AsyncDuplexStreamingCall<RequestBlockInfo, TResponse> Call(int milliSeconds = 0);
        protected abstract AsyncServerStreamingCall<TResponse> Call(RequestBlockInfo requestBlockInfo);
    }

    public abstract class ClientBase
    {
    }
}