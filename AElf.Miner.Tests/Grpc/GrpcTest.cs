using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.Rpc;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using Moq;
using NLog;
using NServiceKit.Common.Extensions;
using Xunit;
using Xunit.Frameworks.Autofac;
using Uri = AElf.Configuration.Config.GRPC.Uri;

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
        public void ServerClientTest()
        {
            string dir = @"/tmp/ServerClientTest";
            _mock.CreateDirectory(dir);
            try
            {
                var sideChainId = _mock.MockServer(50052, "127.0.0.1", dir);

                // create client, main chian is client-side
                var manager = _mock.MinerClientManager();
                int t = 1000;
                manager.Init(dir, t);
                var client = manager.StartNewClientToSideChain(sideChainId.ToHex());

                CancellationTokenSource cancellationTokenSource =
                    new CancellationTokenSource(TimeSpan.FromMilliseconds(3000));
                client.Index(cancellationTokenSource.Token, 0);
                Thread.Sleep(t/2);
                // remove the first one
                Assert.Equal(1, client.IndexedInfoQueueCount);
                Assert.True(client.TryTake(10, out var responseSideChainIndexedInfo));
                Assert.Equal((ulong)0, responseSideChainIndexedInfo.Height);
                
                Thread.Sleep(t);
                Assert.Equal(1, client.IndexedInfoQueueCount);
                
                Thread.Sleep(t);
                Assert.Equal(2, client.IndexedInfoQueueCount);
                
                // remove 2nd item
                Assert.True(client.TryTake(10, out responseSideChainIndexedInfo));
                Assert.Equal(1, client.IndexedInfoQueueCount);
                Assert.Equal((ulong)1, responseSideChainIndexedInfo.Height);

                // remove 3rd item
                Assert.True(client.TryTake(10, out responseSideChainIndexedInfo));
                Assert.Equal(0, client.IndexedInfoQueueCount);
                Assert.Equal((ulong)2, responseSideChainIndexedInfo.Height);
                
                Assert.False(client.TryTake(10, out _));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
            
        }
        
        
        [Fact]
        public async Task ServerClientsTest()
        {
            string dir = @"/tmp/ServerClientsTest";
            _mock.CreateDirectory(dir);
            try
            {
                var sideChainId = _mock.MockServer(50052, "127.0.0.1", dir);
                
                // create client, main chian is client-side
                var manager = _mock.MinerClientManager();
                int t = 1000;
                manager.Init(dir);
                
                await manager.CreateClientsToSideChain();

                GrpcLocalConfig.Instance.WaitingIntervalInMillisecond = 10;
                Thread.Sleep(t/2);
                var result = await manager.CollectSideChainIndexedInfo();
                Assert.Equal(1, result.Count);
                Assert.Equal((ulong)0, result[0].Height);
                
                Thread.Sleep(t);
                result = await manager.CollectSideChainIndexedInfo();
                Assert.Equal(1, result.Count);
                Assert.Equal((ulong)1, result[0].Height);
                
                Thread.Sleep(t);
                result = await manager.CollectSideChainIndexedInfo();
                Assert.Equal(1, result.Count);
                Assert.Equal((ulong)2, result[0].Height);
                
                Thread.Sleep(t);
                result = await manager.CollectSideChainIndexedInfo();
                Assert.Equal(0, result.Count);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
        }
        
        [Fact]
        public async Task MineWithIndexingSideChain()
        {
            string dir = @"/tmp/minerpems";
            var chain = await _mock.CreateChain();
            var poolService = _mock.CreateTxPoolService(chain.Id);
            poolService.Start();

            try
            {
                _mock.CreateDirectory(dir);
                _mock.MockServer(50054, "127.0.0.1", dir);
                var keypair = new KeyPairGenerator().Generate();
                var minerconfig = _mock.GetMinerConfig(chain.Id, 10, keypair.GetAddress());
                var manager = _mock.MinerClientManager();
                int t = 1000;
                manager.Init(dir, t);
                var miner = _mock.GetMiner(minerconfig, poolService, manager);
                GrpcLocalConfig.Instance.Client = true;
                miner.Init(keypair);
            
                Thread.Sleep(t/2);
                var block = await miner.Mine();
                Assert.NotNull(block);
                Assert.NotNull(block.Header.IndexedInfo);
                Assert.Equal(1, block.Header.IndexedInfo.Count);
                Assert.Equal((ulong)0, block.Header.IndexedInfo[0].Height);
                Assert.Equal((ulong)1, block.Header.Index);
            
                Thread.Sleep(t);
                block = await miner.Mine();
                Assert.NotNull(block);
                Assert.NotNull(block.Header.IndexedInfo);
                Assert.Equal(1, block.Header.IndexedInfo.Count);
                Assert.Equal((ulong)1, block.Header.IndexedInfo[0].Height);
                Assert.Equal((ulong)2, block.Header.Index);
            
                Thread.Sleep(t);
                block = await miner.Mine();
                Assert.NotNull(block);
                Assert.NotNull(block.Header.IndexedInfo);
                Assert.Equal(1, block.Header.IndexedInfo.Count);
                Assert.Equal((ulong)2, block.Header.IndexedInfo[0].Height);
                Assert.Equal((ulong)3, block.Header.Index);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                Directory.Delete(Path.Combine(dir), true);
            }
            
        }
    }
}