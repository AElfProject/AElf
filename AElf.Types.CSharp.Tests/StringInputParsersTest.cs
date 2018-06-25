using System;
using System.Collections.Generic;
using AElf.Kernel;
using Google.Protobuf;
using Xunit;

namespace AElf.Types.CSharp.Tests
{
    public class StringInputParsersTest
    {
        [Fact]
        public void BoolTest()
        {
            var parser1 = StringInputParsers.GetStringParserFor(typeof(bool).FullName);
            var parser2 = StringInputParsers.GetStringParserFor(typeof(bool).FullName.ToShorterName());
            Assert.True((bool)parser1("true"));
            Assert.True((bool)parser2("true"));
            Assert.False((bool)parser1("false"));
            Assert.False((bool)parser2("false"));
            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));
        }

        [Fact]
        public void IntTest()
        {
            var parser1 = StringInputParsers.GetStringParserFor(typeof(int).FullName);
            var parser2 = StringInputParsers.GetStringParserFor(typeof(int).FullName.ToShorterName());
            Assert.Equal((int)10, parser1("10"));
            Assert.Equal((int)10, parser2("10"));
            Assert.Equal((int)16, parser1("0x10"));
            Assert.Equal((int)16, parser2("0x10"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));

            string max = int.MaxValue.ToString();
            Assert.Equal(int.MaxValue, parser1(max));
            Assert.Equal(int.MaxValue, parser2(max));

            string min = int.MinValue.ToString();
            Assert.Equal(int.MinValue, parser1(min));
            Assert.Equal(int.MinValue, parser2(min));

            string tooBig = (((long)int.MaxValue) + 1).ToString();
            Assert.ThrowsAny<Exception>(() => parser1(tooBig));
            Assert.ThrowsAny<Exception>(() => parser2(tooBig));
        }

        [Fact]
        public void UIntTest()
        {
            var parser1 = StringInputParsers.GetStringParserFor(typeof(uint).FullName);
            var parser2 = StringInputParsers.GetStringParserFor(typeof(uint).FullName.ToShorterName());
            Assert.Equal((uint)10, parser1("10"));
            Assert.Equal((uint)10, parser2("10"));
            Assert.Equal((uint)16, parser1("0x10"));
            Assert.Equal((uint)16, parser2("0x10"));

            Assert.ThrowsAny<Exception>(() => parser1("-1"));
            Assert.ThrowsAny<Exception>(() => parser2("-1"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));

            string max = uint.MaxValue.ToString();
            Assert.Equal(uint.MaxValue, parser1(max));
            Assert.Equal(uint.MaxValue, parser2(max));

            string min = uint.MinValue.ToString();
            Assert.Equal(uint.MinValue, parser1(min));
            Assert.Equal(uint.MinValue, parser2(min));

            string tooBig = (((long)uint.MaxValue) + 1).ToString();
            Assert.ThrowsAny<Exception>(() => parser1(tooBig));
            Assert.ThrowsAny<Exception>(() => parser2(tooBig));
        }

        [Fact]
        public void Int64Test()
        {
            var parser1 = StringInputParsers.GetStringParserFor(typeof(long).FullName);
            var parser2 = StringInputParsers.GetStringParserFor(typeof(long).FullName.ToShorterName());
            Assert.Equal((long)10, parser1("10"));
            Assert.Equal((long)10, parser2("10"));
            Assert.Equal((long)16, parser1("0x10"));
            Assert.Equal((long)16, parser2("0x10"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));

            string max = long.MaxValue.ToString();
            Assert.Equal(long.MaxValue, parser1(max));
            Assert.Equal(long.MaxValue, parser2(max));

            string min = long.MinValue.ToString();
            Assert.Equal(long.MinValue, parser1(min));
            Assert.Equal(long.MinValue, parser2(min));

            string tooBig = (long.MaxValue).ToString() + "1";
            Assert.ThrowsAny<Exception>(() => parser1(tooBig));
            Assert.ThrowsAny<Exception>(() => parser2(tooBig));
        }

        [Fact]
        public void UInt64Test()
        {
            var parser1 = StringInputParsers.GetStringParserFor(typeof(ulong).FullName);
            var parser2 = StringInputParsers.GetStringParserFor(typeof(ulong).FullName.ToShorterName());
            Assert.Equal((ulong)10, parser1("10"));
            Assert.Equal((ulong)10, parser2("10"));
            Assert.Equal((ulong)16, parser1("0x10"));
            Assert.Equal((ulong)16, parser2("0x10"));

            Assert.ThrowsAny<Exception>(() => parser1("-1"));
            Assert.ThrowsAny<Exception>(() => parser2("-1"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));

            string max = ulong.MaxValue.ToString();
            Assert.Equal(ulong.MaxValue, parser1(max));
            Assert.Equal(ulong.MaxValue, parser2(max));

            string min = ulong.MinValue.ToString();
            Assert.Equal(ulong.MinValue, parser1(min));
            Assert.Equal(ulong.MinValue, parser2(min));

            string tooBig = (ulong.MaxValue).ToString() + "1";
            Assert.ThrowsAny<Exception>(() => parser1(tooBig));
            Assert.ThrowsAny<Exception>(() => parser2(tooBig));
        }

        [Fact]
        public void BytesTest()
        {
            var parser1 = StringInputParsers.GetStringParserFor(typeof(byte[]).FullName);
            var parser2 = StringInputParsers.GetStringParserFor(typeof(byte[]).FullName.ToShorterName());

            Assert.Equal(new byte[] { 0x10, 0x10 }, parser1("1010"));
            Assert.Equal(new byte[] { 0x10, 0x10 }, parser2("1010"));

            Assert.Equal(new byte[] { 0x10, 0x10 }, parser1("0x1010"));
            Assert.Equal(new byte[] { 0x10, 0x10 }, parser2("0x1010"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));
        }

        [Fact]
        public void HashTest()
        {
            var parser = StringInputParsers.GetStringParserFor(typeof(Kernel.Types.Hash).FullName);
            // TODO: Value has to be a fixed length
            var hash = Hash.Generate();
            var hashHex = BitConverter.ToString(hash.Value.ToByteArray()).Replace("-", "");
            Assert.Equal(hash, parser(hashHex));
            Assert.Equal(hash, parser("0x" + hashHex));

            // Lowercase
            Assert.Equal(hash, parser(hashHex.ToLower()));
            Assert.Equal(hash, parser("0x" + hashHex.ToLower()));

            var tooShort = "0x101010";
            Assert.ThrowsAny<Exception>(() => parser(tooShort));

            var tooLong = hashHex + "01";
            Assert.ThrowsAny<Exception>(() => parser(tooLong));
        }
    }
}
