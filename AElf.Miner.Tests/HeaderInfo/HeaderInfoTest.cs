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
using AElf.Common;

namespace AElf.Miner.Tests.Grpc
{
    [UseAutofacTestFramework]
    public class HeaderInfoTest
    {
        private readonly ILogger _logger;

        private List<IBlockHeader> _headers = new List<IBlockHeader>();
        private List<RequestBlockInfo> _requestIndexedInfoList = new List<RequestBlockInfo>();
        private List<ResponseSideChainBlockInfo> _responseIndexedInfoMessages = new List<ResponseSideChainBlockInfo>();

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

        /*public Mock<IAsyncEnumerator<RequestBlockInfo>> MockEnumerator(int count)
        {
            Mock<IAsyncEnumerator<RequestBlockInfo>> mock =
                new Mock<IAsyncEnumerator<RequestBlockInfo>>();
            int i = 0;
            int j = 0;
            mock.Setup(rs => rs.MoveNext()).Returns(() => Task.FromResult(i++ < count));
            mock.Setup(rs => rs.Current).Returns(() => j < count ? _requestIndexedInfoList[j++] : null);
            return mock;
        }*/

        public Mock<IAsyncStreamReader<RequestBlockInfo>> MockRequestStream(int count)
        {
            Mock<IAsyncStreamReader<RequestBlockInfo>> mock =
                new Mock<IAsyncStreamReader<RequestBlockInfo>>();
            int i = 0;
            int j = 0;
            mock.Setup(rs => rs.MoveNext(It.IsAny<CancellationToken>())).Returns(() => Task.FromResult(i++ < count));
            mock.Setup(rs => rs.Current).Returns(() => j < count ? _requestIndexedInfoList[j++] : null);

            return mock;
        }

        public Mock<IServerStreamWriter<ResponseSideChainBlockInfo>> MockResponseStream()
        {
            Mock<IServerStreamWriter<ResponseSideChainBlockInfo>> mock =
                new Mock<IServerStreamWriter<ResponseSideChainBlockInfo>>();
            mock.Setup(rs => rs.WriteAsync(It.IsAny<ResponseSideChainBlockInfo>()))
                .Returns<ResponseSideChainBlockInfo>(res =>
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
            
            _requestIndexedInfoList = new List<RequestBlockInfo>
            {
                new RequestBlockInfo
                {
                    NextHeight = 0
                },
                new RequestBlockInfo
                {
                    NextHeight = 1
                },
                new RequestBlockInfo
                {
                    NextHeight = 2
                }
            };
            
            
            var headerInfoServer = new SideChainBlockInfoRpcServerImpl(MockChainService().Object, _logger);
            var chainId = Hash.Generate();
            headerInfoServer.Init(chainId);
            await headerInfoServer.IndexDuplexStreaming(MockRequestStream(_requestIndexedInfoList.Count).Object,
                MockResponseStream().Object, null);
            Assert.Equal(_requestIndexedInfoList.Count, _requestIndexedInfoList.Count);

        }
        
    }
}