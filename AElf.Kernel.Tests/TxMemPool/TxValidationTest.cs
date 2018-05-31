using System;
using AElf.Kernel.Crypto.ECDSA;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using Google.Protobuf;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
{
    [UseAutofacTestFramework]
    public class TxValidationTest
    {
        private readonly IAccountContextService _accountContextService;
        private readonly ILogger _logger;

        
        public TxValidationTest(IAccountContextService accountContextService, ILogger logger)
        {
            _accountContextService = accountContextService;
            _logger = logger;
        }

        private TxPool GetPool(ulong feeThreshold = 0, uint txSize = 0)
        {
            return new TxPool(new TxPoolConfig
            {
                TxLimitSize = txSize,
                FeeThreshold = feeThreshold
            }, _logger);
        }

        

        private Transaction CreateAndSignTransaction(Hash from = null, Hash to = null, ulong id = 0, ulong fee = 0 )
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            var ps = new Parameters();
            var tx = new Transaction
            {
                From = from,
                To = to,
                IncrementId = id,
                MethodName = "null",
                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded()),
                Fee = fee,
                Params = ByteString.CopyFrom(new Parameters
                {
                    Params =
                    {
                        new Param
                        {
                            IntVal = 1

                        }
                    }
                }.ToByteArray())
            };
            
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
        public Transaction ValidTx()
        {
            var pool = GetPool(1, 1024);
            var tx = CreateAndSignTransaction(Hash.Generate(), Hash.Generate(), 0, 2);
            Assert.Equal(pool.ValidateTx(tx), TxValidation.ValidationError.Success);
            return tx;
        }
        
        

        [Fact]
        public void InvalidTxWithoutMethodName()
        {
            var pool = GetPool(1, 1024);
            var tx = ValidTx();
            Assert.Equal(pool.ValidateTx(tx), TxValidation.ValidationError.Success);

            tx.MethodName = "";
            Assert.Equal(pool.ValidateTx(tx), TxValidation.ValidationError.InvalidTxFormat);
        }

        [Fact]
        public void InvalidSignature()
        {
            var tx = CreateAndSignTransaction(Hash.Generate(), Hash.Generate(), 0, 2);
            Assert.True(tx.VerifySignature());
            //System.Diagnostics.Debug.WriteLine(tx.To.Value.ToBase64());
            tx.To = Hash.Generate();
            //System.Diagnostics.Debug.WriteLine(tx.To.Value.ToBase64());
            Assert.False(tx.VerifySignature());
            
        }

        [Fact]
        public void InvalidAccountAddress()
        {
            var tx = ValidTx();
            Assert.True(tx.CheckAccountAddress());
            tx.From = new Hash(new byte[31]);
            Assert.False(tx.CheckAccountAddress());
        }
        
        [Fact(Skip = "TODO")]
        public void InvalidTxWithFeeNotEnough()
        {
            var pool = GetPool(2, 1024);
            var tx = CreateAndSignTransaction(Hash.Generate(), Hash.Generate(),0, 3);
            Assert.Equal(pool.ValidateTx(tx), TxValidation.ValidationError.Success);

            tx.Fee = 1;
            Assert.Equal(pool.ValidateTx(tx), TxValidation.ValidationError.NotEnoughGas);
        }
        

        [Fact]
        public void InvalidTxWithWrongSize()
        {  
            var pool = GetPool(2, 3);
            var tx = CreateAndSignTransaction(Hash.Generate(), Hash.Generate(),0, 1);
            tx.Params = ByteString.CopyFrom(new Parameters
            {
                Params =
                {
                    new Param
                    {
                        IntVal = 2
                    }
                }
            }.ToByteArray());
            Assert.Equal(pool.ValidateTx(tx), TxValidation.ValidationError.TooBigSize);
        }
        
        
    }
}