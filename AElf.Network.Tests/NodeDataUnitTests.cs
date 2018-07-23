using AElf.Network.Data;
using Xunit;
using NodeData = AElf.Network.Data.Protobuf.NodeData;

namespace AElf.Network.Tests
{
    public class NodeDataUnitTests
    {
        [Fact]
        public void Equals_ShouldReturnTrue_SameInstance()
        {
            NodeData node01 = new NodeData();
            NodeData node02 = node01;

            Assert.True(node01.Equals(node02));
        }
        
        [Fact]
        public void Equals_ShouldReturnTrue_SameData()
        {
            NodeData node01 = new NodeData
            {
                IpAddress = "127.0.0.1",
                Port = 6789
            };

            NodeData node02 = new NodeData
            {
                IpAddress = "127.0.0.1",
                Port = 6789
            };

            Assert.True(node01.Equals(node02));
        }
        
        [Fact]
        public void Equals_ShouldReturnFalse_DifferentIp()
        {
            NodeData node01 = new NodeData
            {
                IpAddress = "127.0.0.1",
                Port = 6789
            };

            NodeData node02 = new NodeData
            {
                IpAddress = "127.0.0.2",
                Port = 6789
            };

            Assert.False(node01.Equals(node02));
        }
        
        [Fact]
        public void Equals_ShouldReturnFalse_DifferentPort()
        {
            NodeData node01 = new NodeData
            {
                IpAddress = "127.0.0.1",
                Port = 6789
            };

            NodeData node02 = new NodeData
            {
                IpAddress = "127.0.0.1",
                Port = 6790
            };

            Assert.False(node01.Equals(node02));
        }
    }
}