using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using Moq;
using Xunit;
using AElf.Common;
using AElf.Crosschain.Grpc;
using AElf.Crosschain.Grpc.Server;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Miner.Tests.HeaderInfo
{
public class HeaderInfoTest
    {
        public ILogger<HeaderInfoTest> Logger {get;set;}

        private List<IBlockHeader> _headers = new List<IBlockHeader>();
        private List<RequestCrossChainBlockData> _requestIndexedInfoList = new List<RequestCrossChainBlockData>();
        private List<ResponseSideChainBlockData> _responseIndexedInfoMessages = new List<ResponseSideChainBlockData>();

        public HeaderInfoTest()
        {
            Logger = NullLogger<HeaderInfoTest>.Instance;
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
            mock.Setup(cs => cs.GetLightChain(It.IsAny<int>())).Returns(MockLightChain().Object);
            return mock;
        }

        public Mock<IBlockHeader> MockBlockHeader()
        {
            Mock<IBlockHeader> mock = new Mock<IBlockHeader>();
            mock.Setup(bh => bh.GetHash()).Returns(Hash.Generate());
            mock.Setup(bh => bh.MerkleTreeRootOfTransactions).Returns(Hash.Generate());
            return mock;
        }

        /*public Mock<IAsyncEnumerator<RequestCrossChainBlockData>> MockEnumerator(int count)
        {
            Mock<IAsyncEnumerator<RequestCrossChainBlockData>> mock =
                new Mock<IAsyncEnumerator<RequestCrossChainBlockData>>();
            int i = 0;
            int j = 0;
            mock.Setup(rs => rs.MoveNext()).Returns(() => Task.FromResult(i++ < count));
            mock.Setup(rs => rs.Current).Returns(() => j < count ? _requestIndexedInfoList[j++] : null);
            return mock;
        }*/

        public Mock<IAsyncStreamReader<RequestCrossChainBlockData>> MockRequestStream(int count)
        {
            Mock<IAsyncStreamReader<RequestCrossChainBlockData>> mock =
                new Mock<IAsyncStreamReader<RequestCrossChainBlockData>>();
            int i = 0;
            int j = 0;
            mock.Setup(rs => rs.MoveNext(It.IsAny<CancellationToken>())).Returns(() => Task.FromResult(i++ < count));
            mock.Setup(rs => rs.Current).Returns(() => j < count ? _requestIndexedInfoList[j++] : null);

            return mock;
        }

        public Mock<IServerStreamWriter<ResponseSideChainBlockData>> MockResponseStream()
        {
            Mock<IServerStreamWriter<ResponseSideChainBlockData>> mock =
                new Mock<IServerStreamWriter<ResponseSideChainBlockData>>();
            mock.Setup(rs => rs.WriteAsync(It.IsAny<ResponseSideChainBlockData>()))
                .Returns<ResponseSideChainBlockData>(res =>
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
            
            _requestIndexedInfoList = new List<RequestCrossChainBlockData>
            {
                new RequestCrossChainBlockData
                {
                    NextHeight = 0
                },
                new RequestCrossChainBlockData
                {
                    NextHeight = 1
                },
                new RequestCrossChainBlockData
                {
                    NextHeight = 2
                }
            };


            var headerInfoServer = new SideChainBlockInfoRpcServer(MockChainService().Object);
            var chainId = ChainHelpers.GetChainId(123);
            headerInfoServer.Init(chainId);
            await headerInfoServer.RequestSideChainDuplexStreaming(MockRequestStream(_requestIndexedInfoList.Count).Object,
                MockResponseStream().Object, null);
            Assert.Equal(_requestIndexedInfoList.Count, _requestIndexedInfoList.Count);

        }
        
    }
}