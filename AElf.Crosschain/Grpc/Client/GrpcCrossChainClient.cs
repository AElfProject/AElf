using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Crosschain.Grpc.Client
{
    public abstract class GrpcCrossChainClient<TResponse> : IGrpcCrossChainClient where TResponse : IResponseIndexingMessage
    {
        public ILogger<GrpcCrossChainClient<TResponse>> Logger {get; set;}
        private int _initInterval;
        private int _adjustedInterval;
        private const int UnavailableConnectionInterval = 1_000;
        private Channel _channel;
        private readonly ClientBase _clientBase;
        protected GrpcCrossChainClient(Channel channel, ClientBase clientBase)
        {
            _channel = channel;
            Logger = NullLogger<GrpcCrossChainClient<TResponse>>.Instance;
            _clientBase = clientBase;
            _adjustedInterval = _initInterval;
        }

        public void UpdateRequestInterval(int initInterval)
        {
            _initInterval = initInterval;
            _adjustedInterval = _initInterval;
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
                        _adjustedInterval = AdjustInterval();
                        continue;
                    }
                    if(!_clientBase.AddNewBlockInfo(response.BlockInfoResult))
                        continue;
                    
                    _adjustedInterval = _initInterval;
                    Logger.LogTrace(
                        $"Received response from chain {response.BlockInfoResult.ChainId.DumpBase58()} at height {response.Height}");
                }
            });

            return responseReaderTask;
        }

        private int AdjustInterval()
        {
            return Math.Min(_adjustedInterval * 2, UnavailableConnectionInterval);
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
                    ChainId = ChainConfig.Instance.ChainId.ConvertBase58ToChainId(),
                    NextHeight = _clientBase.TargetChainHeight
                };
                //Logger.LogTrace($"New request for height {request.NextHeight} to chain {_targetChainId.DumpHex()}");
                await call.RequestStream.WriteAsync(request);
                await Task.Delay(_adjustedInterval);
            }
        }

        /// <summary>
        /// Start to request one by one and also response one bye one.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartDuplexStreamingCall(CancellationToken cancellationToken)
        {
            using (var call = Call())
            {
                while (_channel.State != ChannelState.Ready)
                {
                    await _channel.WaitForStateChangedAsync(_channel.State);
                }
                
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

                        // TODO: maybe improvement for NO wait call, or change the try solution
                        var task = StartDuplexStreamingCall(cancellationToken);
                        return;
                    }

                    Logger.LogError(e, "Miner client stooped with exception.");
                    throw;
                }
                finally
                {
                    await call.RequestStream.CompleteAsync();
                }
                
            }
        }

        protected abstract AsyncDuplexStreamingCall<RequestBlockInfo, TResponse> Call(int milliSeconds = 0);
        protected abstract AsyncServerStreamingCall<TResponse> Call(RequestBlockInfo requestBlockInfo);
    }

    public interface IGrpcCrossChainClient
    {
        Task StartDuplexStreamingCall(CancellationToken cancellationToken);
    }

    
}