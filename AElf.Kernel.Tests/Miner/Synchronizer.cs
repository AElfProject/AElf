using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.SmartContract.Metadata;
using AElf.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using AElf.Kernel.TxMemPool;
using AElf.Runtime.CSharp;
using AElf.Types.CSharp;
using AElf.SmartContract;
using Akka.Actor;
using Akka.IO;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using Google.Protobuf.WellKnownTypes;
using NLog;
using ByteString = Google.Protobuf.ByteString;

namespace AElf.Kernel.Tests.Miner
{
    [UseAutofacTestFramework]
    public class Synchronizer : TestKitBase
    {
        private IChainCreationService _chainCreationService;
        private IChainContextService _chainContextService;
        private IWorldStateDictator _worldStateDictator;
        private ISmartContractManager _smartContractManager;
        private IFunctionMetadataService _functionMetadataService;
        private IExecutingService _executingService;
        private IDataStore _dataStore;
        private IWorldStateStore _worldStateStore;
        private IChangesStore _changesStore;

        public Synchronizer(
            IChainCreationService chainCreationService, IChainContextService chainContextService,
            IChainService chainService, ILogger logger,
            ITransactionResultManager transactionResultManager, ITransactionManager transactionManager,
            FunctionMetadataService functionMetadataService, IExecutingService executingService,
            IChangesStore changesStore, IWorldStateStore worldStateStore, IDataStore dataStore,
            ISmartContractManager smartContractManager, IAccountContextService accountContextService,
            ITxPoolService txPoolService, IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore,
            ITransactionStore transactionStore) : base(new XunitAssertions())
        {

            _chainCreationService = chainCreationService;
            _chainContextService = chainContextService;
            _chainService = chainService;
            _logger = logger;
            _transactionResultManager = transactionResultManager;
            _transactionManager = transactionManager;
            _functionMetadataService = functionMetadataService;
            _executingService = executingService;
            _changesStore = changesStore;
            _worldStateStore = worldStateStore;
            _dataStore = dataStore;
            _worldStateDictator = new WorldStateDictator(worldStateStore, changesStore, dataStore,
                blockHeaderStore, blockBodyStore, transactionStore, _logger);
            _smartContractManager = smartContractManager;
            _accountContextService = accountContextService;

            Initialize();
        }

        private void Initialize()
        {
            _smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner("../../../../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateDictator, _functionMetadataService);
        }
        
        public byte[] OldSmartContractZeroCode
        {
            get
            {
                const string TestContractZeroName = "AElf.Kernel.Tests.TestContractZero";
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath($"../../../../{TestContractZeroName}/bin/Debug/netstandard2.0/{TestContractZeroName}.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        
        
        public byte[] NewSmartContractZeroCode
        {
            get
            {
                return ContractCodes.TestContractZeroCode;
            }
        }
        
        
        public byte[] ExampleContractCode
        {
            get
            {
                return ContractCodes.TestContractCode;
            }
        }
        

        private static int _incrementId;
        private IChainService _chainService;

        
        public ulong NewIncrementId()
        {
            var res = _incrementId;
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) res;
        }
        
        public Block GenerateBlock(Hash chainId, Hash previousHash, ulong index)
        {
            var block = new Block(previousHash)
            {
                Header = new BlockHeader
                {
                    ChainId = chainId,
                    Index = index,
                    PreviousBlockHash = previousHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    MerkleTreeRootOfWorldState = Hash.Default
                }
            };
            block.FillTxsMerkleTreeRootInHeader();
            return block;
        }
        
        public async Task<IChain> CreateChain(byte[] SmartContractZeroCode)
        {
            var chainId = Hash.Generate();
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = SmartContractZeroCode.CalculateHash()
            };

            var chain = await _chainCreationService.CreateNewChainAsync(chainId, new List<SmartContractRegistration>{reg});
            _worldStateDictator.SetChainId(chainId);
            return chain;
        }


        public List<ITransaction> CreateTxs(Hash chainId)
        {
            var contractAddressZero = new Hash(chainId.CalculateHashWith(Globals.GenesisBasicContract)).ToAccount();

            var code = ExampleContractCode;
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            var txnDep = new Transaction()
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack((int)0, code)),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1,
                Type = TransactionType.ContractTransaction
            };
            
            Hash hash = txnDep.GetHash();

            ECSignature signature1 = signer.Sign(keyPair, hash.GetHashBytes());
            txnDep.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txnDep.R = ByteString.CopyFrom(signature1.R); 
            txnDep.S = ByteString.CopyFrom(signature1.S);
            
            var txInv_1 = new Transaction
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = 1,
                MethodName = "Print",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("AElf")),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1,
                Type = TransactionType.ContractTransaction
            };
            ECSignature signature2 = signer.Sign(keyPair, txInv_1.GetHash().GetHashBytes());
            txInv_1.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txInv_1.R = ByteString.CopyFrom(signature2.R); 
            txInv_1.S = ByteString.CopyFrom(signature2.S);
            
