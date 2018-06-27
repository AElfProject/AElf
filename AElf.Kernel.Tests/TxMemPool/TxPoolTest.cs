using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using AElf.Node.RPC.DTO;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using NLog;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Collections;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
{
    [UseAutofacTestFramework]
    public class TxPoolTest
    {
        private readonly IAccountContextService _accountContextService;
        private readonly ILogger _logger;
        private readonly IWorldStateDictator _worldStateDictator;


        public TxPoolTest(IAccountContextService accountContextService, ILogger logger, IWorldStateDictator worldStateDictator)
        {
            _accountContextService = accountContextService;
            _logger = logger;
            _worldStateDictator = worldStateDictator;
        }

        private TxPool GetPool()
        {
            var config = TxPoolConfig.Default;
            _worldStateDictator.SetChainId(config.ChainId);
            return new TxPool(config, _logger);
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
        
        //[Fact]
        public void Serialize()
        {
            System.Diagnostics.Debug.WriteLine(Hash.Generate().ToByteString().ToBase64());
            var tx = Transaction.Parser.ParseFrom(ByteString.FromBase64(
                @"CiIKIKkqNVMSxCWn/TizqYJl0ymJrnrRqZN+W3incFJX3MRIEiIKIIFxBhlGhI1auR05KafXd/lFGU+apqX96q1YK6aiZLMhIgh0cmFuc2ZlcioJCgcSBWhlbGxvOiEAxfMt77nwSKl/WUg1TmJHfxYVQsygPj0wpZ/Pbv+ZK4pCICzGxsZBCBlASmlDdn0YIv6vRUodJl/9jWd8Q1z2ofFwSkEE+PDQtkHQxvw0txt8bmixMA8lL0VM5ScOYiEI82LX1A6oWUNiLIjwAI0Qh5fgO5g5PerkNebXLPDE2dTzVVyYYw=="));
            var pool = GetPool();
            Assert.Equal(TxValidation.ValidationError.Success, pool.ValidateTx(tx));
        }

        
        
        
        [Fact]
        public async Task EntryThreshold_Test()
        {
            // setup config
            var pool = GetPool();
            
            // Add a valid transaction
            var tx = BuildTransaction();
            var tmp = new HashSet<ITransaction> {tx};
            var ctx = await _accountContextService.GetAccountDataContext(tx.From, pool.ChainId);
            pool.Nonces[tx.From] = ctx.IncrementId;
            pool.EnQueueTxs(tmp);
            
            pool.GetPoolState(out var executable, out var waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(1, (int)executable);
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


        /*[Fact]
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
        }*/

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
            
            //pool.Promote();
            
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