using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.SmartContract;
using AElf.Kernel.Managers;
using AElf.Kernel.TxMemPool;
using AsyncEventAggregator;
using Google.Protobuf;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
{
    [UseAutofacTestFramework]
    public class TxPoolServiceTest
    {
        private readonly IAccountContextService _accountContextService;
        private readonly ILogger _logger;
        private readonly IStateDictator _stateDictator;
        public TxPoolServiceTest(IAccountContextService accountContextService, ILogger logger, 
            IStateDictator stateDictator)
        {
            _accountContextService = accountContextService;
            _logger = logger;
            _stateDictator = stateDictator;
            _stateDictator.BlockProducerAccountAddress = Hash.Generate();
            this.Subscribe<TransactionAddedToPool>(async (t) => { await Task.CompletedTask; });
        }

        private ContractTxPool GetContractTxPool(ITxPoolConfig config)
        {
            _stateDictator.SetChainId(config.ChainId);
            return new ContractTxPool(config, _logger);
        }
        
        private DPoSTxPool GetDPoSTxPool(ITxPoolConfig config)
        {
            _stateDictator.SetChainId(config.ChainId);
            return new DPoSTxPool(config, _logger);
        }

        [Fact]
        public async Task AddTxTest()
        {
            var config = TxPoolConfig.Default;
            var pool = GetContractTxPool(config);
            var dPoSPool = GetDPoSTxPool(config);

            var poolService = new TxPoolService(pool, _accountContextService, _logger, dPoSPool);
            poolService.Start();

            var addr11 = Hash.Generate();

            var tx1 = BuildTransaction();
            var res = await poolService.AddTxAsync(tx1);
            Assert.Equal(TxValidation.TxInsertionAndBroadcastingError.Success, res);

            Assert.Equal(0, (int) await poolService.GetWaitingSizeAsync());
            Assert.Equal(1, (int) await poolService.GetExecutableSizeAsync());
            Assert.Equal(1, (int) pool.Size);
        }

        [Fact]
        public async Task WaitingTest()
        {
            var config = TxPoolConfig.Default;
            var pool = GetContractTxPool(config);
            var dPoSPool = GetDPoSTxPool(config);

            var poolService = new TxPoolService(pool, _accountContextService, _logger, dPoSPool);
            poolService.Start();
            var tx1 = BuildTransaction();
            var res = await poolService.AddTxAsync(tx1);
            
            var tx2 = BuildTransaction(nonce:2);
            res = await poolService.AddTxAsync(tx2);
            
            Assert.Equal(TxValidation.TxInsertionAndBroadcastingError.Success, res);

            Assert.Equal(1, (int) await poolService.GetWaitingSizeAsync());
            Assert.Equal(1, (int) await poolService.GetExecutableSizeAsync());
            Assert.Equal(2, (int)pool.Size);

        }


        [Fact]
        public async Task ReadyTxs()
        {
            var config = TxPoolConfig.Default;
            var pool = GetContractTxPool(config);
            var dPoSPool = GetDPoSTxPool(config);

            var poolService = new TxPoolService(pool, _accountContextService, _logger, dPoSPool);
            poolService.Start();

            var keyPair = new KeyPairGenerator().Generate();
            var tx1 = BuildTransaction(keyPair: keyPair);
            var tx2 = BuildTransaction(nonce: 1, keyPair:keyPair);
            await poolService.AddTxAsync(tx1);
            await poolService.AddTxAsync(tx2);
            var txs1 = await poolService.GetReadyTxsAsync();
            Assert.Equal(2, txs1.Count);

            var txResults1 = txs1.Select(t => new TransactionResult
            {
                TransactionId = t.GetHash()
            }).ToList();

            await poolService.UpdateAccountContext(new HashSet<Hash>{keyPair.GetAddress()});

            var addr1 = keyPair.GetAddress();
            var context1 = await _accountContextService.GetAccountDataContext(addr1, pool.ChainId);
            Assert.Equal(2, (int)context1.IncrementId);

            
            var tx3 = BuildTransaction(nonce:2, keyPair:keyPair);
            var tx4 = BuildTransaction(nonce: 3, keyPair:keyPair);
            
            await poolService.AddTxAsync(tx3);
            await poolService.AddTxAsync(tx4);
            
            var txs2 = await poolService.GetReadyTxsAsync();
            Assert.Equal(2, txs2.Count);

            var txResults2 = txs2.Select(t => new TransactionResult
            {
                TransactionId = t.GetHash()
            }).ToList();

            await poolService.UpdateAccountContext(new HashSet<Hash>{addr1});
            var context2 = await _accountContextService.GetAccountDataContext(addr1, pool.ChainId);
            Assert.Equal(4, (int)context2.IncrementId);

        }

        [Fact]
        public async Task StopTest()
        {
            var config = TxPoolConfig.Default;
            var pool = GetContractTxPool(config);
            var dPoSPool = GetDPoSTxPool(config);

            var poolService = new TxPoolService(pool, _accountContextService, _logger, dPoSPool);
            poolService.Start();
            
            await poolService.Stop();

            var tx = BuildTransaction();
            var res = await poolService.AddTxAsync(tx);
            Assert.Equal(TxValidation.TxInsertionAndBroadcastingError.PoolClosed, res);
        }


        [Fact]
        public async Task AddMultiTxs()
        {
            var config = TxPoolConfig.Default;
            var pool = GetContractTxPool(config);
            var dPoSPool = GetDPoSTxPool(config);

            var poolService = new TxPoolService(pool, _accountContextService, _logger, dPoSPool);
            poolService.Start();
            var kp = new KeyPairGenerator().Generate();
            var tx1 = BuildTransaction(keyPair:kp, nonce: 0);
            var tx2 = BuildTransaction(keyPair:kp, nonce: 1);
            var tx2_1 = BuildTransaction(adrTo:Hash.Generate().ToAccount(), keyPair: kp, nonce: 1);
            var r2 = await poolService.AddTxAsync(tx2);
            Assert.Equal(TxValidation.TxInsertionAndBroadcastingError.Success, r2);
            var r2_1 = await poolService.AddTxAsync(tx2_1);
            Assert.Equal(TxValidation.TxInsertionAndBroadcastingError.Success, r2_1);

            Assert.True(poolService.TryGetTx(tx2_1.GetHash(), out var tx));
            Assert.Equal((ulong)0, await poolService.GetExecutableSizeAsync());
            Assert.Equal((ulong)1, await poolService.GetWaitingSizeAsync());
            var r1 = await poolService.AddTxAsync(tx1);
            Assert.Equal(TxValidation.TxInsertionAndBroadcastingError.Success, r1);

            Assert.True(poolService.TryGetTx(tx2_1.GetHash(), out tx));
            Assert.Equal((ulong)2, await poolService.GetExecutableSizeAsync());
            Assert.Equal((ulong)0, await poolService.GetWaitingSizeAsync());
            
       }

        [Fact]
        public async Task RollBackTest()
        {
            var config = TxPoolConfig.Default;
            var pool = GetContractTxPool(config);
            var dPoSPool = GetDPoSTxPool(config);

            var poolService = new TxPoolService(pool, _accountContextService, _logger, dPoSPool);
            poolService.Start();
            
            var kp1 = new KeyPairGenerator().Generate();
            pool.TrySetNonce(kp1.GetAddress(), 2);
            var tx1_0 = BuildTransaction(nonce: 2, keyPair:kp1);
            var tx1_1 = BuildTransaction(nonce: 3, keyPair:kp1);
            await poolService.AddTxAsync(tx1_0);
            await poolService.AddTxAsync(tx1_1);
            var tx1_4 = BuildTransaction(nonce: 0, keyPair: kp1);
            var tx1_5 = BuildTransaction(nonce: 1, keyPair: kp1);

            var kp2 = new KeyPairGenerator().Generate();
            pool.TrySetNonce(kp2.GetAddress(), 1);
            var tx2_0 = BuildTransaction(nonce: 3, keyPair:kp2);
            var tx2_1 = BuildTransaction(nonce: 4, keyPair:kp2);
            await poolService.AddTxAsync(tx2_0);
            await poolService.AddTxAsync(tx2_1);
            var tx2_2 = BuildTransaction(nonce: 0, keyPair: kp2);

            var kp3 = new KeyPairGenerator().Generate();
            pool.TrySetNonce(kp3.GetAddress(), 1);
            var tx3_0 = BuildTransaction(nonce:1, keyPair:kp3);
            var tx3_1 = BuildTransaction(nonce: 3, keyPair: kp3);
            await poolService.AddTxAsync(tx3_0);
            await poolService.AddTxAsync(tx3_1);
            var tx3_2 = BuildTransaction(nonce: 0, keyPair: kp3);

            var kp4 = new KeyPairGenerator().Generate();
            pool.TrySetNonce(kp4.GetAddress(), 3);
            var tx4_0 = BuildTransaction(nonce:1, keyPair:kp4);
            var tx4_1 = BuildTransaction(nonce:2, keyPair:kp4);
            
            
            await poolService.RollBack(new List<Transaction>{tx1_4, tx1_5, tx2_2, tx3_2, tx4_1, tx4_0});
            
            Assert.Equal((ulong)0, pool.GetNonce(kp1.GetAddress()).Value);
            Assert.Equal((ulong)0, pool.GetNonce(kp2.GetAddress()).Value);
            Assert.Equal((ulong)0, pool.GetNonce(kp3.GetAddress()).Value);
            Assert.Equal((ulong)1, pool.GetNonce(kp4.GetAddress()).Value);


            Assert.Equal((ulong) 0,
                (await _accountContextService.GetAccountDataContext(kp1.GetAddress(), pool.ChainId)).IncrementId);
            Assert.Equal((ulong) 0,
                (await _accountContextService.GetAccountDataContext(kp2.GetAddress(), pool.ChainId)).IncrementId);
            Assert.Equal((ulong) 0,
                (await _accountContextService.GetAccountDataContext(kp3.GetAddress(), pool.ChainId)).IncrementId);
            Assert.Equal((ulong) 1,
                (await _accountContextService.GetAccountDataContext(kp4.GetAddress(), pool.ChainId)).IncrementId);

            await poolService.GetReadyTxsAsync();
            Assert.Equal((ulong)4, pool.GetNonce(kp1.GetAddress()).Value);
            Assert.Equal((ulong)1, pool.GetNonce(kp2.GetAddress()).Value);
            Assert.Equal((ulong)2, pool.GetNonce(kp3.GetAddress()).Value);
            Assert.Equal((ulong)3, pool.GetNonce(kp4.GetAddress()).Value);
        }


        public static Transaction BuildTransaction(Hash adrTo = null, ulong nonce = 0, ECKeyPair keyPair = null)
        {
            keyPair = keyPair ?? new KeyPairGenerator().Generate();

            var tx = new Transaction();
            tx.From = keyPair.GetAddress();
            tx.To = (adrTo == null ? Hash.Generate().ToAccount() : adrTo);
            tx.IncrementId = nonce;
            tx.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            tx.Fee = TxPoolConfig.Default.FeeThreshold + 1;
            tx.MethodName = "hello world";
            tx.Type = TransactionType.ContractTransaction;

            tx.Params = ByteString.CopyFrom(new Parameters
            {
                Params = { new Param
                {
                    IntVal = 1
                }}
            }.ToByteArray());

            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            
            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);
            return tx;
        }
    }
}