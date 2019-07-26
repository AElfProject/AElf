using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Deployer;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.Parallel.Tests
{
    public class ParallelTestHelper : OSTestHelper
    {
        private IReadOnlyDictionary<string, byte[]> _codes;
        
        public new IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ?? (_codes = ContractsDeployer.GetContractCodes<ParallelTestHelper>());
        
        public byte[] BasicFunctionWithParallelContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("BasicFunctionWithParallel")).Value;
        
        public Address BasicFunctionWithParallelContractAddress { get; private set; }

        public ParallelTestHelper(IOsBlockchainNodeContextService osBlockchainNodeContextService,
            IAccountService accountService,
            IMinerService minerService,
            IBlockchainService blockchainService,
            ITxHub txHub,
            ISmartContractAddressService smartContractAddressService,
            IBlockAttachService blockAttachService,
            IStaticChainInformationProvider staticChainInformationProvider,
            ITransactionResultService transactionResultService,
            IOptionsSnapshot<ChainOptions> chainOptions) : base(osBlockchainNodeContextService, accountService,
            minerService, blockchainService, txHub, smartContractAddressService, blockAttachService,
            staticChainInformationProvider, transactionResultService, chainOptions)
        {
        }

        public async Task DeployBasicFunctionWithParallelContract()
        {
            BasicFunctionWithParallelContractAddress = await DeployContract<BasicFunctionWithParallelContract>();
        }
        
        public List<Transaction> GenerateBasicFunctionWithParallelTransactions(int groupCount,int transactionCount)
        {
            var transactions = new List<Transaction>();
            
            for (var i = 0; i < groupCount; i++)
            {
                var keyPair = CryptoHelper.GenerateKeyPair();
                var from = Address.FromPublicKey(keyPair.PublicKey);
                var count = transactionCount / groupCount;
                for (var j = 0; j < count; j++)
                {
                    var address = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey);
                    var transaction = GenerateTransaction(from,
                        BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                            .QueryTwoUserWinMoney),
                        new QueryTwoUserWinMoneyInput
                            {First = from, Second = address});
                    var signature =
                        CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().ToByteArray());
                    transaction.Signature = ByteString.CopyFrom(signature); 

                    transactions.Add(transaction);
                }
            }

            return transactions;
        }
    }
}