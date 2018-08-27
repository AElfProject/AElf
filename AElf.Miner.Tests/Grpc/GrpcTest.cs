using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Miner.Rpc;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using Moq;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

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
            return new MinerServer(_logger, new HeaderInfoServerImpl(MockChainService().Object));
        }

        public MinerClient MinerClient()
        {
            return new MinerClient();
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
            if(Directory.Exists((Path.Combine(dir, "certs"))))
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
                var sideChainId = ByteArrayHelpers.FromHexString("0xdb4a9b4fdbc3fa6b3ad07052c5bb3080d6f72635365fa243be5e7250f030cef8");
                MockKeyPair(sideChainId, dir);
                //start server, sidechain is server-side
                server.Init(sideChainId, dir);
                server.StartUp();

                // create client, main chian is client-side
                var client = MinerClient();
                client.Init(dir);
                client.StartNewClient(sideChainId);
                var requestInfo = new RequestIndexedInfo
                {
                    ChainId = _chainId,
                    From = Hash.Generate().ToAccount(),
                    Height = 0
                };

                var resp = client.GetHeaderInfo(sideChainId, requestInfo);
                Assert.Equal(_headers.Count - (int) requestInfo.Height, resp.Headers.Count);
                Assert.Equal(_headers[(int) requestInfo.Height].GetHash(), resp.Headers[(int) requestInfo.Height].BlockHeaderHash);
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