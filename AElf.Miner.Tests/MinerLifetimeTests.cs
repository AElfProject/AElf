using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.ECDSA;
using Google.Protobuf;
using Xunit;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using Address = AElf.Common.Address;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.Extensions;
using AElf.Kernel.TxMemPool;
using AElf.Kernel.Types;
using AElf.Synchronization.BlockExecution;
using AElf.TxPool;
using Easy.MessageHub;
using Microsoft.Extensions.Options;
using Uri = AElf.Configuration.Config.GRPC.Uri;

namespace AElf.Miner.Tests
{
public sealed class MinerLifetimeTests : MinerTestBase
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
        private IAccountService _accountService;

        public MinerLifetimeTests()
        {
            _mock = GetRequiredService<MockSetup>();
            _accountService = GetRequiredService<IAccountService>();
        }

        public byte[] ExampleContractCode
        {
            get
            {
                return ContractCodes.TestContractCode;
            }
        }

        public List<Transaction> CreateTx(int chainId)
        {
            var contractAddressZero = ContractHelpers.GetSystemContractAddress(chainId, GlobalConfig.GenesisBasicContract);
            Console.WriteLine($"zero {contractAddressZero}");
            
            ECKeyPair keyPair = CryptoHelpers.GenerateKeyPair();
            
            var txPrint = new Transaction()
            {
                From = AddressHelpers.BuildAddress(keyPair.PublicKey),
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "Print",                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            Hash hash = txPrint.GetHash();

            var signature = CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, hash.DumpByteArray());
            txPrint.Sigs.Add(ByteString.CopyFrom(signature));
            
            var txs = new List<Transaction>(){
                txPrint
            };

            return txs;
        }
        
        public Block GenerateBlock(int chainId, Hash previousHash, ulong height)
        {
            var block = new Block(previousHash)
            {
                Header = new BlockHeader
                {
                    ChainId = chainId,
                    Height = height,
                    PreviousBlockHash = previousHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    MerkleTreeRootOfWorldState = Hash.Default
                }
            };
            block.FillTxsMerkleTreeRootInHeader();
            return block;
        }
        
        public List<Transaction> CreateTxs(int chainId)
        {
            var contractAddressZero = ContractHelpers.GetSystemContractAddress(chainId, GlobalConfig.GenesisBasicContract);

            var code = ExampleContractCode;
            
            ECKeyPair keyPair = CryptoHelpers.GenerateKeyPair();
            
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

            var signature1 = CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, hash.DumpByteArray());
            txnDep.Sigs.Add(ByteString.CopyFrom(signature1));
            
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
            
