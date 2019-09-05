using Shouldly;
using Xunit;

namespace AElf.Database.Tests
{
    public class HelperTests
    {
        [Fact]
        public void Memchr_Query_Test()
        {
            var data = new byte[4] { 0x01, 0x02, 0x03, 0x04 };
            Helper.Memchr(data, 0x03, 0).ShouldBe(2);
            Helper.Memchr(data, 0x09, 2).ShouldBe(-1);
        }

        [Fact]
        public void Byte_Convert_Test()
        {
            var data = "hello aelf";
            Helper.StringToBytes(null).ShouldBe(null);

            var array = Helper.StringToBytes(data);
            array.ShouldNotBe(null);

            Helper.BytesToString(null).ShouldBe(null);
            Helper.BytesToString(array).ShouldBe(data);
        }
    }
}