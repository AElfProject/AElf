using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.BlockService;
using Google.Protobuf;

namespace AElf.Consensus
{
    public class ConsensusExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly IConsensusService _consensusService;

        private readonly IAccountService _accountService;
        
        public ConsensusExtraDataProvider(IConsensusService consensusService, IAccountService accountService)
        {
            _consensusService = consensusService;
            _accountService = accountService;
        }
        
        public async Task FillExtraData(Block block)
        {
            if (block.Header.BlockExtraData == null)
            {
                block.Header.BlockExtraData = new BlockExtraData();
            }

            var consensusInformation =
                _consensusService.GetNewConsensusInformation(block.Header.ChainId,
                    await _accountService.GetAccountAsync());

            block.Header.BlockExtraData.ConsensusInformation = ByteString.CopyFrom(consensusInformation);
        }

        public async Task<bool> ValidateExtraData(Block block)
        {
            var consensusInformation = block.Header.BlockExtraData.ConsensusInformation;

            var result = _consensusService.ValidateConsensus(block.Header.ChainId,
                await _accountService.GetAccountAsync(), consensusInformation.ToByteArray());

            return result;
        }
    }
}