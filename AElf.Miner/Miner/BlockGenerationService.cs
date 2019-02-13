using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.BlockService;
using Google.Protobuf;

namespace AElf.Miner.Miner
{
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IBlockChain _blockChain;
        private readonly IBlockExtraDataService _blockExtraDataService;
        private int ChainId { get; } = ChainConfig.Instance.ChainId.ConvertBase58ToChainId();
        public BlockGenerationService(IChainService chainService, IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
            _blockChain = chainService.GetBlockChain(ChainId);
        }

        public async Task<IBlock> GenerateBlockAsync(HashSet<TransactionResult> results, DateTime currentBlockTime)
        {
            var currentBlockHash = await _blockChain.GetCurrentBlockHashAsync();
            var index = await _blockChain.GetCurrentBlockHeightAsync() + 1;
            
            var block = new Block(currentBlockHash)
            {
                Header =
                {
                    Index = index,
                    ChainId = ChainId,
                    Bloom = ByteString.CopyFrom(
                        Bloom.AndMultipleBloomBytes(
                            results.Where(x => !x.Bloom.IsEmpty).Select(x => x.Bloom.ToByteArray())
                        )
                    )
                }
            };
            
            // todo: get block extra data with _blockExtraDataService including consensus data, cross chain data etc.. 
            await _blockExtraDataService.FillBlockExtraData(block);

            // calculate and set tx merkle tree root 
            block.Complete(currentBlockTime, results);
            return block;
        }
    }
}