using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ContractDeployer;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.CrossChain;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Configuration;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using TokenContract = AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub;
using CrossChainContract = AElf.Contracts.CrossChain.CrossChainContractContainer.CrossChainContractStub;
using InitializeInput = AElf.Contracts.CrossChain.InitializeInput;

namespace AElf.Contracts.TestBase;

/// <summary>
///     For testing contracts.
///     Basically we can use this class to:
///     Create a new chain with provided smart contracts deployed,
///     Package chosen transactions and mine a block,
///     execute a block if transactions in block body provided,
///     call a contract method and get the execution result,
///     generate a new tester using exists context,
///     etc.
/// </summary>
/// <typeparam name="TContractTestAElfModule"></typeparam>
public class ContractTester<TContractTestAElfModule> : ITransientDependency
    where TContractTestAElfModule : ContractTestAElfModule
{
    private IReadOnlyDictionary<string, byte[]> _codes;
    public long InitialBalanceOfStarter = 800_000_000_00000000L;
    public List<ECKeyPair> InitialMinerList = new();
    public long InitialTreasuryAmount = 200_000_000_00000000L;
    public bool IsPrivilegePreserved = true;

    public long TokenTotalSupply = 1_000_000_000_00000000L;

    public ContractTester() : this(0, null)
    {
    }

    public ContractTester(int chainId, ECKeyPair keyPair)
    {
        var sampleKeyPairs = SampleECKeyPairs.KeyPairs.Take(3).ToList();
        InitialMinerList.AddRange(sampleKeyPairs);
        KeyPair = keyPair ?? InitialMinerList[1];

        Application =
            AbpApplicationFactory.Create<TContractTestAElfModule>(options =>
            {
                options.UseAutofac();
                if (chainId != 0) options.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });

                options.Services.Configure<ConsensusOptions>(o =>
                {
                    var miners = new List<string>();

                    foreach (var minerKeyPair in InitialMinerList) miners.Add(minerKeyPair.PublicKey.ToHex());

                    o.InitialMinerList = miners;
                    o.MiningInterval = 4000;
                    o.StartTimestamp = new Timestamp { Seconds = 0 };
                });

                if (keyPair != null)
                    options.Services.AddTransient(o =>
                    {
                        var mockService = new Mock<IAccountService>();
                        mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                            Task.FromResult(CryptoHelper.SignWithPrivateKey(KeyPair.PrivateKey, data)));

                        mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(KeyPair.PublicKey);

                        return mockService.Object;
                    });
            });

        Application.Initialize();
    }

    private ContractTester(IAbpApplicationWithInternalServiceProvider application, ECKeyPair keyPair)
    {
        application.Services.AddTransient(o =>
        {
            var mockService = new Mock<IAccountService>();
            mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                Task.FromResult(CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, data)));

            mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(keyPair.PublicKey);

            return mockService.Object;
        });

        Application = application;

        KeyPair = keyPair;
    }

    private ContractTester(IAbpApplicationWithInternalServiceProvider application, int chainId)
    {
        application.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });

        Application = application;
    }

    /// <summary>
    ///     Use default chain id.
    /// </summary>
    /// <param name="keyPair"></param>
    public ContractTester(ECKeyPair keyPair)
    {
        Application =
            AbpApplicationFactory.Create<TContractTestAElfModule>(options =>
            {
                options.UseAutofac();
                options.Services.AddTransient(o =>
                {
                    var mockService = new Mock<IAccountService>();
                    mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                        Task.FromResult(CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, data)));

                    mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(keyPair.PublicKey);

                    return mockService.Object;
                });
            });

        Application.Initialize();

        KeyPair = keyPair;
    }

    /// <summary>
    ///     Use randomized ECKeyPair.
    /// </summary>
    /// <param name="chainId"></param>
    public ContractTester(int chainId)
    {
        Application =
            AbpApplicationFactory.Create<TContractTestAElfModule>(options =>
            {
                options.UseAutofac();
                options.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });
            });
        Application.Initialize();
    }

    public IReadOnlyDictionary<string, byte[]> Codes =>
        _codes ?? (_codes = ContractsDeployer.GetContractCodes<TContractTestAElfModule>());

    public byte[] ConsensusContractCode => GetContractCodeByName(SmartContractTestConstants.Consensus);

    public byte[] TokenContractCode => GetContractCodeByName(SmartContractTestConstants.MultiToken);

    public byte[] CrossChainContractCode => GetContractCodeByName(SmartContractTestConstants.CrossChain);

    public byte[] ParliamentContractCode => GetContractCodeByName(SmartContractTestConstants.Parliament);

    public byte[] ConfigurationContractCode => GetContractCodeByName(SmartContractTestConstants.Configuration);

    public byte[] AssociationContractCode => GetContractCodeByName(SmartContractTestConstants.Association);

    public byte[] ReferendumContractCode => GetContractCodeByName(SmartContractTestConstants.Referendum);

    private IAbpApplicationWithInternalServiceProvider Application { get; }

    public ECKeyPair KeyPair { get; }

    public string PublicKey => KeyPair.PublicKey.ToHex();

    private byte[] GetContractCodeByName(string contractName)
    {
        return Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith(contractName)).Value;
    }

    /// <summary>
    ///     Initial a chain with given chain id (passed to ctor),
    ///     and produce the genesis block with provided smart contract configuration.
    ///     Will deploy consensus contract by default.
    /// </summary>
    /// <returns>Return contract addresses as the param order.</returns>
    public async Task<OsBlockchainNodeContext> InitialChainAsync(
        Action<List<GenesisSmartContractDto>> configureSmartContract = null)
    {
        var osBlockchainNodeContextService =
            Application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
        var chainOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
        var consensusOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ConsensusOptions>>().Value;
        var dto = new OsBlockchainNodeContextStartDto
        {
            ChainId = chainOptions.ChainId,
            ZeroSmartContract = typeof(BasicContractZero),
            SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory
        };

        dto.InitializationSmartContracts.AddGenesisSmartContract(
            ConsensusContractCode,
            ConsensusSmartContractAddressNameProvider.Name,
            GenerateConsensusInitializationCallList(consensusOptions));
        configureSmartContract?.Invoke(dto.InitializationSmartContracts);

        return await osBlockchainNodeContextService.StartAsync(dto);
    }

    public async Task<OsBlockchainNodeContext> InitialChainAsyncWithAuthAsync(
        Action<List<GenesisSmartContractDto>> configureSmartContract = null)
    {
        var osBlockchainNodeContextService =
            Application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
        var contractOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ContractOptions>>().Value;
        var consensusOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ConsensusOptions>>().Value;
        consensusOptions.StartTimestamp = TimestampHelper.GetUtcNow();

        var dto = new OsBlockchainNodeContextStartDto
        {
            ChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
            ZeroSmartContract = typeof(BasicContractZero),
            SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory,
            ContractDeploymentAuthorityRequired = contractOptions.ContractDeploymentAuthorityRequired
        };
        dto.InitializationSmartContracts.AddGenesisSmartContract(
            ConsensusContractCode,
            ConsensusSmartContractAddressNameProvider.Name,
            GenerateConsensusInitializationCallList(consensusOptions));
        configureSmartContract?.Invoke(dto.InitializationSmartContracts);

        var result = await osBlockchainNodeContextService.StartAsync(dto);
        var blockChainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var transactionManager = Application.ServiceProvider.GetRequiredService<ITransactionResultManager>();
        var chain = await blockChainService.GetChainAsync();
        var block = await blockChainService.GetBlockByHashAsync(chain.GenesisBlockHash);
        foreach (var transactionId in block.TransactionIds)
        {
            var transactionResult =
                await transactionManager.GetTransactionResultAsync(transactionId, block.GetHash());
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined, transactionResult.Error);
        }

        return result;
    }

    public async Task<OsBlockchainNodeContext> InitialCustomizedChainAsync(int chainId,
        List<string> initialMiners = null, int miningInterval = 4000,
        Timestamp startTimestamp = null, Action<List<GenesisSmartContractDto>> configureSmartContract = null)
    {
        if (initialMiners == null)
            initialMiners = Enumerable.Range(0, 3).Select(i => SampleECKeyPairs.KeyPairs[i].PublicKey.ToHex())
                .ToList();

        if (startTimestamp == null) startTimestamp = TimestampHelper.GetUtcNow();

        var osBlockchainNodeContextService =
            Application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
        var contractOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ContractOptions>>().Value;
        var dto = new OsBlockchainNodeContextStartDto
        {
            ChainId = chainId,
            ZeroSmartContract = typeof(BasicContractZero),
            SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory,
            ContractDeploymentAuthorityRequired = contractOptions.ContractDeploymentAuthorityRequired
        };

        dto.InitializationSmartContracts.AddGenesisSmartContract(
            ConsensusContractCode,
            ConsensusSmartContractAddressNameProvider.Name,
            GenerateConsensusInitializationCallList(initialMiners, miningInterval, startTimestamp));
        configureSmartContract?.Invoke(dto.InitializationSmartContracts);

        return await osBlockchainNodeContextService.StartAsync(dto);
    }

    private List<ContractInitializationMethodCall>
        GenerateConsensusInitializationCallList(ConsensusOptions consensusOptions)
    {
        var consensusMethodCallList = new List<ContractInitializationMethodCall>();
        consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
            new InitialAElfConsensusContractInput
            {
                IsTermStayOne = true
            });
        consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
            new MinerList
            {
                Pubkeys =
                {
                    consensusOptions.InitialMinerList.Select(ByteStringHelper.FromHexString)
                }
            }.GenerateFirstRoundOfNewTerm(consensusOptions.MiningInterval,
                consensusOptions.StartTimestamp));
        return consensusMethodCallList;
    }

    private List<ContractInitializationMethodCall>
        GenerateConsensusInitializationCallList(List<string> initialMiners,
            int miningInterval, Timestamp startTimestamp)
    {
        var consensusMethodCallList = new List<ContractInitializationMethodCall>();
        consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
            new InitialAElfConsensusContractInput
            {
                IsSideChain = true
            });
        consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
            new MinerList
            {
                Pubkeys =
                {
                    initialMiners.Select(ByteStringHelper.FromHexString)
                }
            }.GenerateFirstRoundOfNewTerm(miningInterval, startTimestamp));
        return consensusMethodCallList;
    }

    public async Task InitialSideChainAsync(int chainId,
        Action<List<GenesisSmartContractDto>> configureSmartContract = null)
    {
        var osBlockchainNodeContextService =
            Application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
        var dto = new OsBlockchainNodeContextStartDto
        {
            ChainId = chainId,
            ZeroSmartContract = typeof(BasicContractZero),
            SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory
        };

        dto.InitializationSmartContracts.AddGenesisSmartContract(
            ConsensusContractCode,
            ConsensusSmartContractAddressNameProvider.Name,
            new List<ContractInitializationMethodCall>
            {
                new()
                {
                    MethodName =
                        nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                    Params = new InitialAElfConsensusContractInput { IsSideChain = true }.ToByteString()
                }
            });
        configureSmartContract?.Invoke(dto.InitializationSmartContracts);

        await osBlockchainNodeContextService.StartAsync(dto);
    }

    /// <summary>
    ///     Same chain, different key pair.
    /// </summary>
    /// <param name="keyPair"></param>
    /// <returns></returns>
    public ContractTester<TContractTestAElfModule> CreateNewContractTester(ECKeyPair keyPair)
    {
        return new ContractTester<TContractTestAElfModule>(Application, keyPair);
    }

    /// <summary>
    ///     Same key pair, different chain.
    /// </summary>
    /// <param name="chainId"></param>
    /// <returns></returns>
    public ContractTester<TContractTestAElfModule> CreateNewContractTester(int chainId)
    {
        return new ContractTester<TContractTestAElfModule>(Application, chainId);
    }

    // TODO: This can be deprecated after Tester reconstructed.
    public T GetService<T>()
    {
        return Application.ServiceProvider.GetService<T>();
    }

    public async Task<byte[]> GetPublicKeyAsync()
    {
        var accountService = Application.ServiceProvider.GetRequiredService<IAccountService>();
        return await accountService.GetPublicKeyAsync();
    }

    public Address GetContractAddress(Hash name)
    {
        return AsyncHelper.RunSync(() => GetContractAddressAsync(name));
    }

    private async Task<Address> GetContractAddressAsync(Hash name)
    {
        var smartContractAddressService =
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
        var blockChainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var chain = await blockChainService.GetChainAsync();
        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        return name == Hash.Empty
            ? smartContractAddressService.GetZeroSmartContractAddress()
            : await smartContractAddressService.GetAddressByContractNameAsync(chainContext, name.ToStorageKey());
    }


    public Address GetZeroContractAddress()
    {
        var smartContractAddressService =
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
        return smartContractAddressService.GetZeroSmartContractAddress();
    }

    public Address GetCallOwnerAddress()
    {
        return Address.FromPublicKey(KeyPair.PublicKey);
    }

    public async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
        IMessage input)
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var refBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
        var tx = new Transaction
        {
            From = Address.FromPublicKey(KeyPair.PublicKey),
            To = contractAddress,
            MethodName = methodName,
            Params = input.ToByteString(),
            RefBlockNumber = refBlock.Height,
            RefBlockPrefix = BlockHelper.GetRefBlockPrefix(refBlock.GetHash())
        };

        var signature = CryptoHelper.SignWithPrivateKey(KeyPair.PrivateKey, tx.GetHash().ToByteArray());
        tx.Signature = ByteString.CopyFrom(signature);

        return tx;
    }

    /// <summary>
    ///     Generate a transaction and sign it by provided key pair.
    /// </summary>
    /// <param name="contractAddress"></param>
    /// <param name="methodName"></param>
    /// <param name="ecKeyPair"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
        ECKeyPair ecKeyPair, IMessage input)
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var refBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
        var paramInfo =
            input == null ? ByteString.Empty : input.ToByteString(); //Add input parameter is null situation
        var tx = new Transaction
        {
            From = Address.FromPublicKey(ecKeyPair.PublicKey),
            To = contractAddress,
            MethodName = methodName,
            Params = paramInfo,
            RefBlockNumber = refBlock.Height,
            RefBlockPrefix = BlockHelper.GetRefBlockPrefix(refBlock.GetHash())
        };

        var signature = CryptoHelper.SignWithPrivateKey(ecKeyPair.PrivateKey, tx.GetHash().ToByteArray());
        tx.Signature = ByteString.CopyFrom(signature);

        return tx;
    }

    /// <summary>
    ///     Mine a block with given normal txs and system txs.
    ///     Normal txs will use tx pool while system txs not.
    /// </summary>
    /// <param name="txs"></param>
    /// <param name="blockTime"></param>
    /// <returns></returns>
    public async Task<BlockExecutedSet> MineAsync(List<Transaction> txs, Timestamp blockTime = null)
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
        return await MineAsync(txs, blockTime, preBlock.GetHash(), preBlock.Height);
    }

    /// <summary>
    ///     Mine a block with only system txs.
    /// </summary>
    /// <returns></returns>
    public async Task<BlockExecutedSet> MineEmptyBlockAsync()
    {
        return await MineAsync(new List<Transaction>());
    }

    public async Task<BlockExecutedSet> MineEmptyBlockAsync(Hash preBlockHash, long preBlockHeight)
    {
        return await MineAsync(new List<Transaction>(), null, preBlockHash, preBlockHeight);
    }

    private async Task<BlockExecutedSet> MineAsync(List<Transaction> txs, Timestamp blockTime, Hash preBlockHash,
        long preBlockHeight)
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var miningService = Application.ServiceProvider.GetRequiredService<IMiningService>();
        var blockAttachService = Application.ServiceProvider.GetRequiredService<IBlockAttachService>();

        var executedBlockSet = await miningService.MineAsync(new RequestMiningDto
        {
            PreviousBlockHash = preBlockHash,
            PreviousBlockHeight = preBlockHeight,
            BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(int.MaxValue),
            TransactionCountLimit = int.MaxValue
        }, txs, blockTime ?? DateTime.UtcNow.ToTimestamp());

        var block = executedBlockSet.Block;

        await blockchainService.AddTransactionsAsync(txs);
        await blockchainService.AddBlockAsync(block);
        await blockAttachService.AttachBlockAsync(block);

        return executedBlockSet;
    }

    /// <summary>
    ///     Gets a specified transactions from their ids
    /// </summary>
    /// <param name="txs"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(IEnumerable<Hash> txs)
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        return await blockchainService.GetTransactionsAsync(txs);
    }

    /// <summary>
    ///     Generate a tx then package the new tx to a new block.
    /// </summary>
    /// <param name="contractAddress"></param>
    /// <param name="methodName"></param>
    /// <param name="input"></param>
    /// <param name="blockTime"></param>
    /// <returns></returns>
    public async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress, string methodName,
        IMessage input, Timestamp blockTime = null)
    {
        var tx = await GenerateTransactionAsync(contractAddress, methodName, KeyPair, input);
        var blockExecutedSet = await MineAsync(new List<Transaction> { tx }, blockTime);
        var result = blockExecutedSet.TransactionResultMap[tx.GetHash()];

        return result;
    }

    /// <summary>
    ///     Generate a tx then package the new tx to a new block.
    /// </summary>
    /// <param name="contractAddress"></param>
    /// <param name="methodName"></param>
    /// <param name="input"></param>
    /// <param name="keyPair"></param>
    /// <returns></returns>
    public async Task<(BlockExecutedSet, Transaction)> ExecuteContractWithMiningReturnBlockAsync(
        Address contractAddress,
        string methodName, IMessage input, ECKeyPair keyPair = null)
    {
        var usingKeyPair = keyPair ?? KeyPair;
        var tx = await GenerateTransactionAsync(contractAddress, methodName, usingKeyPair, input);
        return (await MineAsync(new List<Transaction> { tx }), tx);
    }

    /// <summary>
    ///     Using tx to call a method without mining.
    ///     The state database won't change.
    /// </summary>
    /// <param name="contractAddress"></param>
    /// <param name="methodName"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
        IMessage input)
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var transactionReadOnlyExecutionService =
            Application.ServiceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();
        var tx = await GenerateTransactionAsync(contractAddress, methodName, input);
        var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
        var transactionTrace = await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
        {
            BlockHash = preBlock.GetHash(),
            BlockHeight = preBlock.Height
        }, tx, DateTime.UtcNow.ToTimestamp());

        return transactionTrace.ReturnValue;
    }

    public async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
        IMessage input, DateTime dateTime)
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var transactionReadOnlyExecutionService =
            Application.ServiceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();
        var tx = await GenerateTransactionAsync(contractAddress, methodName, input);
        var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
        var transactionTrace = await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
        {
            BlockHash = preBlock.GetHash(),
            BlockHeight = preBlock.Height
        }, tx, dateTime.ToTimestamp());

        return transactionTrace.ReturnValue;
    }

    public void SignTransaction(ref List<Transaction> transactions, ECKeyPair callerKeyPair)
    {
        foreach (var transaction in transactions)
        {
            var signature =
                CryptoHelper.SignWithPrivateKey(callerKeyPair.PrivateKey, transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
        }
    }

    public void SupplyTransactionParameters(ref List<Transaction> transactions)
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var refBlock = AsyncHelper.RunSync(() => blockchainService.GetBestChainLastBlockHeaderAsync());
        foreach (var transaction in transactions)
        {
            transaction.RefBlockNumber = refBlock.Height;
            transaction.RefBlockPrefix = BlockHelper.GetRefBlockPrefix(refBlock.GetHash());
        }
    }

    public async Task<Chain> GetChainAsync()
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        return await blockchainService.GetChainAsync();
    }

    /// <summary>
    ///     Execute a block and add it to chain database.
    /// </summary>
    /// <param name="block"></param>
    /// <param name="txs"></param>
    /// <returns></returns>
    public async Task ExecuteBlock(Block block, List<Transaction> txs)
    {
        var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var transactionManager = Application.ServiceProvider.GetRequiredService<ITransactionManager>();
        var blockAttachService =
            Application.ServiceProvider.GetRequiredService<IBlockAttachService>();
        await transactionManager.AddTransactionsAsync(txs);
        await blockchainService.AddBlockAsync(block);
        await blockAttachService.AttachBlockAsync(block);
    }

    /// <summary>
    ///     Get the execution result of a tx by its tx id.
    /// </summary>
    /// <param name="txId"></param>
    /// <returns></returns>
    public async Task<TransactionResult> GetTransactionResultAsync(Hash txId)
    {
        var transactionResultQueryService =
            Application.ServiceProvider.GetRequiredService<ITransactionResultQueryService>();
        return await transactionResultQueryService.GetTransactionResultAsync(txId);
    }

    public Address GetAddress(ECKeyPair keyPair)
    {
        return Address.FromPublicKey(keyPair.PublicKey);
    }

    /// <summary>
    ///     Zero Contract and Consensus Contract will deploy independently, thus this list won't contain this two contracts.
    /// </summary>
    /// <returns></returns>
    public Action<List<GenesisSmartContractDto>> GetDefaultContractTypes(Address issuer, out long totalSupply,
        out long dividend, out long balanceOfStarter, bool addDefaultPrivilegedProposer = false)
    {
        totalSupply = TokenTotalSupply;
        dividend = InitialTreasuryAmount;
        balanceOfStarter = InitialBalanceOfStarter;

        var tokenContractCallList = new List<ContractInitializationMethodCall>();
        tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
        {
            Symbol = "ELF",
            TokenName = "Native token",
            TotalSupply = totalSupply,
            Decimals = 8,
            Issuer = issuer,
            Owner = issuer,
            IsBurnable = true
        });
        tokenContractCallList.Add(nameof(TokenContract.SetPrimaryTokenSymbol),
            new SetPrimaryTokenSymbolInput { Symbol = "ELF" });
        tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
        {
            Symbol = "ELF",
            Amount = balanceOfStarter,
            To = Address.FromPublicKey(KeyPair.PublicKey)
        });
        tokenContractCallList.Add(nameof(TokenContract.InitialCoefficients), new Empty());
        var crossChainContractCallList = new List<ContractInitializationMethodCall>
        {
            {
                nameof(CrossChainContract.Initialize),
                new InitializeInput { IsPrivilegePreserved = true }
            }
        };

        var parliamentContractCallList = new List<ContractInitializationMethodCall>();
        var parliamentContractInitializeInput = new Parliament.InitializeInput();
        if (addDefaultPrivilegedProposer)
            parliamentContractInitializeInput.PrivilegedProposer = SampleAddress.AddressList[0];

        parliamentContractCallList.Add(nameof(ParliamentContractContainer.ParliamentContractStub.Initialize),
            parliamentContractInitializeInput);
        return list =>
        {
            list.AddGenesisSmartContract(TokenContractCode, TokenSmartContractAddressNameProvider.Name,
                tokenContractCallList);
            list.AddGenesisSmartContract(ParliamentContractCode, ParliamentSmartContractAddressNameProvider.Name,
                parliamentContractCallList);
            list.AddGenesisSmartContract(CrossChainContractCode, CrossChainSmartContractAddressNameProvider.Name,
                crossChainContractCallList);
            list.AddGenesisSmartContract(ConfigurationContractCode,
                ConfigurationSmartContractAddressNameProvider.Name,
                new List<ContractInitializationMethodCall>());
            list.AddGenesisSmartContract(AssociationContractCode, AssociationSmartContractAddressNameProvider.Name);
            list.AddGenesisSmartContract(ReferendumContractCode, ReferendumSmartContractAddressNameProvider.Name);
        };
    }

    /// <summary>
    ///     System contract dto for side chain initialization.
    /// </summary>
    /// <returns></returns>
    public Action<List<GenesisSmartContractDto>> GetSideChainSystemContract(Address issuer, int mainChainId,
        string symbol,
        out long totalSupply,
        Address proposer, long parentChainHeightOfCreation = 1, Address parentChainTokenContractAddress = null)
    {
        totalSupply = TokenTotalSupply;
        var nativeTokenInfo = new TokenInfo
        {
            Symbol = "ELF",
            Decimals = 2,
            Issuer = issuer,
            Owner = issuer,
            IsBurnable = true,
            TokenName = "elf token",
            TotalSupply = TokenTotalSupply,
            IssueChainId = ChainHelper.ConvertBase58ToChainId("AELF")
        };
        var chainOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
        var tokenInitializationCallList = new List<ContractInitializationMethodCall>();
        tokenInitializationCallList.Add(
            nameof(TokenContract.Create),
            new CreateInput
            {
                Decimals = nativeTokenInfo.Decimals,
                IssueChainId = nativeTokenInfo.IssueChainId,
                Issuer = nativeTokenInfo.Issuer,
                Owner = nativeTokenInfo.Issuer,
                IsBurnable = nativeTokenInfo.IsBurnable,
                Symbol = nativeTokenInfo.Symbol,
                TokenName = nativeTokenInfo.TokenName,
                TotalSupply = nativeTokenInfo.TotalSupply
            });

        if (symbol != "ELF")
        {
            tokenInitializationCallList.Add(
                nameof(TokenContract.Create),
                new CreateInput
                {
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = Address.FromPublicKey(KeyPair.PublicKey),
                    Owner = Address.FromPublicKey(KeyPair.PublicKey),
                    TotalSupply = 1_000_000_000,
                    Symbol = symbol,
                    TokenName = "TEST",
                    IssueChainId = chainOptions.ChainId
                }
            );
        }

        tokenInitializationCallList.Add(nameof(TokenContract.SetPrimaryTokenSymbol),
            new SetPrimaryTokenSymbolInput
            {
                Symbol = symbol
            });
        // side chain creator should not be null

        // if(parentChainTokenContractAddress != null)
        //     tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.InitializeFromParentChain),
        //         new InitializeFromParentChainInput
        //         {
        //             RegisteredOtherTokenContractAddresses = {[mainChainId] = parentChainTokenContractAddress}
        //         });
        tokenInitializationCallList.Add(nameof(TokenContract.InitialCoefficients),
            new Empty());

        var parliamentContractCallList = new List<ContractInitializationMethodCall>();
        var contractOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ContractOptions>>().Value;
        parliamentContractCallList.Add(nameof(ParliamentContractContainer.ParliamentContractStub.Initialize),
            new Parliament.InitializeInput
            {
                PrivilegedProposer = proposer,
                ProposerAuthorityRequired = true
            });
        var crossChainContractCallList = new List<ContractInitializationMethodCall>();
        crossChainContractCallList.Add(nameof(CrossChainContract.Initialize),
            new InitializeInput
            {
                IsPrivilegePreserved = IsPrivilegePreserved,
                ParentChainId = mainChainId,
                CreationHeightOnParentChain = parentChainHeightOfCreation
            });
        return list =>
        {
            list.AddGenesisSmartContract(TokenContractCode, TokenSmartContractAddressNameProvider.Name,
                tokenInitializationCallList);
            list.AddGenesisSmartContract(ParliamentContractCode,
                ParliamentSmartContractAddressNameProvider.Name,
                parliamentContractCallList);
            list.AddGenesisSmartContract(CrossChainContractCode, CrossChainSmartContractAddressNameProvider.Name,
                crossChainContractCallList);
        };
    }
}