using System;
using AElf.Kernel.Crypto.ECDSA;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
{
    [UseAutofacTestFramework]
    public class TxValidationTest
    {
        private readonly IAccountContextService _accountContextService;
        
        public TxValidationTest(IAccountContextService accountContextService)
        {
            _accountContextService = accountContextService;
        }

        private TxPool GetPool(ulong feeThreshold = 0, uint txSize = 0)
        {
            return new TxPool(new TxPoolConfig
            {
                TxLimitSize = txSize,
                FeeThreshold = feeThreshold
            }, _accountContextService);
        }

        private Transaction GetTransaction(Hash from = null, Hash to = null, ulong id = 0, ulong fee = 0 )
        {
            return new Transaction
            {
                From = from,
                To = to,
                IncrementId = id
            };
        }

        private void SignTx(Transaction tx)
        {
            // Generate the key pair 
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            
            tx.P =  ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            
            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            
            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);
        }
        
        [Fact]
        public Transaction ValidTx()
        {
            var pool = GetPool(1, 1024);
            var tx = GetTransaction(Hash.Generate(), Hash.Generate(), 0, 2);
            tx.MethodName = "valid";
            tx.Params = ByteString.CopyFrom(new byte[1]);
            Assert.True(pool.ValidateTx(tx));
            return tx;
        }
        
        [Fact]
        public void InvalidTxWithoutFromAccount()
        {
            var pool = GetPool();
            var tx = ValidTx();
            tx.From = null;
            SignTx(tx);
            Assert.False(pool.ValidateTx(tx));
        }

        [Fact]
        public void InvalidTxWithoutMethodName()
        {
            var pool = GetPool();
            var tx = ValidTx();
            tx.MethodName = "";
            SignTx(tx);
            Assert.False(pool.ValidateTx(tx));
        }

        [Fact]
        public void InvalidSignature()
        {
            
        }

        [Fact]
        public void InvalidAccountAddress()
        {
            var pool = GetPool();
            var tx = ValidTx();
            tx.From = new Hash(new byte[31]);
            SignTx(tx);
            Assert.False(pool.ValidateTx(tx));
        }
        
        [Fact]
        public void InvalidTxWithFeeNotEnough()
        {
            var pool = GetPool(feeThreshold: 2);
            var tx = GetTransaction(fee: 1);
            Assert.False(pool.ValidateTx(tx));
        }
        

        [Fact]
        public void InvalidTxWithWrongSize()
        {  
            var pool = GetPool(txSize: 3);
            var tx = GetTransaction();
            tx.Params = ByteString.CopyFrom(new byte[2]);
            Console.WriteLine(tx.CalculateSize());
            Assert.False(pool.ValidateTx(tx));
        }
        
        
    }
}