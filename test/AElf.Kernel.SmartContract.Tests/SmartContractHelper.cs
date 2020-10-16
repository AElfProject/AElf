using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ContractDeployer;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Mono.Cecil.Cil;

namespace AElf.Kernel.SmartContract
{
    public class SmartContractHelper
    {
        private readonly ITransactionExecutingService _transactionExecutingService;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly KernelTestHelper _kernelTestHelper;
        
        internal IReadOnlyDictionary<string, byte[]> Codes;

        public SmartContractHelper(ITransactionExecutingService transactionExecutingService,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider, IBlockStateSetManger blockStateSetManger,
            IBlockchainService blockchainService, ITransactionResultManager transactionResultManager,
            KernelTestHelper kernelTestHelper)
        {
            _transactionExecutingService = transactionExecutingService;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _blockStateSetManger = blockStateSetManger;
            _blockchainService = blockchainService;
            _transactionResultManager = transactionResultManager;
            _kernelTestHelper = kernelTestHelper;
            Codes = ContractsDeployer.GetContractCodes<SmartContractTestAElfModule>();
        }

        internal async Task<Block> GenerateBlockAsync(long previousBlockHeight, Hash previousBlockHash,
            List<Transaction> transactions = null)
        {
            var block = _kernelTestHelper.GenerateBlock(previousBlockHeight, previousBlockHash, transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockchainService.AddTransactionsAsync(transactions);
            return block;
        }
        
        internal Transaction BuildDeploySystemSmartContractTransaction(Hash contractName,byte[] contractCode)
        {
            var address = _defaultContractZeroCodeProvider.ContractZeroAddress;
            return new Transaction
            {
                From = address,
                To = address,
                MethodName = nameof(ACS0Container.ACS0Stub.DeploySystemSmartContract),
                Params = new SystemContractDeploymentInput
                {
                    Category = 0,
                    Code = ByteString.CopyFrom(contractCode),
                    Name = contractName
                }.ToByteString()
            };
        }
 
        internal async Task MineBlockAsync(Block block)
        {
            var transactions = block.Body.TransactionIds.Count > 0
                ? await _blockchainService.GetTransactionsAsync(block.TransactionIds)
                : new List<Transaction>();
            var executionReturnSets = await _transactionExecutingService.ExecuteAsync(new TransactionExecutingDto
            {
                BlockHeader = block.Header,
                Transactions = transactions,
            }, CancellationToken.None);

            await _transactionResultManager.AddTransactionResultsAsync(
                executionReturnSets.Select(s => s.TransactionResult).ToList(), block.GetHash());
            
            var blockStateSet = new BlockStateSet
            {
                BlockHash = block.GetHash(),
                PreviousHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Height
            };
            foreach (var stateChange in executionReturnSets.SelectMany(executionReturnSet => executionReturnSet.StateChanges))
            {
                blockStateSet.Changes[stateChange.Key] = stateChange.Value;
            }

            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet);
        }

        internal async Task<Chain> CreateChainWithGenesisContractAsync()
        {
            _defaultContractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(null);
            var chain = await _kernelTestHelper.CreateChain();
            var transaction = BuildDeploySystemSmartContractTransaction(ZeroSmartContractAddressNameProvider.Name,
                Codes["AElf.Contracts.Genesis"]);
            var block = await _blockchainService.GetBlockByHashAsync(chain.BestChainHash);
            block.Body.TransactionIds.Add(transaction.GetHash());
            await _blockchainService.AddTransactionsAsync(new[] {transaction});
            await MineBlockAsync(block);
            return chain;
        }
        
        internal async Task<Chain> CreateChainAsync()
        {
            var chain = await _kernelTestHelper.CreateChain();
            var block = await _blockchainService.GetBlockByHashAsync(chain.BestChainHash);
            await MineBlockAsync(block);
            return chain;
        }
    }
}