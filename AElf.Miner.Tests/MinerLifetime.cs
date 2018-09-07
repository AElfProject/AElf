using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.ECDSA;
using AElf.SmartContract;
using AElf.Kernel.Managers;
using AElf.Miner.Miner;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Tests;
using AElf.Miner.Tests.Grpc;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Runtime.CSharp;
using Moq;
using NLog;
using MinerConfig = AElf.Miner.Miner.MinerConfig;

namespace AElf.Kernel.Tests.Miner
{
    [UseAutofacTestFramework]
    public class MinerLifetime
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;

        public ulong NewIncrementId()
        {
            var res = _incrementId;
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) res;
        }

        public readonly ILogger _logger;
        private MockSetup _mock;

        public MinerLifetime(MockSetup mock, ILogger logger)
        {
            _mock = mock;
            _logger = logger;
        }


        public byte[] SmartContractZeroCode
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

        public Mock<ITxPoolService> MockTxPoolService(Hash chainId)
        {
            var contractAddressZero = new Hash(chainId.CalculateHashWith(Globals.GenesisBasicContract)).ToAccount();

            var code = ExampleContractCode;

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
            };
            
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            var txnDep = new Transaction()
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params = {
                        new Param
                        {
                            RegisterVal = regExample
                        }
                    }
                }.ToByteArray()),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            Hash hash = txnDep.GetHash();

            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            txnDep.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txnDep.R = ByteString.CopyFrom(signature.R); 
            txnDep.S = ByteString.CopyFrom(signature.S);
            
            var txs = new List<Transaction>(){
                txnDep
            };
            
            var mock = new Mock<ITxPoolService>();
            mock.Setup((s) => s.GetReadyTxsAsync(3000)).Returns(Task.FromResult(txs));
            return mock;
        }
        
        
        public List<Transaction> CreateTxs(Hash chainId)
        {
            var contractAddressZero = new Hash(chainId.CalculateHashWith(Globals.GenesisBasicContract)).ToAccount();

            var code = ExampleContractCode;

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
            };
            
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            
            var txPrint = new Transaction()
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "Print",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params = {
                        new Param
                        {
                            StrVal = "AElf"
                        }
                    }
                }.ToByteArray()),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            Hash hash = txPrint.GetHash();

            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            txPrint.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txPrint.R = ByteString.CopyFrom(signature.R); 
            txPrint.S = ByteString.CopyFrom(signature.S);
            
            var txs = new List<Transaction>(){
                txPrint
            };

            return txs;
        }
        
        
        [Fact]
        public async Task Mine()
        {
            var chain = await _mock.CreateChain();
            var poolService = _mock.CreateTxPoolService(chain.Id);
            poolService.Start();

            var txs = CreateTxs(chain.Id);
            foreach (var tx in txs)
            {
                await poolService.AddTxAsync(tx);
            }
            
            // create miner
            var keypair = new KeyPairGenerator().Generate();
            var minerconfig = _mock.GetMinerConfig(chain.Id, 10, keypair.GetAddress());
            var miner = _mock.GetMiner(minerconfig, poolService);

            GrpcLocalConfig.Instance.Client = false;
            GrpcLocalConfig.Instance.WaitingIntervalInMillisecond = 10;
            miner.Init(keypair);
            
            var block = await miner.Mine(Timeout.Infinite, false);
            
            Assert.NotNull(block);
            Assert.Equal((ulong)1, block.Header.Index);
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
            Hash addr = uncompressedPrivKey.Take(ECKeyPair.AddressLength).ToArray();
            Assert.Equal(minerconfig.CoinBase, addr);
            
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            Assert.True(verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().GetHashBytes()));
        }
        
        [Fact]
        public async Task ExecuteWithoutTransaction()
        {
            var chain = await _mock.CreateChain();
            var poolService = _mock.CreateTxPoolService(chain.Id);
            poolService.Start();

            // create miner
            var keypair = new KeyPairGenerator().Generate();
            var minerconfig = _mock.GetMinerConfig(chain.Id, 10, keypair.GetAddress());
            var miner = _mock.GetMiner(minerconfig, poolService);
            GrpcLocalConfig.Instance.Client = false;
            miner.Init(keypair);

            var block = await miner.Mine(Timeout.Infinite, false);
            
            Assert.NotNull(block);
            Assert.Equal((ulong)1, block.Header.Index);
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
            Hash addr = uncompressedPrivKey.Take(ECKeyPair.AddressLength).ToArray();
            Assert.Equal(minerconfig.CoinBase, addr);
            
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            Assert.True(verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().GetHashBytes()));
        }

        
    }
}