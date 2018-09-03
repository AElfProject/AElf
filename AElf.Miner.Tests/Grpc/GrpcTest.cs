using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
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
        private readonly ILogger _logger;
        private List<IBlockHeader> _headers = new List<IBlockHeader>();
        private Hash _chainId;
        public Mock<ILightChain> MockLightChain()
        {
            Mock<ILightChain> mock = new Mock<ILightChain>();
            mock.Setup(lc => lc.GetCurrentBlockHeightAsync()).Returns(Task.FromResult((ulong)_headers.Count - 1));
            mock.Setup(lc => lc.GetHeaderByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(_headers[(int) p]));

            return mock;
        }
        
        public Mock<IChainService> MockChainService()
        {
            Mock<IChainService> mock = new Mock<IChainService>();
            mock.Setup(cs => cs.GetLightChain(It.IsAny<Hash>())).Returns(MockLightChain().Object);
            return mock;
        }

        public Mock<IBlockHeader> MockBlockHeader()
        {
            Mock<IBlockHeader> mock = new Mock<IBlockHeader>();
            mock.Setup(bh => bh.GetHash()).Returns(Hash.Generate());
            mock.Setup(bh => bh.MerkleTreeRootOfTransactions).Returns(Hash.Generate());
            return mock;
        }

        public GrpcTest(ILogger logger)
        {
            _logger = logger;
        }

        public MinerServer MinerServer()
        {
            return new MinerServer(_logger, new HeaderInfoServerImpl(MockChainService().Object, _logger));
        }

        public MinerClientGenerator MinerClientGenerator()
        {
            return new MinerClientGenerator(_logger);
        }


        public void MockKeyPair(Hash chainId, string dir)
        {
            
            var certificateStore = new CertificateStore(dir);
            var name = chainId.ToHex();
            var keyPair = certificateStore.WriteKeyAndCertificate(name, "127.0.0.1");
        }
        
        [Fact]
        public void ServerTest()
        {
            string dir = @"/tmp/pems";
            if(Directory.Exists(Path.Combine(dir, "certs")))
                Directory.Delete(Path.Combine(dir, "certs"), true);
            try
            {
                _chainId = Hash.Generate();
            
                _headers = new List<IBlockHeader>
                {
                    MockBlockHeader().Object,
                    MockBlockHeader().Object,
                    MockBlockHeader().Object
                };
                var server = MinerServer();
                var sideChainId = Hash.Generate();
                MockKeyPair(sideChainId, dir);
                //start server, sidechain is server-side
                GrpcLocalConfig.Instance.LocalServerPort = 50052;
                GrpcLocalConfig.Instance.LocalServerIP = "127.0.0.1";

                server.Init(sideChainId, dir);
                server.StartUp();

                // create client, main chian is client-side
                var generator = MinerClientGenerator();
                generator.Init(dir);
                GrpcRemoteConfig.Instance.ChildChains = new Dictionary<string, Uri>
                {
                    {sideChainId.ToHex(), new Uri
                    {
                        Address = GrpcLocalConfig.Instance.LocalServerIP,
                        Port = GrpcLocalConfig.Instance.LocalServerPort
                    }}
                };
                var client = generator.StartNewClientToSideChain(sideChainId);

                CancellationTokenSource cancellationTokenSource =
                    new CancellationTokenSource(TimeSpan.FromMilliseconds(3000));
                client.Index(cancellationTokenSource.Token, 0);
                Thread.Sleep(500);
                Assert.Equal(1, client.IndexedInfoQueue.Count);
                Assert.Equal((ulong)0, ((ResponseIndexedInfoMessage)client.IndexedInfoQueue.First()).Height);
                // remove the first one
                Assert.True(client.IndexedInfoQueue.TryTake(out _));
                
                Thread.Sleep(1000);
                Assert.Equal(1, client.IndexedInfoQueue.Count);
                Assert.Equal((ulong)1, ((ResponseIndexedInfoMessage)client.IndexedInfoQueue.First()).Height);
                Thread.Sleep(1000);
                Assert.Equal(2, client.IndexedInfoQueue.Count);
                Assert.Equal((ulong)1, ((ResponseIndexedInfoMessage)client.IndexedInfoQueue.First()).Height);
                
                // remove 2rd item
                Assert.True(client.IndexedInfoQueue.TryTake(out _));
                Assert.Equal(1, client.IndexedInfoQueue.Count);
                Assert.Equal((ulong)2, ((ResponseIndexedInfoMessage)client.IndexedInfoQueue.First()).Height);

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