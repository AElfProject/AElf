using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusReaderContextService
    {
        Timestamp GetBlockTime();
        Task<Address> GetAccountAsync();
    }

    public class ConsensusReaderContextService : IConsensusReaderContextService
    {
        private readonly IBlockTimeProvider _blockTimeProvider;
        private readonly IAccountService _accountService;

        public ConsensusReaderContextService(IBlockTimeProvider blockTimeProvider, IAccountService accountService)
        {
            _blockTimeProvider = blockTimeProvider;
            _accountService = accountService;
        }

        public Timestamp GetBlockTime()
        {
            return _blockTimeProvider.GetBlockTime();
        }

        public async Task<Address> GetAccountAsync()
        {
            return Address.FromPublicKey(await _accountService.GetPublicKeyAsync());
        }
    }
}