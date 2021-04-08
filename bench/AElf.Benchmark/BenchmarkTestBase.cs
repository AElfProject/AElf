using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.ContractDeployer;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Application;
using AElf.OS.Node.Application;
using AElf.TestBase;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Modularity;

namespace AElf.Benchmark
{
    public class BenchmarkTestBase : BenchmarkTestBase<BenchmarkAElfModule>
    {
    }

    public class BenchmarkTestBase<TModule> : AElfIntegratedTest<TModule>
        where TModule : IAbpModule
    {
        protected readonly IBlockchainStore<TransactionResult> TransactionResultStore;

        public BenchmarkTestBase()
        {
            TransactionResultStore = GetRequiredService<IBlockchainStore<TransactionResult>>();
        }

        protected async Task RemoveTransactionResultsAsync(IEnumerable<Hash> transactionIds, Hash disambiguationHash)
        {
            await TransactionResultStore.RemoveAllAsync(transactionIds
                .Select(t => HashHelper.XorAndCompute(t, disambiguationHash).ToStorageKey()).ToList());
        }
    }

    public class MiningWithTransactionsBenchmarkBase : BenchmarkTestBase<MiningBenchmarkAElfModule>
    {
        private readonly IOsBlockchainNodeContextService _osBlockchainNodeContextService;
        private readonly IAccountService _accountService;
        protected readonly IBlockchainService BlockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        protected readonly ITransactionPoolService TransactionPoolService;

        public MiningWithTransactionsBenchmarkBase()
        {
            TransactionPoolService = GetRequiredService<ITransactionPoolService>();
            _osBlockchainNodeContextService = GetRequiredService<IOsBlockchainNodeContextService>();
            _accountService = GetRequiredService<IAccountService>();
            BlockchainService = GetRequiredService<IBlockchainService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        }

        private IReadOnlyDictionary<string, byte[]> _codes;

        public IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ??= ContractsDeployer.GetContractCodes<BenchmarkTestBase>();

        public async Task InitializeChainAsync()
        {
            await StartNodeAsync();
            var chain = await BlockchainService.GetChainAsync();

            if (chain.BestChainHeight == 1)
            {
                var genesisBlock = await BlockchainService.GetBlockByHashAsync(chain.GenesisBlockHash);
                chain = await BlockchainService.GetChainAsync();
                await BlockchainService.SetIrreversibleBlockAsync(chain, genesisBlock.Height, genesisBlock.GetHash());
            }

            await TransactionPoolService.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash,
                chain.BestChainHeight);
        }

        public readonly long TokenTotalSupply = 100_000_000_000_000_000L;
        private readonly string _nativeSymbol = "ELF";

        private async Task StartNodeAsync()
        {
            var ownAddress = await _accountService.GetAccountAsync();
            var callList = new List<ContractInitializationMethodCall>();
            callList.Add(nameof(TokenContractContainer.TokenContractStub.Create), new CreateInput
            {
                Symbol = _nativeSymbol,
                TokenName = "ELF_Token",
                TotalSupply = TokenTotalSupply,
                Decimals = 8,
                Issuer = ownAddress,
                IsBurnable = true
            });
            // callList.Add(nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol),
            //     new SetPrimaryTokenSymbolInput {Symbol = _nativeSymbol});
            callList.Add(nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
            {
                Symbol = _nativeSymbol,
                Amount = TokenTotalSupply,
                To = ownAddress,
                Memo = "Issue"
            });

            var tokenContractCode = Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("MultiToken")).Value;
            var dto = new OsBlockchainNodeContextStartDto
            {
                ZeroSmartContract = typeof(BasicContractZero),
                ChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
                SmartContractRunnerCategory = KernelConstants.CodeCoverageRunnerCategory,
            };
            var genesisSmartContractDto = new GenesisSmartContractDto
            {
                Code = tokenContractCode,
                SystemSmartContractName = TokenSmartContractAddressNameProvider.Name,
            };
            genesisSmartContractDto.AddGenesisTransactionMethodCall(callList.ToArray());
            dto.InitializationSmartContracts.Add(genesisSmartContractDto);

            await _osBlockchainNodeContextService.StartAsync(dto);
        }

        public async Task<List<Transaction>> GenerateTransferTransactionsAsync(int txCount)
        {
            var txList = new List<Transaction>();
            while (txCount-- > 0)
                txList.Add(await GenerateTransferTransactionAsync());
            return txList;
        }

        public async Task<Transaction> GenerateTransferTransactionAsync()
        {
            var chain = await BlockchainService.GetChainAsync();
            var tokenContractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, TokenSmartContractAddressNameProvider.StringName);
            var getBalanceInput = new GetBalanceInput
            {
                Owner = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey),
                Symbol = _nativeSymbol
            };
            var transaction = new Transaction
            {
                From = await _accountService.GetAccountAsync(),
                To = tokenContractAddress,
                MethodName = nameof(TokenContractContainer.TokenContractStub.GetBalance),
                Params = getBalanceInput.ToByteString(),
                RefBlockNumber = chain.BestChainHeight,
                RefBlockPrefix = BlockHelper.GetRefBlockPrefix(chain.BestChainHash)
            };
            var sig = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(sig);

            return transaction;
        }
    }

    public class BenchmarkParallelTestBase : BenchmarkTestBase<BenchmarkParallelAElfModule>
    {
    }
}