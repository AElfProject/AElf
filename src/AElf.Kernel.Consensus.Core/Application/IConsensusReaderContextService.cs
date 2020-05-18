using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusReaderContextService
    {
        Task<ContractReaderContext> GetContractReaderContextAsync(IChainContext chainContext);
    }

    public class ConsensusReaderContextService : IConsensusReaderContextService
    {
        private readonly IBlockTimeProvider _blockTimeProvider;
        private readonly IAccountService _accountService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ConsensusReaderContextService(IBlockTimeProvider blockTimeProvider, IAccountService accountService,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockTimeProvider = blockTimeProvider;
            _accountService = accountService;
            _smartContractAddressService = smartContractAddressService;
        }

        public async Task<ContractReaderContext> GetContractReaderContextAsync(IChainContext chainContext)
        {
            var timestamp = _blockTimeProvider.GetBlockTime();
            var sender = Address.FromPublicKey(await _accountService.GetPublicKeyAsync());
            var consensusContractAddress = await _smartContractAddressService.GetAddressByContractNameAsync(
                chainContext, ConsensusSmartContractAddressNameProvider.StringName);

            return new ContractReaderContext
            {
                BlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight,
                ContractAddress = consensusContractAddress,
                Sender = sender,
                Timestamp = timestamp
            };
        }
    }
}