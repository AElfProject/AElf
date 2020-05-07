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
            var hexValue = HashHelper.ComputeFrom("hx").ToHex();

            var hexValueWithPrefix = hexValue.AppendHexPrefix();
            hexValueWithPrefix.Substring(0, 2).ShouldBe("0x");
            var hexValueWithPrefix1 = hexValueWithPrefix.AppendHexPrefix();
            hexValueWithPrefix1.ShouldBeSameAs(hexValueWithPrefix);

            var byteArray = HashHelper.ComputeFrom("hx").ToByteArray();
            var hexString = byteArray.ToHex(true);
            hexString.Substring(0, 2).ShouldBe("0x");

            var hex = hexValueWithPrefix.RemoveHexPrefix();
            hex.ShouldBe(hexValue);
            var hex1 = hex.RemoveHexPrefix();
            hex1.ShouldBeSameAs(hex);

            var hash1 = hexValue.ComputeHash();
            hash1.ShouldNotBe(null);


            var byteArray2 = new byte[] {0, 1, 2};
            var base58Str = Base58CheckEncoding.EncodePlain(byteArray2);
            base58Str.DecodeBase58().ShouldBe(byteArray2);
        }

        [Fact]
        public void Number_Extensions_Methods_Test()
        {
            //ulong
            var uNumber = (ulong) 10;
            var byteArray = uNumber.ToBytes();
            byteArray.ShouldNotBe(null);

            //long
            var lNumber = (long) 998;
            var bytes = lNumber.ToBytes();
            bytes.ToInt64(true).ShouldBe(lNumber);

            //int
            var iNumber = 10;
            var byteArray1 = iNumber.ToBytes();
            byteArray1.ShouldNotBe(null);
            byteArray1.ToInt32(true).ShouldBe(iNumber);

            //hash
            var hash = HashHelper.ComputeFrom(iNumber);
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

            var bytes2 = new byte[] {0, 1, 2};
            bytes2.ToHex().ShouldBe("000102");
            bytes2.ToHex(true).ShouldBe("0x000102");
            bytes2.LeftPad(4).ShouldBe(new byte[] {0, 0, 1, 2});
            bytes2.Find(new byte[] {1, 2}).ShouldBe(1);
            bytes2.Find(new byte[] {1, 2, 0}).ShouldNotBe(1);


            var bytes3 = 123.ToBytes();
            bytes3.ToInt32(true).ShouldBe(123);

            var bytes4 = 998L.ToBytes();
            bytes4.ToInt64(true).ShouldBe(998L);
        }

        [Fact]
        public void ByteStringExtensions_Test()
        {
            var bs = ByteString.CopyFrom(new byte[] {0, 3, 5});
            var hex = bs.ToHex();
            hex.ToByteString().ShouldBe(bs);
        }

        [Fact]
        public void HexStringExtensions_Test()
        {
            var hexStr = HashHelper.ComputeFromString("hex").ToHex();
            var bs = hexStr.ToByteString();
            bs.ToHex().ShouldBe(hexStr);
        }

        [Fact]
        public void IMessageExtensions_Test()
        {
            var address1 = Address.FromBase58("2DZER7qHVwv3PUMFsHuQaQbE4wDFsCRzJsxLwYEk8rgM3HVn1S");
            var bytesValue = address1.ToBytesValue();
            Address.Parser.ParseFrom(bytesValue.Value).ShouldBe(address1);
        }

        [Fact]
        public void StateKeyExtensions_Test()
        {
            var key = "mykey";
            var path = "TokenContractAddress/Balances/address/symbol";
            var path1 = new StatePath
            {
                Parts = {key, path}
            };
            var address1 = Address.FromBase58("2DZER7qHVwv3PUMFsHuQaQbE4wDFsCRzJsxLwYEk8rgM3HVn1S");
            var stateKey = path1.ToStateKey(address1);
            var scopedStatePath = new ScopedStatePath()
            {
                Address = address1,
                Path = path1
            };

            var expected = $"{address1.ToBase58()}/{key}/{path}";

            stateKey.ShouldBe(expected);
            scopedStatePath.ToStateKey().ShouldBe(expected);
        }
    }
}