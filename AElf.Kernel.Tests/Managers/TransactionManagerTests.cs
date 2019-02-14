using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Xunit;
using AElf.Common;
using AElf.Cryptography;
using AElf.TxPool;

namespace AElf.Kernel.Tests.Managers
{
    public sealed class TransactionManagerTests:AElfKernelTestBase
    {
        private ITransactionManager _transactionManager;

        public TransactionManagerTests()
        {
            _transactionManager = GetRequiredService<ITransactionManager>();
        }

        [Fact]
        public async Task TestInsert()
        {
            await _transactionManager.AddTransactionAsync(new Transaction
            {
                From = Address.Generate(),
                To = Address.Generate()
            });
        }

        [Fact]
        public async Task GetTest()
        {
            var t = BuildTransaction();
            var key = await _transactionManager.AddTransactionAsync(t);
            var td = await _transactionManager.GetTransaction(key);
            Assert.Equal(t, td);
        }

        [Fact]
        public async Task RemoveTest()
        {
            var t1 = BuildTransaction();
            var t2 = BuildTransaction();

            var key1 = await _transactionManager.AddTransactionAsync(t1);
            var key2 = await _transactionManager.AddTransactionAsync(t2);

            var td1 = await _transactionManager.GetTransaction(key1);
            Assert.Equal(t1, td1);

            await _transactionManager.RemoveTransaction(key2);
            var td2 = await _transactionManager.GetTransaction(key2);
            Assert.Equal(td2, null);
        }

        [Fact]
        public async Task TestInsertMultipleTx()
        {
            var address = Address.Generate();
            var t1 = BuildTransaction(address, 1);
            var t2 = BuildTransaction(address, 2);
            var key1 = await _transactionManager.AddTransactionAsync(t1);
            var key2 = await _transactionManager.AddTransactionAsync(t2);
            Assert.NotEqual(key1, key2);
        }
        
        public static Transaction BuildTransaction(Address adrTo = null, ulong nonce = 0, ECKeyPair keyPair = null)
        {
            keyPair = keyPair ?? CryptoHelpers.GenerateKeyPair();

            var tx = new Transaction();
            tx.From = Address.Generate();
            tx.To = adrTo ?? Address.Generate();
            tx.IncrementId = nonce;
            
            //todo review probably useless - or a proper sig is needed
            //            var sig = new Sig
            //            {
            //                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded())
            //            };
            //            tx.Sigs.Add(sig);
            
            tx.Fee = TxPoolConfig.Default.FeeThreshold + 1;
            tx.MethodName = "hello world";

            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            var signature = CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, hash.DumpByteArray());
            
            // Update the signature
            //todo review probably useless - or a proper sig is needed
            //tx.Sig = ByteString.CopyFrom(signature.SigBytes);
            
            return tx;
        }
    }
}