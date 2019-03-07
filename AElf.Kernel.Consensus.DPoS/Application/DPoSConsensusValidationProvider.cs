using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class DPoSConsensusValidationProvider : IBlockValidationProvider
    {
        private readonly IConsensusService _consensusService;
        private readonly IBlockExtraDataOrderService _blockExtraDataOrderService;

        public DPoSConsensusValidationProvider(IConsensusService consensusService, IBlockExtraDataOrderService blockExtraDataOrderService)
        {
            _consensusService = consensusService;
            _blockExtraDataOrderService = blockExtraDataOrderService;
        }
        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            if (block.Height == 1)
            {
                return true;
            }

            if (block.Header.BlockExtraDatas.Count == 0)
            {
                return true;
            }

            var result = await _consensusService.ValidateConsensusAsync(block.Header.PreviousBlockHash,
                block.Height - 1,
                block.Header
                    .BlockExtraDatas[
                        _blockExtraDataOrderService.GetExtraDataProviderOrder(typeof(ConsensusExtraDataProvider))]
                    .ToByteArray());
            return result;
        }

        public Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            // TODO: Need a new contract method to validate consensus information after block execution.
            return Task.FromResult(true);
        }
    }
}