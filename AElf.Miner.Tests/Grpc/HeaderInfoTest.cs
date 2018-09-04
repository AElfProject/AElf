using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel;
using AElf.Miner.Rpc;
using AElf.Miner.Rpc.Server;
using Grpc.Core;
using Moq;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Miner.Tests.Grpc
{
    [UseAutofacTestFramework]
    public class HeaderInfoTest
    {
        private readonly ILogger _logger;

        private List<IBlockHeader> _headers = new List<IBlockHeader>();
        private List<RequestIndexedInfoMessage> _requestIndexedInfoList = new List<RequestIndexedInfoMessage>();
        private List<ResponseIndexedInfoMessage> _responseIndexedInfoMessages = new List<ResponseIndexedInfoMessage>();

        public HeaderInfoTest(ILogger logger)
        {
            _logger = logger;
        }

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

        /*public Mock<IAsyncEnumerator<RequestIndexedInfoMessage>> MockEnumerator(int count)
        {
            Mock<IAsyncEnumerator<RequestIndexedInfoMessage>> mock =
                new Mock<IAsyncEnumerator<RequestIndexedInfoMessage>>();
            int i = 0;
            int j = 0;
            mock.Setup(rs => rs.MoveNext()).Returns(() => Task.FromResult(i++ < count));
            mock.Setup(rs => rs.Current).Returns(() => j < count ? _requestIndexedInfoList[j++] : null);
            return mock;
        }*/

        public Mock<IAsyncStreamReader<RequestIndexedInfoMessage>> MockRequestStream(int count)
        {
            Mock<IAsyncStreamReader<RequestIndexedInfoMessage>> mock =
                new Mock<IAsyncStreamReader<RequestIndexedInfoMessage>>();
            int i = 0;
            int j = 0;
            mock.Setup(rs => rs.MoveNext(It.IsAny<CancellationToken>())).Returns(() => Task.FromResult(i++ < count));
            mock.Setup(rs => rs.Current).Returns(() => j < count ? _requestIndexedInfoList[j++] : null);

            return mock;
        }

        public Mock<IServerStreamWriter<ResponseIndexedInfoMessage>> MockResponseStream()
        {
            Mock<IServerStreamWriter<ResponseIndexedInfoMessage>> mock =
                new Mock<IServerStreamWriter<ResponseIndexedInfoMessage>>();
            mock.Setup(rs => rs.WriteAsync(It.IsAny<ResponseIndexedInfoMessage>()))
                .Returns<ResponseIndexedInfoMessage>(res =>
                {
                    _responseIndexedInfoMessages.Add(res);
                    return Task.CompletedTask;
                });
            return mock;
        }
        

        [Fact]
        public async Task HeaderInfoServerTest()
        {
            _headers = new List<IBlockHeader>
            {
                MockBlockHeader().Object,
                MockBlockHeader().Object,
                MockBlockHeader().Object
            };
            
            _requestIndexedInfoList = new List<RequestIndexedInfoMessage>
            {
                new RequestIndexedInfoMessage
                {
                    NextHeight = 0
                },
                new RequestIndexedInfoMessage
                {
                    NextHeight = 1
                },
                new RequestIndexedInfoMessage
                {
                    NextHeight = 2
                }
            };
            
            
            var headerInfoServer = new HeaderInfoServerImpl(MockChainService().Object, _logger);
            var chainId = Hash.Generate();
            headerInfoServer.Init(chainId);
            await headerInfoServer.Index(MockRequestStream(_requestIndexedInfoList.Count).Object,
                MockResponseStream().Object, null);
            Assert.Equal(_requestIndexedInfoList.Count, _requestIndexedInfoList.Count);

        }
        
    }
}