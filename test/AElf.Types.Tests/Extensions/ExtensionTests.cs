using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Google.Protobuf;
using Shouldly;

namespace AElf.Types.Tests.Extensions
{
    public class ExtensionTests
    {
        [Fact]
        public void String_Extension_Methods_Test()
        {
            var hexValue = Hash.FromString("hx").ToHex();

            var hexValueWithPrefix = hexValue.AppendHexPrefix();
            hexValueWithPrefix.Substring(0, 2).ShouldBe("0x");
            var hexValueWithPrefix1 = hexValueWithPrefix.AppendHexPrefix();
            hexValueWithPrefix1.ShouldBeSameAs(hexValueWithPrefix);

            var byteArray = Hash.FromString("hx").ToByteArray();
            var hexString = byteArray.ToHex(true);
            hexString.Substring(0, 2).ShouldBe("0x");

            var hex = hexValueWithPrefix.RemoveHexPrefix();
            hex.ShouldBe(hexValue);
            var hex1 = hex.RemoveHexPrefix();
            hex1.ShouldBeSameAs(hex);

            var hash1 = hexValue.ComputeHash();
            hash1.ShouldNotBe(null);
        }

        [Fact]
        public void Number_Extensions_Methods_Test()
        {
            //ulong
            var uNumber = (ulong)10;
            var byteArray = uNumber.ToBytes();
            byteArray.ShouldNotBe(null);

            //int
            var iNumber = 10;
            var byteArray1 = iNumber.DumpByteArray();
            byteArray1.ShouldNotBe(null);

            //hash
            var hash = iNumber.ToHash();
            hash.ShouldNotBe(null);
        }

        [Fact]
        public void Byte_Extensions_ToPlainBase58_Test()
        {
            var emptyByteString = ByteString.Empty;
            emptyByteString.ToPlainBase58().ShouldBe(string.Empty);
            
            var byteString = ByteString.CopyFromUtf8("5ta1yvi2dFEs4V7YLPgwkbnn816xVUvwWyTHPHcfxMVLrLB");
            byteString.ToPlainBase58().ShouldBe("SmUQnCq4Ffvy8UeR9EEV9DhNVcNaLhGpqFTDZfzdebANJAgngqe8RfT1sqPPqJQ9");

            var bytes = new byte[] {0, 0, 0};
            byteString = ByteString.CopyFrom(bytes);
            byteString.ToPlainBase58().ShouldBe("111");
        }

        [Fact]
        public async Task Task_Extensions_Test_One_Cancellation()
        {
            var ct = new CancellationTokenSource(100);
            int times = 2;
            var task = CountAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct.Token));
        }
        
        [Fact]
        public async Task Task_Extensions_Test_One_Cancellation_NotCancelled()
        {
            var ct = new CancellationTokenSource(300);
            int times = 2;
            var task = CountAsync(times, 100);
            int res = await task.WithCancellation(ct.Token);
            Assert.Equal(times, res);
        }

        [Fact]
        public async Task Task_Extensions_Test_Two_BothCanceled()
        {
            var ct1 = new CancellationTokenSource(100);
            var ct2 = new CancellationTokenSource(100);
            int times = 2;
            var task = CountAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct1.Token, ct2.Token));
        }
        
        [Fact]
        public async Task Task_Extensions_Test_Two_Cancellations()
        {
            var ct1 = new CancellationTokenSource(300);
            var ct2 = new CancellationTokenSource(100);
            int times = 2;
            var task = CountAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct1.Token, ct2.Token));
        }
        
        [Fact]
        public async Task Task_Extensions_Test_Two_Cancellations_NotCanceled()
        {
            var ct1 = new CancellationTokenSource(300);
            var ct2 = new CancellationTokenSource(300);
            int times = 2;
            var task = CountAsync(times, 100);
            var res = await task.WithCancellation(ct1.Token, ct2.Token);
            Assert.Equal(times, res);
        }
        
        private async Task<int> CountAsync(int times, int waitingPeriodMilliSecond)
        {
            int i = 0;
            while (i < times)
            {
                await Task.Delay(waitingPeriodMilliSecond);
                i++;
            }
            
            return i; 
        }
    }
}