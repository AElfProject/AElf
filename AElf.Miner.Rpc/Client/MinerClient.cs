using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel;
using Grpc.Core;
using NLog;
using NLog.Fluent;

namespace AElf.Miner.Rpc.Client
{
    [LoggerName("MinerClient")]
    public class MinerClient
    {
        private readonly HeaderInfoRpc.HeaderInfoRpcClient _client;
        private ulong _next;
        private readonly ILogger _logger;
        private Hash _targetChainId;

        public BlockingCollection<ResponseIndexedInfoMessage> IndexedInfoQueue { get; } =
            new BlockingCollection<ResponseIndexedInfoMessage>(new ConcurrentQueue<ResponseIndexedInfoMessage>());

        public MinerClient(Channel channel, ILogger logger, Hash targetChainId)
        {
            _logger = logger;
            _targetChainId = targetChainId;
            _client = new HeaderInfoRpc.HeaderInfoRpcClient(channel);
        }

        public async Task Index(CancellationToken cancellationToken, ulong next)
        {
            _next = next;
            try
            {
                using (var call = _client.Index())
                {
                    // response reader task
                    var responseReaderTask = Task.Run(async () =>
                    {
                        while (await call.ResponseStream.MoveNext())
                        {

                            var indexedInfo = call.ResponseStream.Current;

                            // request failed or useless response
                            if(!indexedInfo.Success || indexedInfo.Height != _next)
                                continue;
                            if(IndexedInfoQueue.TryAdd(indexedInfo))
                                _next++;
                        }
                    });

                    // send request every second until cancellation
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var request = new RequestIndexedInfoMessage
                        {
                            NextHeight = IndexedInfoQueue.Count == 0 ? _next : IndexedInfoQueue.Last().Height + 1
                        };
                        _logger.Log(LogLevel.Debug,
                            $"Request IndexedInfo message of height {request.NextHeight} from chain \"{_targetChainId.ToHex()}\"");
                        await call.RequestStream.WriteAsync(request);

                        await Task.Delay(1000);
                    }
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
    }
}