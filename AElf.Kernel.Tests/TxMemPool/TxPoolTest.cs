using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.SmartContract;
using Google.Protobuf;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
{
    [UseAutofacTestFramework]
    public class TxPoolTest
    {
        private readonly ILogger _logger;
        private readonly IStateDictator _stateDictator;

        public TxPoolTest(ILogger logger, IStateDictator stateDictator)
        {
            _logger = logger;
            _stateDictator = stateDictator;
        }

        private ContractTxPool GetPool(TxPoolConfig config, ECKeyPair ecKeyPair = null)
        {
            if (ecKeyPair != null)
                config.EcKeyPair = ecKeyPair;
            _stateDictator.ChainId = config.ChainId;
            return new ContractTxPool(config, _logger);
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
        
        
        
        [Fact]
        public async Task EntryThreshold_Test()
        {
            // setup config
            var config = TxPoolConfig.Default;
            var pool = GetPool(config);
            
            // Add a valid transaction
            var tx = BuildTransaction();
            var tmp = new HashSet<Transaction> {tx};
            var accountContextService = new AccountContextService(_stateDictator);
            var ctx = await accountContextService.GetAccountDataContext(tx.From, pool.ChainId);
            pool.TrySetNonce(tx.From,ctx.IncrementId);
            pool.EnQueueTxs(tmp);
            
            pool.GetPoolState(out var executable, out var waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(1, (int)executable);
        }

        [Fact]
        public void DemoteTxs()
        {
            var config = TxPoolConfig.Default;
            var pool = GetPool(config);
            var kp1 = new KeyPairGenerator().Generate();
            var tx1_0 = BuildTransaction(nonce: 2, keyPair:kp1);
            var tx1_1 = BuildTransaction(nonce: 1, keyPair:kp1);
            var tx1_2 = BuildTransaction(nonce: 3, keyPair: kp1);

            pool.TrySetNonce(kp1.GetAddress(), 1);
            pool.EnQueueTx(tx1_0);
            pool.EnQueueTx(tx1_1);
            pool.EnQueueTx(tx1_2);
            
            Assert.Equal((ulong)0, pool.GetWaitingSize());
            Assert.Equal((ulong)3, pool.GetExecutableSize());
            
            pool.Withdraw(kp1.GetAddress(), 0);
            Assert.Equal((ulong)0, pool.GetNonce(kp1.GetAddress()));
            Assert.Equal((ulong)0, pool.GetExecutableSize());
            Assert.Equal((ulong)3, pool.GetWaitingSize());

            var tx1_4 = BuildTransaction(nonce: 0, keyPair: kp1);
            pool.EnQueueTx(tx1_4);
            Assert.Equal((ulong)4, pool.GetExecutableSize());
            Assert.Equal((ulong)0, pool.GetWaitingSize());
        }


        [Fact(Skip = "Failed in CI due to concurrent running")]
        public async Task ReadyTxsTest()
        {
            ECKeyPair ecKeyPair = new KeyPairGenerator().Generate();
            var config = TxPoolConfig.Default;
            config.Maximal = 10;
            var pool = GetPool(config, ecKeyPair);
            var tmp = new HashSet<Transaction>();
            var accountContextService = new AccountContextService(_stateDictator);

            // Add valid transactions
            int i = 0;
            while (i++ < 15)
            {
                var tx = BuildTransaction();
                var ctx =  await accountContextService.GetAccountDataContext(tx.From, pool.ChainId);
                pool.TrySetNonce(tx.From, ctx.IncrementId);
                tmp.Add(tx);
            }
            pool.EnQueueTxs(tmp);

            // add miner txs
            int minerTxCount = 5;
            i = 0;
            var minerTxs = new HashSet<Transaction>();

            while (i++ < minerTxCount)
            {
                var minerTx = BuildTransaction(nonce:(ulong)(i-1), keyPair:ecKeyPair);
                minerTxs.Add(minerTx);
            }
            var ctx1 =  await accountContextService.GetAccountDataContext(ecKeyPair.GetAddress(), pool.ChainId);
            pool.TrySetNonce(ecKeyPair.GetAddress(), ctx1.IncrementId);
            pool.EnQueueTxs(minerTxs);

            var ready = pool.ReadyTxs();
            
            Assert.Equal(10, ready.Count);
            foreach (var mtx in minerTxs)
            {
                Assert.True(ready.Contains(mtx));
            }

            Assert.Equal(minerTxCount, (int) pool.GetNonce(ecKeyPair.GetAddress()).Value);
        }
    }
}