            var signature2 = CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, txInv_1.GetHash().DumpByteArray());
            txInv_1.Sigs.Add(ByteString.CopyFrom(signature2));
            
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
            
            var signature3 = CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, txInv_2.GetHash().DumpByteArray());
            txInv_2.Sigs.Add(ByteString.CopyFrom(signature3));
            
            var txs = new List<Transaction>(){
                txnDep, txInv_1, txInv_2
            };

            return txs;
        }
        
        [Fact(Skip = "Miner refactor needed.")]
        public async Task Mine_ProduceSecondBlock_WithCorrectSig()
        {
            // create the miners keypair, this is the miners identity
            var minerKeypair = CryptoHelpers.GenerateKeyPair();
            var minerAddress = AddressHelpers.BuildAddress(minerKeypair.PublicKey);
            
            var chain = await _mock.CreateChain();
            
            var txHub = _mock.CreateAndInitTxHub();
            txHub.Start();

            var txs = CreateTx(chain.Id);
            foreach (var tx in txs)
            {
                await txHub.AddTransactionAsync(chain.Id, tx);
            }
            
            var manager = _mock.MinerClientManager();
            var miner = _mock.GetMiner(txHub, manager);

            GrpcLocalConfig.Instance.ClientToSideChain = false;
            GrpcLocalConfig.Instance.WaitingIntervalInMillisecond = 10;
            var block = await miner.Mine(chain.Id);
            
            Assert.NotNull(block);
            Assert.Equal(GlobalConfig.GenesisBlockHeight + 1, block.Header.Height);
        }

        [Fact(Skip = "ChainId changed")]
        public async Task SyncGenesisBlock_False_Rollback()
        {
            var chain = await _mock.CreateChain();
            
            var block = GenerateBlock(chain.Id, chain.GenesisBlockHash, GlobalConfig.GenesisBlockHeight + 1);
            
            var txs = CreateTxs(chain.Id);
            
            block.Body.Transactions.Add(txs[0].GetHash());
            block.Body.Transactions.Add(txs[2].GetHash());

            block.Body.TransactionList.Add(txs[0]);
            block.Body.TransactionList.Add(txs[2]);
            block.FillTxsMerkleTreeRootInHeader();
            block.Body.BlockHeader = block.Header.GetHash();            
            var publicKey = await _accountService.GetPublicKeyAsync();
            block.Sign(publicKey, data => _accountService.SignAsync(data));

            var manager = _mock.MinerClientManager();
            var blockExecutor = _mock.GetBlockExecutor(manager);

            var res = await blockExecutor.ExecuteBlock(block);
            Assert.NotEqual(BlockExecutionResult.Success, res);

            var blockchain = _mock.GetBlockChain(chain.Id); 
            var curHash = await blockchain.GetCurrentBlockHashAsync();
            var index = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Height;
            Assert.Equal(GlobalConfig.GenesisBlockHeight, index);
            Assert.Equal(chain.GenesisBlockHash.ToHex(), curHash.ToHex());
        }

        #region GRPC

        [Fact(Skip = "To be refactored")]
        public async Task SideChainServerClientsTest()
        {
            string dir = @"/tmp/ServerClientsTestA";
            _mock.ClearDirectory(dir);
            try
            {
                var chainId = ChainHelpers.GetChainId(123);
                GlobalConfig.MinimalBlockInfoCacheThreshold = 0;
                var port = 50052;
                var address = "127.0.0.1";
                var sideChainId = _mock.MockSideChainServer(port, address, dir);
                var parimpl = _mock.MockParentChainBlockInfoRpcServer();
                parimpl.Init(chainId);
                var sideimpl = _mock.MockSideChainBlockInfoRpcServer();
                sideimpl.Init(sideChainId);
                var serverManager = _mock.ServerManager(parimpl, sideimpl);
                serverManager.Init(chainId, dir);
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
                var libHeight = GlobalConfig.GenesisBlockHeight;
                MessageHub.Instance.Publish(new NewLibFound{Height = libHeight++});
                Thread.Sleep(t/2);
                var result = await manager.CollectSideChainBlockInfo();
                int count = result.Count;
                Assert.Equal(1, count);
                Assert.Equal(GlobalConfig.GenesisBlockHeight, result[0].Height);
                _mock.GetTimes++;
                
                MessageHub.Instance.Publish(new NewLibFound{Height = libHeight++});
                Thread.Sleep(t);
                result = await manager.CollectSideChainBlockInfo();
                count = result.Count;
                Assert.Equal(1, count);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 1, result[0].Height);
                _mock.GetTimes++;
                
                MessageHub.Instance.Publish(new NewLibFound{Height = libHeight++});
                Thread.Sleep(t);
                result = await manager.CollectSideChainBlockInfo();
                count = result.Count;
                Assert.Equal(1, count);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 2, result[0].Height);
                manager.CloseClientsToSideChain();
                _mock.GetTimes++;
                
                MessageHub.Instance.Publish(new NewLibFound{Height = libHeight++});
                Thread.Sleep(t);
                result = await manager.CollectSideChainBlockInfo();
                count = result.Count;
                Assert.Equal(0, count);
                
                // reset
                GrpcLocalConfig.Instance.ClientToSideChain = false;
                GrpcLocalConfig.Instance.SideChainServer = false;

            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
        }

        [Fact(Skip = "To be refactored")]
        public async Task ParentChainServerClientTest()
        {
            string dir = @"/tmp/ServerClientsTestB";
            _mock.ClearDirectory(dir);
            try
            {
                var chainId = ChainHelpers.GetChainId(123);
                GlobalConfig.MinimalBlockInfoCacheThreshold = 0;
                var port = 50053;
                var address = "127.0.0.1";
                
                var parentChainId = _mock.MockParentChainServer(port, address, dir);
                var parimpl = _mock.MockParentChainBlockInfoRpcServer();
                parimpl.Init(parentChainId);
                var sideimpl = _mock.MockSideChainBlockInfoRpcServer();
                sideimpl.Init(chainId);
                var serverManager = _mock.ServerManager(parimpl, sideimpl);
                serverManager.Init(chainId, dir);
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
                GrpcLocalConfig.Instance.ClientToSideChain = false;
                manager.Init(dir, t);

                GrpcLocalConfig.Instance.WaitingIntervalInMillisecond = 10;

                var libHeight = GlobalConfig.GenesisBlockHeight;
                MessageHub.Instance.Publish(new NewLibFound{Height = libHeight++});
                Thread.Sleep(t/2);
                var result = await manager.TryGetParentChainBlockInfo();
                Assert.NotNull(result);
                Assert.True(result.Count == 1);
                Assert.True(GlobalConfig.GenesisBlockHeight == result[0].Height);
                Assert.True(1 == result[0].IndexedBlockInfo.Count);
                Assert.True(result[0].IndexedBlockInfo.Keys.Contains(GlobalConfig.GenesisBlockHeight));
                //Assert.True(await manager.UpdateParentChainBlockInfo(result));
                var chainManager = _mock.MockChainManager().Object;
                await chainManager.UpdateCurrentBlockHeightAsync(result[0].ChainId, result[0].Height);
                _mock.GetTimes++;

                MessageHub.Instance.Publish(new NewLibFound{Height = libHeight++});
                Thread.Sleep(t);
                result = await manager.TryGetParentChainBlockInfo();
                Assert.NotNull(result);
                Assert.True(result.Count == 1);
                Assert.True(GlobalConfig.GenesisBlockHeight + 1 == result[0].Height);
                Assert.True(1 == result[0].IndexedBlockInfo.Count);
                Assert.True(result[0].IndexedBlockInfo.Keys.Contains(GlobalConfig.GenesisBlockHeight + 1));
                //Assert.True(await manager.UpdateParentChainBlockInfo(result));
                await chainManager.UpdateCurrentBlockHeightAsync(result[0].ChainId, result[0].Height);
                _mock.GetTimes++;

                MessageHub.Instance.Publish(new NewLibFound{Height = libHeight++});
                Thread.Sleep(t);
                result = await manager.TryGetParentChainBlockInfo();
                Assert.NotNull(result);
                Assert.True(result.Count == 1);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 2, result[0].Height);
                Assert.True(1 == result[0].IndexedBlockInfo.Count);
                Assert.True(result[0].IndexedBlockInfo.Keys.Contains(GlobalConfig.GenesisBlockHeight + 2));
                manager.CloseClientToParentChain();
                
                // reset
                GrpcLocalConfig.Instance.ClientToSideChain = false;
                GrpcLocalConfig.Instance.SideChainServer = false;
            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
        }
        
        /*[Fact(Skip = "TBD, side chain life time needed.")]
        public async Task MineWithIndexingSideChain()
        {
            // create the miners keypair, this is the miners identity
            var minerKeypair = new KeyPairGenerator().Generate();
            
            string dir = @"/tmp/minerpems";
            
            var chain = await _mock.CreateChain();
            
            var minerConfig = _mock.GetMinerConfig(chain.Id);
            
            ChainConfig.Instance.ChainId = chain.Id.DumpBase58();
            
            NodeConfig.Instance.ECKeyPair = minerKeypair;
            NodeConfig.Instance.NodeAccount = AddressHelpers.BuildAddress(chain.Id.DumpByteArray(), minerKeypair.PublicKey).GetFormatted();
            
            var pool = _mock.CreateAndInitTxHub();
            pool.Start();

            try
            {
                int sidePort = 50054;
                string address = "127.0.0.1";
                _mock.ClearDirectory(dir);
                GrpcRemoteConfig.Instance.ParentChain = null;
                var sideChainId = _mock.MockSideChainServer(sidePort, address, dir);
                //var parentChainId = _mock.MockParentChainServer(parentPort, address, dir);
                var parimpl = _mock.MockParentChainBlockInfoRpcServer();
                //parimpl.Init(parentChainId);
                var sideimpl = _mock.MockSideChainBlockInfoRpcServer();
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
                Thread.Sleep(t);

                var miner = _mock.GetMiner(minerConfig, pool, manager);
                miner.Init();
                
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
            
        }*/
        

        #endregion
    }
}