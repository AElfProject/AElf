using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Crypto.ECDSA;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using Google.Protobuf;
using Org.BouncyCastle.Utilities.Collections;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
{
    [UseAutofacTestFramework]
    public class TxPoolTest
    {
        private readonly IAccountContextService _accountContextService;
        
        public TxPoolTest(IAccountContextService accountContextService)
        {
            _accountContextService = accountContextService;
        }

        private TxPool GetPool()
        {
            return new TxPool(TxPoolConfig.Default);
        }

        public static Transaction BuildTransaction(Hash adrFrom = null, Hash adrTo = null, ulong nonce = 0)
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();

            var tx = new Transaction();
            tx.From = adrFrom == null ? Hash.Generate() : adrFrom;
            tx.To = adrTo == null ? Hash.Generate() : adrTo;
            tx.IncrementId = nonce;
            tx.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            tx.Fee = TxPoolConfig.Default.FeeThreshold + 1;
            tx.MethodName = "hello world";
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
            var conf = TxPoolConfig.Default;
            conf.EntryThreshold = 1;

            var pool = new TxPool(conf);
            
            // Add a valid transaction
            var tx = BuildTransaction();
            var tmp = new HashSet<ITransaction> {tx};
            var ctx = await _accountContextService.GetAccountDataContext(tx.From, conf.ChainId);
            pool.Nonces[tx.From] = ctx.IncrementId;
            pool.EnQueueTxs(tmp);
            
            pool.GetPoolState(out var executable, out var waiting);
            Assert.Equal(1, (int)waiting);
            Assert.Equal(0, (int)executable);
        }

        
        /*[Fact]
        public async Task ContainsTx_ReturnsTrue_AfterAdd()
        {
            var pool = GetPool();
            
            // Add a valid transaction
            var tx = BuildTransaction();
            var tmp = new HashSet<ITransaction> {tx};
            var ctx = await _accountContextService.GetAccountDataContext(tx.From, TxPoolConfig.Default.ChainId);
            pool.Nonces[tx.From] = ctx.IncrementId;
            pool.QueueTxs(tmp);

            var res = pool.Contains(tx.GetHash());
            
            Assert.True(res);
        }*/


        [Fact]
        public async Task PromoteTest()
        {
            var pool = GetPool();
            
            // Add a valid transaction
            var tx = BuildTransaction();
            var tmp = new HashSet<ITransaction> {tx};
            var ctx = await _accountContextService.GetAccountDataContext(tx.From, TxPoolConfig.Default.ChainId);
            pool.Nonces[tx.From] = ctx.IncrementId;
            pool.EnQueueTxs(tmp);
            
            pool.Promote();
            
            pool.GetPoolState(out var executable, out var waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(1, (int)executable);
        }

        [Fact]
        public async Task ReadyTxsTest()
        {
            var pool = GetPool();
            
            // Add a valid transaction
            var tx = BuildTransaction();
            var tmp = new HashSet<ITransaction> {tx};
            var ctx =  await _accountContextService.GetAccountDataContext(tx.From, TxPoolConfig.Default.ChainId);
            pool.Nonces[tx.From] = ctx.IncrementId;
            pool.EnQueueTxs(tmp);
            
            pool.Promote();
            
            var ready = pool.ReadyTxs(10);
            
            Assert.Equal(1, ready.Count);
            Assert.True(ready.Contains(tx));
            Assert.Equal(pool.Nonces[tx.From], ctx.IncrementId + 1);
        }


        /*[Fact]
        public async Task GetTxTest()
        {
            var pool = GetPool();
            var tx = BuildTransaction();
            var tmp = new HashSet<ITransaction> {tx};
            var ctx = await _accountContextService.GetAccountDataContext(tx.From, TxPoolConfig.Default.ChainId);
            pool.Nonces[tx.From] = ctx.IncrementId;
            pool.QueueTxs(tmp);
            
            var t = pool.GetTx(tx.GetHash());
            
            Assert.Equal(tx, t);
        }*/
    }
}