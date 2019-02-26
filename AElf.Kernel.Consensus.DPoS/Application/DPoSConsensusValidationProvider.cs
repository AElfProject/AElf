using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class DPoSConsensusValidationProvider : IBlockValidationProvider
    {
        private readonly IConsensusService _consensusService;

        public DPoSConsensusValidationProvider(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }
        public async Task<bool> ValidateBlockBeforeExecuteAsync(int chainId, IBlock block)
        {
            if (block.Height == 1)
            {
                return true;
            }

            var result = await _consensusService.ValidateConsensusAsync(chainId, block.Header.PreviousBlockHash,
                block.Height - 1, block.Header.BlockExtraData.ConsensusInformation.ToByteArray());
            return result;
        }

        public Task<bool> ValidateBlockAfterExecuteAsync(int chainId, IBlock block)
        {
            // TODO: Need a new contract method to validate consensus information after block execution.
            return Task.FromResult(true);
        }
    }
}