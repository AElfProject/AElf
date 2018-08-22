using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel;
using AElf.Miner.Rpc;
using AElf.Miner.Rpc.Server;
using Moq;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Miner.Tests.Grpc
{
    [UseAutofacTestFramework]
    public class HeaderInfoTest
    {
        private List<IBlockHeader> _headers = new List<IBlockHeader>();
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

        [Fact]
        public async Task HeaderInfoServerTest()
        {
            _headers = new List<IBlockHeader>
            {
                MockBlockHeader().Object,
                MockBlockHeader().Object,
                MockBlockHeader().Object
            };
            
            var headerInfoServer = new HeaderInfoServerImpl(MockChainService().Object);

            var requestInfo = new RequestIndexedInfo
            {
                ChainId = Hash.Generate(),
                From = Hash.Generate().ToAccount(),
                Height = 0
            };
            var responsedInfo = await headerInfoServer.GetHeaderInfo(requestInfo, null);
            Assert.Equal(_headers.Count, responsedInfo.Headers.Count);
            Assert.Equal(_headers[(int) requestInfo.Height].GetHash(), responsedInfo.Headers[0].BlockHeaderHash);

            requestInfo.Height = 1;
            responsedInfo = await headerInfoServer.GetHeaderInfo(requestInfo, null);
            Assert.Equal(_headers.Count - 1, responsedInfo.Headers.Count);
            Assert.Equal(_headers[(int) requestInfo.Height].GetHash(), responsedInfo.Headers[0].BlockHeaderHash);
            
            requestInfo.Height = 2;
            responsedInfo = await headerInfoServer.GetHeaderInfo(requestInfo, null);
            Assert.Equal(_headers.Count - 2, responsedInfo.Headers.Count);
            Assert.Equal(_headers[(int) requestInfo.Height].GetHash(), responsedInfo.Headers[0].BlockHeaderHash);
            
            requestInfo.Height = 3;
            responsedInfo = await headerInfoServer.GetHeaderInfo(requestInfo, null);
            Assert.Equal(_headers.Count - 3, responsedInfo.Headers.Count);
            
            requestInfo.Height = 4;
            responsedInfo = await headerInfoServer.GetHeaderInfo(requestInfo, null);
            Assert.Equal(0, responsedInfo.Headers.Count);
        }
        
    }
}