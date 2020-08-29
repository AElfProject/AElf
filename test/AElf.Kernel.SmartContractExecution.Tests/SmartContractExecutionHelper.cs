using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.ContractDeployer;
using AElf.Cryptography;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContractExecution
{
    public class SmartContractExecutionHelper
    {
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        private readonly IBlockExecutionResultProcessingService _blockExecutionResultProcessingService;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;

        public SmartContractExecutionHelper(IBlockExecutingService blockExecutingService,
            IBlockchainService blockchainService, IBlockchainExecutingService blockchainExecutingService,
            IBlockExecutionResultProcessingService blockExecutionResultProcessingService, 
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider)
        {
            _blockExecutingService = blockExecutingService;
            _blockchainService = blockchainService;
            _blockchainExecutingService = blockchainExecutingService;
            _blockExecutionResultProcessingService = blockExecutionResultProcessingService;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            ContractCodes= ContractsDeployer.GetContractCodes<SmartContractExecutionTestAElfModule>();
        }

        public IReadOnlyDictionary<string, byte[]> ContractCodes;

        public async Task<Chain> CreateChainAsync()
        {
            _defaultContractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(null);
            var blockHeader = new BlockHeader
            {
                Height = AElfConstants.GenesisBlockHeight,
                PreviousBlockHash = Hash.Empty,
                Time = new Timestamp {Seconds = 0}
            };
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    From = _defaultContractZeroCodeProvider.ContractZeroAddress,
                    To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                    MethodName = nameof(ACS0Container.ACS0Stub.DeploySystemSmartContract),
                    Params = new SystemContractDeploymentInput
                    {
                        Name = ZeroSmartContractAddressNameProvider.Name,
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(ContractCodes["AElf.Contracts.Genesis"]),
                    }.ToByteString()
                },
                new Transaction
                {
                    From = _defaultContractZeroCodeProvider.ContractZeroAddress,
                    To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                    MethodName = nameof(ACS0Container.ACS0Stub.DeploySystemSmartContract),
                    Params = new SystemContractDeploymentInput
                    {
                        Name = ConfigurationSmartContractAddressNameProvider.Name,
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(ContractCodes["AElf.Contracts.Configuration"])
                    }.ToByteString()
                }
            };

            var block = await _blockExecutingService.ExecuteBlockAsync(blockHeader, transactions);
            var chain = await _blockchainService.CreateChainAsync(block.Block, transactions);
            var blockExecutionResult = await _blockchainExecutingService.ExecuteBlocksAsync(new[] {block.Block});
            await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, blockExecutionResult);

            return await _blockchainService.GetChainAsync();
        }

        public async Task<BlockExecutedSet> ExecuteTransactionAsync(Transaction transaction, Chain chain = null)
        {
            chain ??= await _blockchainService.GetChainAsync();
            var blockHeader = new BlockHeader
            {
                Height = chain.BestChainHeight + 1,
                PreviousBlockHash = chain.BestChainHash,
                Time = TimestampHelper.GetUtcNow(),
                SignerPubkey = ByteString.CopyFrom(CryptoHelper.GenerateKeyPair().PublicKey)
            };
            
            var transactions = new List<Transaction>
            {
                transaction
            };

            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(blockHeader, transactions);
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(blockExecutedSet.Block);
            await _blockchainService.AttachBlockToChainAsync(chain, blockExecutedSet.Block);
            var blockExecutionResult = await _blockchainExecutingService.ExecuteBlocksAsync(new[] {blockExecutedSet.Block});
            await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, blockExecutionResult);
            return blockExecutionResult.SuccessBlockExecutedSets.Single();
        }
    }
}