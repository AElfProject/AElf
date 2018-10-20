using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Cryptography.ECDSA;
using Xunit;
using Xunit.Frameworks.Autofac;
using Uri = AElf.Configuration.Config.GRPC.Uri;
using AElf.Configuration;

namespace AElf.Miner.Tests.Grpc
{
    [UseAutofacTestFramework]
    public class GrpcTest
    {
        private readonly MockSetup _mock;

        public GrpcTest(MockSetup mock)
        {
            _mock = mock;
        }

        [Fact]
        public async Task SideChainServerClientsTest()
        {
            string dir = @"/tmp/ServerClientsTest";
            _mock.ClearDirectory(dir);
            try
            {
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
                        sideChainId.DumpHex(), new Uri{
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
                Assert.Equal((ulong)0, result[0].Height);
                
                Thread.Sleep(t);
                result = await manager.CollectSideChainBlockInfo();
                count = result.Count;
                Assert.Equal(1, count);
                Assert.Equal((ulong)1, result[0].Height);
                
                Thread.Sleep(t);
                result = await manager.CollectSideChainBlockInfo();
                count = result.Count;
                Assert.Equal(1, count);
                Assert.Equal((ulong)2, result[0].Height);
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
                        parentChainId.DumpHex(), new Uri{
                            Address = address,
                            Port = port
                        }
                    }
                };
                GrpcLocalConfig.Instance.ClientToParentChain = true;
                manager.Init(dir, t);

                GrpcLocalConfig.Instance.WaitingIntervalInMillisecond = 10;
                Thread.Sleep(t/2);
                var result = await manager.TryGetParentChainBlockInfo();
                Assert.NotNull(result);
                Assert.Equal((ulong)0, result.Height);
                Assert.Equal(1, result.IndexedBlockInfo.Count);
                Assert.True(result.IndexedBlockInfo.Keys.Contains(GlobalConfig.GenesisBlockHeight));
                Assert.True(await manager.UpdateParentChainBlockInfo(result));
                
                Thread.Sleep(t);
                result = await manager.TryGetParentChainBlockInfo();
                Assert.NotNull(result);
                Assert.Equal((ulong)1, result.Height);
                Assert.Equal(1, result.IndexedBlockInfo.Count);
                Assert.True(result.IndexedBlockInfo.Keys.Contains(GlobalConfig.GenesisBlockHeight + 1));
                Assert.True(await manager.UpdateParentChainBlockInfo(result));

                Thread.Sleep(t);
                result = await manager.TryGetParentChainBlockInfo();
                Assert.NotNull(result);
                Assert.Equal((ulong)2, result.Height);
                Assert.Equal(1, result.IndexedBlockInfo.Count);
                Assert.True(result.IndexedBlockInfo.Keys.Contains(GlobalConfig.GenesisBlockHeight + 2));
                manager.CloseClientToParentChain();
            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
        }
        
        [Fact(Skip = "")]
        public async Task MineWithIndexingSideChain()
        {
            string dir = @"/tmp/minerpems";
            var chain = await _mock.CreateChain();
            var keyPair = new KeyPairGenerator().Generate();
            var minerConfig = _mock.GetMinerConfig(chain.Id, 10, keyPair.GetAddress().DumpByteArray());
            NodeConfig.Instance.ECKeyPair = keyPair;
            NodeConfig.Instance.NodeAccount = keyPair.GetAddressHex();
            NodeConfig.Instance.ChainId = chain.Id.DumpHex();
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
                        sideChainId.DumpHex(), new Uri{
                            Address = address,
                            Port = sidePort
                        }
                    }
                };
                
                GrpcLocalConfig.Instance.ClientToSideChain = true;
                manager.Init(dir, t);
                var miner = _mock.GetMiner(minerConfig, pool, manager);
                miner.Init();
            
                Thread.Sleep(t/2);
                var block = await miner.Mine();
                Assert.NotNull(block);
                Assert.NotNull(block.Body.IndexedInfo);
                int count = block.Body.IndexedInfo.Count;
                Assert.Equal(1, count);
                Assert.Equal((ulong) 0, block.Body.IndexedInfo[0].Height);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 1, block.Header.Index);
            
                Thread.Sleep(t);
                block = await miner.Mine();
                Assert.NotNull(block);
                Assert.NotNull(block.Body.IndexedInfo);
                count = block.Body.IndexedInfo.Count;
                Assert.Equal(1, count);
                Assert.Equal((ulong)1, block.Body.IndexedInfo[0].Height);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 2, block.Header.Index);
            
                Thread.Sleep(t);
                block = await miner.Mine();
                Assert.NotNull(block);
                Assert.NotNull(block.Body.IndexedInfo);
                count = block.Body.IndexedInfo.Count;
                Assert.Equal(1, count);
                Assert.Equal((ulong)2, block.Body.IndexedInfo[0].Height);
                Assert.Equal(GlobalConfig.GenesisBlockHeight + 3, block.Header.Index);
                
                manager.CloseClientsToSideChain();
            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
            
        }
    }
}