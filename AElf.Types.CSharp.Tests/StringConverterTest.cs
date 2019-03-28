﻿using System;
using Xunit;

namespace AElf.Types.CSharp.Tests
{
    public class StringInputParsersTest
    {
        [Fact]
        public void BoolTest()
        {
            var parser1 = StringConverter.GetTypeParser(typeof(bool).FullName);
            var parser2 = StringConverter.GetTypeParser(typeof(bool).FullName.ToShorterName());
            Assert.True((bool) parser1("true"));
            Assert.True((bool) parser2("true"));
            Assert.False((bool) parser1("false"));
            Assert.False((bool) parser2("false"));
            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));
        }

        [Fact]
        public void BytesTest()
        {
            var parser1 = StringConverter.GetTypeParser(typeof(byte[]).FullName);
            var parser2 = StringConverter.GetTypeParser(typeof(byte[]).FullName.ToShorterName());

            Assert.Equal(new byte[] {0x10, 0x10}, parser1("1010"));
            Assert.Equal(new byte[] {0x10, 0x10}, parser2("1010"));

            Assert.Equal(new byte[] {0x10, 0x10}, parser1("0x1010"));
            Assert.Equal(new byte[] {0x10, 0x10}, parser2("0x1010"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));
        }

        [Fact]
        public void Int64Test()
        {
            var parser1 = StringConverter.GetTypeParser(typeof(long).FullName);
            var parser2 = StringConverter.GetTypeParser(typeof(long).FullName.ToShorterName());
            Assert.Equal((long) 10, parser1("10"));
            Assert.Equal((long) 10, parser2("10"));
            Assert.Equal((long) 16, parser1("0x10"));
            Assert.Equal((long) 16, parser2("0x10"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));

            var max = long.MaxValue.ToString();
            Assert.Equal(long.MaxValue, parser1(max));
            Assert.Equal(long.MaxValue, parser2(max));

            var min = long.MinValue.ToString();
            Assert.Equal(long.MinValue, parser1(min));
            Assert.Equal(long.MinValue, parser2(min));

            var tooBig = long.MaxValue + "1";
            Assert.ThrowsAny<Exception>(() => parser1(tooBig));
            Assert.ThrowsAny<Exception>(() => parser2(tooBig));
        }

        [Fact]
        public void IntTest()
        {
            var parser1 = StringConverter.GetTypeParser(typeof(int).FullName);
            var parser2 = StringConverter.GetTypeParser(typeof(int).FullName.ToShorterName());
            Assert.Equal(10, parser1("10"));
            Assert.Equal(10, parser2("10"));
            Assert.Equal(16, parser1("0x10"));
            Assert.Equal(16, parser2("0x10"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));

            var max = int.MaxValue.ToString();
            Assert.Equal(int.MaxValue, parser1(max));
            Assert.Equal(int.MaxValue, parser2(max));

            var min = int.MinValue.ToString();
            Assert.Equal(int.MinValue, parser1(min));
            Assert.Equal(int.MinValue, parser2(min));

            var tooBig = ((long) int.MaxValue + 1).ToString();
            Assert.ThrowsAny<Exception>(() => parser1(tooBig));
            Assert.ThrowsAny<Exception>(() => parser2(tooBig));
        }

        [Fact]
        public void UInt64Test()
        {
            var parser1 = StringConverter.GetTypeParser(typeof(ulong).FullName);
            var parser2 = StringConverter.GetTypeParser(typeof(ulong).FullName.ToShorterName());
            Assert.Equal((ulong) 10, parser1("10"));
            Assert.Equal((ulong) 10, parser2("10"));
            Assert.Equal((ulong) 16, parser1("0x10"));
            Assert.Equal((ulong) 16, parser2("0x10"));

            Assert.ThrowsAny<Exception>(() => parser1("-1"));
            Assert.ThrowsAny<Exception>(() => parser2("-1"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));

            var max = ulong.MaxValue.ToString();
            Assert.Equal(ulong.MaxValue, parser1(max));
            Assert.Equal(ulong.MaxValue, parser2(max));

            var min = ulong.MinValue.ToString();
            Assert.Equal(ulong.MinValue, parser1(min));
            Assert.Equal(ulong.MinValue, parser2(min));

            var tooBig = ulong.MaxValue + "1";
            Assert.ThrowsAny<Exception>(() => parser1(tooBig));
            Assert.ThrowsAny<Exception>(() => parser2(tooBig));
        }

        [Fact]
        public void UIntTest()
        {
            var parser1 = StringConverter.GetTypeParser(typeof(uint).FullName);
            var parser2 = StringConverter.GetTypeParser(typeof(uint).FullName.ToShorterName());
            Assert.Equal((uint) 10, parser1("10"));
            Assert.Equal((uint) 10, parser2("10"));
            Assert.Equal((uint) 16, parser1("0x10"));
            Assert.Equal((uint) 16, parser2("0x10"));

            Assert.ThrowsAny<Exception>(() => parser1("-1"));
            Assert.ThrowsAny<Exception>(() => parser2("-1"));

            Assert.ThrowsAny<Exception>(() => parser1("invalid input"));
            Assert.ThrowsAny<Exception>(() => parser2("invalid input"));

            var max = uint.MaxValue.ToString();
            Assert.Equal(uint.MaxValue, parser1(max));
            Assert.Equal(uint.MaxValue, parser2(max));

            var min = uint.MinValue.ToString();
            Assert.Equal(uint.MinValue, parser1(min));
            Assert.Equal(uint.MinValue, parser2(min));

            var tooBig = ((long) uint.MaxValue + 1).ToString();
            Assert.ThrowsAny<Exception>(() => parser1(tooBig));
            Assert.ThrowsAny<Exception>(() => parser2(tooBig));
        }

        //todo warning adr
//        [Fact]
//        public void HashTest()
//        {
//            var parser = StringInputParsers.GetStringParserFor(typeof(Address).FullName);
//            // TODO: Value has to be a fixed length
//            var hash = Address.FromRawBytes(Hash.Generate().ToByteArray());
//            var hashHex = BitConverter.ToString(hash.DumpByteArray()).Replace("-", "");
//            
//            // Note: Hash has the same structure as BytesValue, hence using BytesValue for serialization.
//            // So that we don't need dependency AElf.Kernel.
//            Assert.Equal(hash.ToByteArray(), ((IMessage)parser(hashHex)).ToByteArray());
//            Assert.Equal(hash.ToByteArray(), ((IMessage)parser("0x" + hashHex)).ToByteArray());
//
//            // Lowercase
//            Assert.Equal(hash.ToByteArray(), ((IMessage)parser(hashHex.ToLower())).ToByteArray());
//            Assert.Equal(hash.ToByteArray(), ((IMessage)parser("0x" + hashHex.ToLower())).ToByteArray());
//
//            var tooShort = "0x101010";
//            Assert.ThrowsAny<Exception>(() => parser(tooShort));
//
//            var tooLong = hashHex + "01";
//            Assert.ThrowsAny<Exception>(() => parser(tooLong));
//        }
    }
}