            var txInv_2 = new Transaction
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId =txInv_1.IncrementId,
                MethodName = "Print",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("Hoopox")),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1,
                Type = TransactionType.ContractTransaction
            };
            
            ECSignature signature3 = signer.Sign(keyPair, txInv_2.GetHash().GetHashBytes());
            txInv_2.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txInv_2.R = ByteString.CopyFrom(signature3.R); 
            txInv_2.S = ByteString.CopyFrom(signature3.S);
            
            var txs = new List<ITransaction>(){
                txnDep, txInv_1, txInv_2
            };

            return txs;
        }
        
        private IAccountContextService _accountContextService;
        private readonly ILogger _logger;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private ISmartContractRunnerFactory _smartContractRunnerFactory;
        private ISmartContractService _smartContractService;
        private IActorRef _requestor;

        
        [Fact]
        public async Task SyncGenesisBlock_False_Rollback()
        {
            var poolconfig = TxPoolConfig.Default;
            var chain = await CreateChain(NewSmartContractZeroCode);
            poolconfig.ChainId = chain.Id;
            
            var contractTxPool = new ContractTxPool(poolconfig, _logger);
            var dPoSTxPool = new DPoSTxPool(poolconfig, _logger);
            
            var poolService = new TxPoolService(contractTxPool, _accountContextService, _logger, dPoSTxPool);
            
            poolService.Start();
            var block = GenerateBlock(chain.Id, chain.GenesisBlockHash, 1);
            
            var txs = CreateTxs(chain.Id);
            foreach (var transaction in txs)
            {
                await poolService.AddTxAsync(transaction);
            }
            
            Assert.Equal((ulong)0, await poolService.GetWaitingSizeAsync());
            Assert.Equal((ulong)2, await poolService.GetExecutableSizeAsync());
            Assert.True(poolService.TryGetTx(txs[2].GetHash(), out var tx));
            
            block.Body.Transactions.Add(txs[0].GetHash());
            block.Body.Transactions.Add(txs[2].GetHash());

            block.FillTxsMerkleTreeRootInHeader();
            block.Body.BlockHeader = block.Header.GetHash();

            var synchronizer = new BlockExecutor(poolService,
                _chainService, _worldStateDictator, _executingService, null, _transactionManager, _transactionResultManager);

            synchronizer.Start();
            var res = await synchronizer.ExecuteBlock(block);
            Assert.False(res);

            Assert.Equal((ulong)0, await poolService.GetWaitingSizeAsync());
            Assert.Equal((ulong)2, await poolService.GetExecutableSizeAsync());
            //Assert.False(poolService.TryGetTx(txs[2].GetHash(), out tx));
            Assert.True(poolService.TryGetTx(txs[1].GetHash(), out tx));
            Assert.Equal((ulong)0, contractTxPool.GetNonce(tx.From));

            var blockchain = _chainService.GetBlockChain(chain.Id); 
            var curHash = await blockchain.GetCurrentBlockHashAsync();
            var index = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Index;
            Assert.Equal((ulong)0, index);
            Assert.Equal(chain.GenesisBlockHash, curHash);
        }
    }
}