using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Application
{
    public class SystemContractService : ISystemContractService
    {
        private readonly ISmartContractManager _smartContractManager;
        private readonly ISmartContractExecutiveService _executiveService;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;

        public SystemContractService(ISmartContractManager smartContractManager,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            ISmartContractExecutiveService executiveService)
        {
            _smartContractManager = smartContractManager;
            _executiveService = executiveService;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
        }

        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(int chainId,
            IChainContext chainContext, Address address)
        {
            Hash hash = null;
            if (address == Address.BuildContractAddress(chainId, 0))
            {
                hash = _defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash;
            }

            hash = await GetContractHashFromZeroAsync(chainId, chainContext, address);
            return await _smartContractManager.GetAsync(hash);
        }

        private async Task<Hash> GetContractHashFromZeroAsync(int chainId, IChainContext chainContext, Address address)
        {
            var transaction = new Transaction()
            {
                From = Address.Zero,
                To = Address.BuildContractAddress(chainId, 0),
                MethodName = "GetContractInfo",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(address))
            };
            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash()
            };

            var txCtxt = new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                CurrentBlockTime = DateTime.UtcNow,
                Transaction = transaction,
                BlockHeight = chainContext.BlockHeight + 1,
                Trace = trace,
                CallDepth = 0,
            };
            var registration = await _smartContractManager.GetAsync(_defaultContractZeroCodeProvider
                .DefaultContractZeroRegistration.CodeHash);
            var executiveZero = await _executiveService.GetExecutiveAsync(registration);
            await executiveZero.SetTransactionContext(txCtxt).Apply();
            trace.RetVal.Data.DeserializeToString();
            return null;
        }
    }
}