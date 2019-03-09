using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Grpc.Server
{
    public class CrossChainGrpcServer : CrossChainRpc.CrossChainRpcBase, ICrossChainServer
    {
        public ILogger<CrossChainGrpcServer> Logger { get; }
        private ILocalEventBus LocalEventBus { get; }
        private readonly IBlockchainService _blockchainService;
        private readonly CrossChainGrpcConfigOption _crossChainGrpcConfigOption;
        private global::Grpc.Core.Server _grpcServer;
        private readonly ICertificateStore _certificateStore;
        public CrossChainGrpcServer(IBlockchainService blockchainService,
            IOptionsSnapshot<CrossChainGrpcConfigOption> crossChainGrpcConfigOption, ICertificateStore certificateStore)
        {
            _blockchainService = blockchainService;
            _certificateStore = certificateStore;
            _crossChainGrpcConfigOption = crossChainGrpcConfigOption.Value;
            Logger = NullLogger<CrossChainGrpcServer>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task StartAsync(int chainId)
        {
            if(!_crossChainGrpcConfigOption.LocalServer)
                return;
            _grpcServer = new global::Grpc.Core.Server
            {
                Services = {CrossChainRpc.BindService(this)},
                Ports =
                {
                    new ServerPort(_crossChainGrpcConfigOption.LocalServerIP, _crossChainGrpcConfigOption.LocalServerPort,
                        new SslServerCredentials(
                            new List<KeyCertificatePair> {LoadKeyCertificatePair(ChainHelpers.ConvertChainIdToBase58(chainId))}))
                }
            };
            await Task.Run(() => _grpcServer.Start());
        }

        private KeyCertificatePair LoadKeyCertificatePair(string keyFileName)
        {
            string certificate = _certificateStore.LoadCertificate(keyFileName);
            string privateKey = _certificateStore.LoadKeyStore(keyFileName);
            return new KeyCertificatePair(certificate, privateKey);
        }

        /// <summary>
        /// Response to recording request from side chain node.
        /// Many requests to many responses.
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task RequestParentChainDuplexStreaming(IAsyncStreamReader<RequestCrossChainBlockData> requestStream, 
            IServerStreamWriter<ResponseParentChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogDebug("Parent Chain Server received IndexedInfo message.");

            while (await requestStream.MoveNext())
            {
                var requestInfo = requestStream.Current;
                var requestedHeight = requestInfo.NextHeight;
                var sideChainId = requestInfo.SideChainId;
                
                var block = await GetIrreversibleBlockByHeightAsync(requestedHeight);
                if (block == null)
                {
                    await responseStream.WriteAsync(new ResponseParentChainBlockData
                    {
                        Success = false
                    });
                    continue;
                }

                var res = new ResponseParentChainBlockData
                {
                    Success = true,
                    BlockData = new ParentChainBlockData
                    {
                        Root = new ParentChainBlockRootInfo
                        {
                            ParentChainHeight = requestedHeight,
                            SideChainTransactionsRoot =
                                Hash.LoadByteArray(block.Header.BlockExtraDatas[0].ToByteArray()),
                            ParentChainId = block.Header.ChainId
                        }
                    }
                };

                var indexedSideChainBlockDataResult = GetIndexedSideChainBlockInfoResult(block);
                
                if (indexedSideChainBlockDataResult.Count != 0)
                {
                    var binaryMerkleTree = new BinaryMerkleTree();
                    foreach (var blockInfo in indexedSideChainBlockDataResult)
                    {
                        binaryMerkleTree.AddNode(blockInfo.TransactionMKRoot);
                    }

                    binaryMerkleTree.ComputeRootHash();
                    // This is to tell side chain the merkle path for one side chain block,
                    // which could be removed with subsequent improvement.
                    // This assumes indexing multi blocks from one chain at once, actually only one block every time right now.
                    for (int i = 0; i < indexedSideChainBlockDataResult.Count; i++)
                    {
                        var info = indexedSideChainBlockDataResult[i];
                        if (!info.ChainId.Equals(sideChainId))
                            continue;
                        var merklePath = binaryMerkleTree.GenerateMerklePath(i);
                        res.BlockData.IndexedMerklePath.Add(info.Height, merklePath);
                    }
                }
                await responseStream.WriteAsync(res);
            }
        }
        
        /// <summary>
        /// Response to indexing request from main chain node.
        /// Many requests to many responses.
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task RequestSideChainDuplexStreaming(IAsyncStreamReader<RequestCrossChainBlockData> requestStream, 
            IServerStreamWriter<ResponseSideChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogDebug("Side Chain Server received IndexedInfo message.");

            try
            {
                while (await requestStream.MoveNext())
                {
                    var requestInfo = requestStream.Current;
                    var requestedHeight = requestInfo.NextHeight;
                    
                    var block = await GetIrreversibleBlockByHeightAsync(
                        requestedHeight);
                    if (block == null)
                    {
                        await responseStream.WriteAsync(new ResponseSideChainBlockData
                        {
                            Success = false
                        });
                        continue;
                    }
                    
                    var blockHeader = block.Header;
                    var res = new ResponseSideChainBlockData
                    {
                        Success = blockHeader != null,
                        BlockData = blockHeader == null ? null : new SideChainBlockData
                        {
                            SideChainHeight = requestedHeight,
                            BlockHeaderHash = blockHeader.GetHash(),
                            TransactionMKRoot = blockHeader.MerkleTreeRootOfTransactions,
                            SideChainId = blockHeader.ChainId
                        }
                    };
                    
                    await responseStream.WriteAsync(res);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Side chain server out of service with exception.");
            }
        }

        private IList<SideChainBlockData> GetIndexedSideChainBlockInfoResult(Block block)
        {
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(block.Body.TransactionList.Last().Params);
            return crossChainBlockData.SideChainBlockData;
        }

        public override Task<IndexingRequestResult> RequestIndexing(IndexingRequestMessage request, ServerCallContext context)
        {
            LocalEventBus.PublishAsync(new GrpcServeNewChainReceivedEvent
            {
                CrossChainCommunicationContext = new GrpcCrossChainCommunicationContext
                {
                    TargetIp = request.Ip,
                    TargetPort = request.Port,
                    ChainId = request.SideChainId
                }
            });
            return Task.FromResult(new IndexingRequestResult{Result = true});
        }

        private async Task<Block> GetIrreversibleBlockByHeightAsync( long height)
        {
            var chain = await _blockchainService.GetChainAsync();
            if (chain.LastIrreversibleBlockHeight < height)
                return null;
            var blockHash = await _blockchainService.GetBlockHashByHeightAsync(chain, height);
            return await _blockchainService.GetBlockByHashAsync(blockHash);
        }

        public void Dispose()
        {
            if (_grpcServer == null)
                return;
            _grpcServer.ShutdownAsync();
            _grpcServer = null;
        }
    }
}