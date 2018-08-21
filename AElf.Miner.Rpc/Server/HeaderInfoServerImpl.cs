using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Kernel;
using Grpc.Core;

namespace AElf.Miner.Rpc.Server
{
    public class HeaderInfoServerImpl : HeaderInfoRpc.HeaderInfoRpcBase
    {
        private readonly IChainService _chainService;
        public HeaderInfoServerImpl(IChainService chainService)
        {
            _chainService = chainService;
        }

        //
        public override async Task<ReponseIndexedInfo> GetHeaderInfo(RequestIndexedInfo requestInfo, ServerCallContext context)
        {
            // TODO: verify the from address and the chain 
            
            var res = new ReponseIndexedInfo
            {
                Headers = {}
            };
            ulong height = requestInfo.Height;
            var lightChain = _chainService.GetLightChain(requestInfo.ChainId);
            while (true)
            {
                var curHeight = await lightChain.GetCurrentBlockHeightAsync();
                if (height > curHeight)
                    break;
                var blockHeader = await lightChain.GetHeaderByHeightAsync(height);
                res.Headers.Add(new HeaderInfo
                {
                    BlockHeaderHash = blockHeader.GetHash(),
                    TransactionMKRoot = blockHeader.MerkleTreeRootOfTransactions
                });
                height++;
            }
            return res;
        }
    }
}