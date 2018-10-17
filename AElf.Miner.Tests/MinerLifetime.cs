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
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
using Moq;
using NLog;
using MinerConfig = AElf.Miner.Miner.MinerConfig;
using AElf.Common;
using Address = AElf.Common.Address;
using AElf.Common;

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

        private MockSetup _mock;

        public MinerLifetime(MockSetup mock)
        {
            _mock = mock;
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
            var contractAddressZero = AddressHelpers.GetSystemContractAddress(chainId, GlobalConfig.GenesisBasicContract);

            var code = ExampleContractCode;

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = Hash.FromRawBytes(code)
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

            ECSignature signature = signer.Sign(keyPair, hash.DumpByteArray());
            txnDep.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txnDep.R = ByteString.CopyFrom(signature.R); 
            txnDep.S = ByteString.CopyFrom(signature.S);
            
            var txs = new List<Transaction>(){
                txnDep
            };
            
            var mock = new Mock<ITxPoolService>();
            mock.Setup((s) => s.GetReadyTxsAsync(null, Address.FromRawBytes(Hash.Generate().ToByteArray()), 3000)).Returns(Task.FromResult(txs));
            return mock;
        }
        
        
        public List<Transaction> CreateTx(Hash chainId)
        {
            var contractAddressZero = AddressHelpers.GetSystemContractAddress(chainId, GlobalConfig.GenesisBasicContract);
            Console.WriteLine($"zero {contractAddressZero}");
            var code = ExampleContractCode;
         
            
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

            ECSignature signature = signer.Sign(keyPair, hash.DumpByteArray());
            txPrint.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txPrint.R = ByteString.CopyFrom(signature.R); 
            txPrint.S = ByteString.CopyFrom(signature.S);
            
            var txs = new List<Transaction>(){
                txPrint
            };

            return txs;
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
        
        public List<Transaction> CreateTxs(Hash chainId)
        {
            var contractAddressZero = AddressHelpers.GetSystemContractAddress(chainId, GlobalConfig.GenesisBasicContract);

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

            ECSignature signature1 = signer.Sign(keyPair, hash.DumpByteArray());
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
            ECSignature signature2 = signer.Sign(keyPair, txInv_1.GetHash().DumpByteArray());
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
            
            ECSignature signature3 = signer.Sign(keyPair, txInv_2.GetHash().DumpByteArray());
            txInv_2.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txInv_2.R = ByteString.CopyFrom(signature3.R); 
            txInv_2.S = ByteString.CopyFrom(signature3.S);
            
            var txs = new List<Transaction>(){
                txnDep, txInv_1, txInv_2
            };

            return txs;
        }
        [Fact]
        public async Task Mine()
        {
            var chain = await _mock.CreateChain();
            var poolService = _mock.CreateTxPoolService(chain.Id);
            poolService.Start();

            var txs = CreateTx(chain.Id);
            foreach (var tx in txs)
            {
                await poolService.AddTxAsync(tx);
            }
            
            // create miner
            var keypair = new KeyPairGenerator().Generate();
            var minerconfig = _mock.GetMinerConfig(chain.Id, 10, keypair.GetAddress().DumpByteArray());
            var manager = _mock.MinerClientManager();
            var miner = _mock.GetMiner(minerconfig, poolService, manager);

            //GrpcLocalConfig.Instance.ClientToParentChain = false;
            GrpcLocalConfig.Instance.ClientToSideChain = false;
            GrpcLocalConfig.Instance.WaitingIntervalInMillisecond = 10;
            //GrpcRemoteConfig.Instance.ParentChain = null;
            miner.Init(keypair);
            
            var block = await miner.Mine();
            
            Assert.NotNull(block);
            Assert.Equal((ulong)1, block.Header.Index);
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
//            Hash addr = uncompressedPrivKey.Take(Common.Globals.AddressLength).ToArray();
            Address addr = Address.FromRawBytes(uncompressedPrivKey);
            Assert.Equal(minerconfig.CoinBase, addr);
            
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            Assert.True(verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().DumpByteArray()));
        }
        
        [Fact]
        public async Task ExecuteWithoutTransaction()
        {
            var chain = await _mock.CreateChain();
            var poolService = _mock.CreateTxPoolService(chain.Id);
            poolService.Start();

            // create miner
            var keypair = new KeyPairGenerator().Generate();
            var minerconfig = _mock.GetMinerConfig(chain.Id, 10, keypair.GetAddress().DumpByteArray());
            var manager = _mock.MinerClientManager();
            var miner = _mock.GetMiner(minerconfig, poolService, manager);
            //GrpcLocalConfig.Instance.ClientToParentChain = false;
            GrpcLocalConfig.Instance.ClientToSideChain = false;
            //GrpcRemoteConfig.Instance.ParentChain = null;
            miner.Init(keypair);

            var block = await miner.Mine();
            
            Assert.NotNull(block);
            Assert.Equal((ulong)1, block.Header.Index);
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
//            Hash addr = uncompressedPrivKey.Take(ECKeyPair.AddressLength).ToArray();
            Address addr = Address.FromRawBytes(uncompressedPrivKey);
            Assert.Equal(minerconfig.CoinBase, addr);
            
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            Assert.True(verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().DumpByteArray()));
        }

        [Fact]
        public async Task SyncGenesisBlock_False_Rollback()
        {
            var poolconfig = TxPoolConfig.Default;
            var chain = await _mock.CreateChain();
            poolconfig.ChainId = chain.Id;
            
            var poolService = _mock.CreateTxPoolService(chain.Id);
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
            block.Sign(new KeyPairGenerator().Generate());

            var manager = _mock.MinerClientManager();
            var synchronizer = _mock.GetBlockExecutor(poolService, manager);

            synchronizer.Init();
            var res = await synchronizer.ExecuteBlock(block);
            Assert.False(res);

            Assert.Equal((ulong)0, await poolService.GetWaitingSizeAsync());
            Assert.Equal((ulong)2, await poolService.GetExecutableSizeAsync());
            //Assert.False(poolService.TryGetTx(txs[2].GetHash(), out tx));
            Assert.True(poolService.TryGetTx(txs[1].GetHash(), out tx));

            var blockchain = _mock.GetBlockChain(chain.Id); 
            var curHash = await blockchain.GetCurrentBlockHashAsync();
            var index = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Index;
            Assert.Equal((ulong)0, index);
            Assert.Equal(chain.GenesisBlockHash.DumpHex(), curHash.DumpHex());
        }
    }
}