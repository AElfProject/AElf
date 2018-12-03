using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.ECDSA;
using AElf.Miner.Tests;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Runtime.CSharp;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
using Moq;
using AElf.Common;
using Address = AElf.Common.Address;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Miner.TxMemPool;
using AElf.Synchronization.BlockExecution;
using NServiceKit.Text;
using Uri = AElf.Configuration.Config.GRPC.Uri;

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

        public List<Transaction> CreateTx(Hash chainId)
        {
            var contractAddressZero = ContractHelpers.GetSystemContractAddress(chainId, GlobalConfig.GenesisBasicContract);
            Console.WriteLine($"zero {contractAddressZero}");
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            
            var txPrint = new Transaction()
            {
                From = AddressHelpers.BuildAddress(keyPair.PublicKey),
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
            txPrint.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));
            
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
            var contractAddressZero = ContractHelpers.GetSystemContractAddress(chainId, GlobalConfig.GenesisBasicContract);

            var code = ExampleContractCode;
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            
            var txnDep = new Transaction()
            {
                From = AddressHelpers.BuildAddress(keyPair.PublicKey),
                To = contractAddressZero,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack((int)0, code)),
                Fee = TxPoolConfig.Default.FeeThreshold + 1,
                Type = TransactionType.ContractTransaction
            };
            
            Hash hash = txnDep.GetHash();

            ECSignature signature1 = signer.Sign(keyPair, hash.DumpByteArray());
            txnDep.Sigs.Add(ByteString.CopyFrom(signature1.SigBytes));
            
            var txInv_1 = new Transaction
            {
                From = AddressHelpers.BuildAddress(keyPair.PublicKey),
                To = contractAddressZero,
                IncrementId = 1,
                MethodName = "Print",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("AElf")),
                Fee = TxPoolConfig.Default.FeeThreshold + 1,
                Type = TransactionType.ContractTransaction
            };
            
            ECSignature signature2 = signer.Sign(keyPair, txInv_1.GetHash().DumpByteArray());
            txInv_1.Sigs.Add(ByteString.CopyFrom(signature2.SigBytes));
            
            var txInv_2 = new Transaction
            {
                From = AddressHelpers.BuildAddress(keyPair.PublicKey),
                To = contractAddressZero,
                IncrementId =txInv_1.IncrementId,
                MethodName = "Print",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("AElf")),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1,
                Type = TransactionType.ContractTransaction
            };
            
            ECSignature signature3 = signer.Sign(keyPair, txInv_2.GetHash().DumpByteArray());
            txInv_2.Sigs.Add(ByteString.CopyFrom(signature3.SigBytes));
            
            var txs = new List<Transaction>(){
                txnDep, txInv_1, txInv_2
            };

            return txs;
        }
        
        [Fact]
        public async Task Mine()
        {
            var chain = await _mock.CreateChain();
            // create miner
            var keypair = new KeyPairGenerator().Generate();
            var minerconfig = _mock.GetMinerConfig(chain.Id, 10, AddressHelpers.BuildAddress(keypair.PublicKey).DumpByteArray());
            ChainConfig.Instance.ChainId = chain.Id.DumpBase58();
            NodeConfig.Instance.NodeAccount = AddressHelpers.BuildAddress(keypair.PublicKey).GetFormatted();
            var txPool = _mock.CreateTxPool();
            txPool.Start();

            var txs = CreateTx(chain.Id);
            foreach (var tx in txs)
            {
                await txPool.AddTransactionAsync(tx);
            }
            
            var manager = _mock.MinerClientManager();
            var miner = _mock.GetMiner(minerconfig, txPool, manager);

            GrpcLocalConfig.Instance.ClientToSideChain = false;
            GrpcLocalConfig.Instance.WaitingIntervalInMillisecond = 10;
            
            NodeConfig.Instance.ECKeyPair = keypair;
            miner.Init();
            
            var block = await miner.Mine();
            
            Assert.NotNull(block);
            Assert.Equal(GlobalConfig.GenesisBlockHeight + 1, block.Header.Index);
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
            Address addr = AddressHelpers.BuildAddress(keypair.PublicKey);
            // Assert.Equal(minerconfig.CoinBase, addr); 
            
            ECVerifier verifier = new ECVerifier();
            Assert.True(verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().DumpByteArray()));
        }

        [Fact]
        public async Task SyncGenesisBlock_False_Rollback()
        {
            var chain = await _mock.CreateChain();
            ChainConfig.Instance.ChainId = chain.Id.DumpBase58();
            NodeConfig.Instance.NodeAccount = Address.Generate().GetFormatted();
            
            var block = GenerateBlock(chain.Id, chain.GenesisBlockHash, GlobalConfig.GenesisBlockHeight + 1);
            
            var txs = CreateTxs(chain.Id);
            
            block.Body.Transactions.Add(txs[0].GetHash());
            block.Body.Transactions.Add(txs[2].GetHash());

            block.Body.TransactionList.Add(txs[0]);
            block.Body.TransactionList.Add(txs[2]);
            block.FillTxsMerkleTreeRootInHeader();
            block.Body.BlockHeader = block.Header.GetHash();
            block.Sign(new KeyPairGenerator().Generate());

            var manager = _mock.MinerClientManager();
            var blockExecutor = _mock.GetBlockExecutor(manager);

            var res = await blockExecutor.ExecuteBlock(block);
            Assert.NotEqual(BlockExecutionResult.Success, res);

            var blockchain = _mock.GetBlockChain(chain.Id); 
            var curHash = await blockchain.GetCurrentBlockHashAsync();
            var index = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Index;
            Assert.Equal(GlobalConfig.GenesisBlockHeight, index);
            Assert.Equal(chain.GenesisBlockHash.DumpHex(), curHash.DumpHex());
        }

        #region GRPC

        [Fact]
        public async Task SideChainServerClientsTest()
        {
            string dir = @"/tmp/ServerClientsTest";
            _mock.ClearDirectory(dir);
            try
            {
                GlobalConfig.InvertibleChainHeight = 0;
                var port = 50052;
                var address = "127.0.0.1";
                var sideChainId = _mock.MockSideChainServer(port, address, dir);
                var parimpl = _mock.MockParentChainBlockInfoRpcServerImpl();
                parimpl.Init(Hash.Generate());
                var sideimpl = _mock.MockSideChainBlockInfoRpcServerImpl();
                sideimpl.Init(sideChainId);
                var serverManager = _mock.ServerManager(parimpl, sideimpl);
                serverManager.Init(dir);
                // create client, main chian is client-side
                var manager = _mock.MinerClientManager();
                int t = 1000;
                GrpcRemoteConfig.Instance.ChildChains = new Dictionary<string, Uri>
                {
                    {
                        sideChainId.DumpBase58(), new Uri{
                            Address = address,
                            Port = port
                        }
                    }
                };
                GrpcLocalConfig.Instance.ClientToSideChain = true;
                manager.Init(dir, t);

                GrpcLocalConfig.Instance.WaitingIntervalInMillisecond = 10;
                Thread.Sleep(t/2);
                var result = await manager.CollectSideChainBlockInfo();
                int count = result.Count;
                Assert.Equal(1, count);
                Assert.Equal(GlobalConfig.GenesisBlockHeight, result[0].Height);
                
                Thread.Sleep(t);
                result = await manager.CollectSideChainBlockInfo();
                count = result.Count;
                Assert.Equal(1, count);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 1, result[0].Height);
                
                Thread.Sleep(t);
                result = await manager.CollectSideChainBlockInfo();
                count = result.Count;
                Assert.Equal(1, count);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 2, result[0].Height);
                manager.CloseClientsToSideChain();

                Thread.Sleep(t);
                result = await manager.CollectSideChainBlockInfo();
                count = result.Count;
                Assert.Equal(0, count);
            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
        }

        [Fact]
        public async Task ParentChainServerClientTest()
        {
            string dir = @"/tmp/ServerClientsTest";
            _mock.ClearDirectory(dir);
            try
            {
                GlobalConfig.InvertibleChainHeight = 0;
                var port = 50053;
                var address = "127.0.0.1";
                
                var parentChainId = _mock.MockParentChainServer(port, address, dir);
                var parimpl = _mock.MockParentChainBlockInfoRpcServerImpl();
                parimpl.Init(parentChainId);
                var sideimpl = _mock.MockSideChainBlockInfoRpcServerImpl();
                sideimpl.Init(Hash.Generate());
                var serverManager = _mock.ServerManager(parimpl, sideimpl);
                serverManager.Init(dir);
                // create client, main chain is client-side
                var manager = _mock.MinerClientManager();
                int t = 1000;
                // for client
                
                GrpcRemoteConfig.Instance.ParentChain = new Dictionary<string, Uri>
                {
                    {
                        parentChainId.DumpBase58(), new Uri{
                            Address = address,
                            Port = port
                        }
                    }
                };
                GrpcLocalConfig.Instance.ClientToParentChain = true;
                manager.Init(dir, t);

                GrpcLocalConfig.Instance.WaitingIntervalInMillisecond = 10;
                
                Thread.Sleep(t/2);
                var result = manager.TryGetParentChainBlockInfo();
                Assert.NotNull(result);
                Assert.Equal(GlobalConfig.GenesisBlockHeight, result.Height);
                Assert.Equal(1, result.IndexedBlockInfo.Count);
                Assert.True(result.IndexedBlockInfo.Keys.Contains(GlobalConfig.GenesisBlockHeight));
                //Assert.True(await manager.UpdateParentChainBlockInfo(result));
                var chainManagerBasic = _mock.MockChainManager().Object;
                await chainManagerBasic.UpdateCurrentBlockHeightAsync(result.ChainId, result.Height);
                _mock.GetTimes++;
                
                Thread.Sleep(t);
                result = manager.TryGetParentChainBlockInfo();
                Assert.NotNull(result);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 1, result.Height);
                Assert.Equal(1, result.IndexedBlockInfo.Count);
                Assert.True(result.IndexedBlockInfo.Keys.Contains(GlobalConfig.GenesisBlockHeight + 1));
                //Assert.True(await manager.UpdateParentChainBlockInfo(result));
                await chainManagerBasic.UpdateCurrentBlockHeightAsync(result.ChainId, result.Height);
                _mock.GetTimes++;

                Thread.Sleep(t);
                result =  manager.TryGetParentChainBlockInfo();
                Assert.NotNull(result);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 2, result.Height);
                Assert.Equal(1, result.IndexedBlockInfo.Count);
                Assert.True(result.IndexedBlockInfo.Keys.Contains(GlobalConfig.GenesisBlockHeight + 2));
                manager.CloseClientToParentChain();
            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
        }
        
        [Fact]
        public async Task MineWithIndexingSideChain()
        {
            GlobalConfig.InvertibleChainHeight = 0;
            string dir = @"/tmp/minerpems";
            var chain = await _mock.CreateChain();
            var keyPair = new KeyPairGenerator().Generate();
            
            var minerConfig = _mock.GetMinerConfig(chain.Id, 10, null);
            
            ChainConfig.Instance.ChainId = chain.Id.DumpBase58();
            
            NodeConfig.Instance.ECKeyPair = keyPair;
            NodeConfig.Instance.NodeAccount = AddressHelpers.BuildAddress(chain.Id.DumpByteArray(), keyPair.PublicKey).GetFormatted();
            
            var pool = _mock.CreateTxPool();
            pool.Start();

            try
            {
                int sidePort = 50054;
                int parentPort = 50055;
                string address = "127.0.0.1";
                _mock.ClearDirectory(dir);
                GrpcRemoteConfig.Instance.ParentChain = null;
                var sideChainId = _mock.MockSideChainServer(sidePort, address, dir);
                //var parentChainId = _mock.MockParentChainServer(parentPort, address, dir);
                var parimpl = _mock.MockParentChainBlockInfoRpcServerImpl();
                //parimpl.Init(parentChainId);
                var sideimpl = _mock.MockSideChainBlockInfoRpcServerImpl();
                sideimpl.Init(sideChainId);
                var serverManager = _mock.ServerManager(parimpl, sideimpl);
                serverManager.Init(dir);
                
                var manager = _mock.MinerClientManager();
                int t = 1000;
                GrpcRemoteConfig.Instance.ChildChains = new Dictionary<string, Uri>
                {
                    {
                        sideChainId.DumpBase58(), new Uri{
                            Address = address,
                            Port = sidePort
                        }
                    }
                };
                
                GrpcLocalConfig.Instance.ClientToSideChain = true;
                GrpcLocalConfig.Instance.ClientToParentChain = false;
                manager.Init(dir, t);
                var miner = _mock.GetMiner(minerConfig, pool, manager);
                miner.Init();
                //Thread.Sleep(t/2);
                ChainConfig.Instance.ChainId = chain.Id.DumpBase58();
                var block = await miner.Mine();
                Assert.NotNull(block);
                Assert.NotNull(block.Body.IndexedInfo);
                int count = block.Body.IndexedInfo.Count;
                Assert.Equal(1, count);
                Assert.Equal(GlobalConfig.GenesisBlockHeight, block.Body.IndexedInfo[0].Height);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 1, block.Header.Index);
            
                Thread.Sleep(t);
                block = await miner.Mine();
                Assert.NotNull(block);
                Assert.NotNull(block.Body.IndexedInfo);
                count = block.Body.IndexedInfo.Count;
                Assert.Equal(1, count);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 1, block.Body.IndexedInfo[0].Height);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 2, block.Header.Index);
            
                Thread.Sleep(t);
                block = await miner.Mine();
                Assert.NotNull(block);
                Assert.NotNull(block.Body.IndexedInfo);
                count = block.Body.IndexedInfo.Count;
                Assert.Equal(1, count);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 2, block.Body.IndexedInfo[0].Height);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 3, block.Header.Index);
                
                manager.CloseClientsToSideChain();
            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
            
        }
        

        #endregion
    }
}