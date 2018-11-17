using System;
using AElf.Network.Connection;
using Xunit;

namespace AElf.Network.Tests
{
    
    public class MessageWriterTests
    { 
        [Fact]
        public void PayloadToPartials_OnLimit_ReturnsOnePartial()
        {
            MessageWriter writer = new MessageWriter(null);
            writer.MaxOutboundPacketSize = 10;
            
            byte[] bytes = NetworkTestHelpers.GetRandomBytes(10);

            var partials = writer.PayloadToPartials(0, bytes, writer.MaxOutboundPacketSize);
            
            Assert.NotNull(partials);
            Assert.Equal(partials.Count, 1);
            
            Assert.Equal(partials[0].IsEnd, true);
            Assert.Equal(partials[0].Data.Length, 10);
            
            Assert.Equal(partials[0].Position, 0);
        }
        
        [Fact]
        public void PayloadToPartials_JustAboveLimit_ReturnsTwoPartial()
        {
            MessageWriter writer = new MessageWriter(null);
            writer.MaxOutboundPacketSize = 10;
            
            byte[] bytes = NetworkTestHelpers.GetRandomBytes(11);

            var partials = writer.PayloadToPartials(0, bytes, writer.MaxOutboundPacketSize);
            
            Assert.NotNull(partials);
            Assert.Equal(partials.Count, 2);
            
            // First should not be end, second should be
            Assert.Equal(partials[0].IsEnd, false);
            Assert.Equal(partials[1].IsEnd, true);
            
            // Validate lengths
            Assert.Equal(partials[0].Data.Length, 10);
            Assert.Equal(partials[1].Data.Length, 1);
            
            // validate positions
            Assert.Equal(partials[0].Position, 0);
            Assert.Equal(partials[1].Position, 1);
        }
        
        [Fact]
        public void PayloadToPartials_ExactlyTwice_ReturnsTwoPartial()
        {
            MessageWriter writer = new MessageWriter(null);
            writer.MaxOutboundPacketSize = 10;
            
            byte[] bytes = NetworkTestHelpers.GetRandomBytes(20);

            var partials = writer.PayloadToPartials(0, bytes, writer.MaxOutboundPacketSize);
            
            Assert.NotNull(partials);
            Assert.Equal(partials.Count, 2);
            
            // First should not be end, second should be
            Assert.Equal(partials[0].IsEnd, false);
            Assert.Equal(partials[1].IsEnd, true);
            
            // Validate lengths
            Assert.Equal(partials[0].Data.Length, 10);
            Assert.Equal(partials[1].Data.Length, 10);
            
            // validate positions
            Assert.Equal(partials[0].Position, 0);
            Assert.Equal(partials[1].Position, 1);
        }
    }
}