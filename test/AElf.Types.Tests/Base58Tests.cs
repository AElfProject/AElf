﻿using System;
using Xunit;
using Google.Protobuf;
using Shouldly;

namespace AElf.Types.Tests
{
    public class Base58Tests
    {
        [Fact]
        public void Encode_And_Decode_Hash()
        {
            var hash = Hash.Generate();
            var data = hash.DumpByteArray();

            var enCode = Base58CheckEncoding.Encode(data);
            enCode.ShouldNotBe(string.Empty);
            var deCode = Base58CheckEncoding.Decode(enCode);
            deCode.ShouldBe(data);
        }

        [Fact]
        public void Encode_And_Decode_Address()
        {
            var address = Address.Generate();
            var data = address.DumpByteArray();

            var enCode = Base58CheckEncoding.Encode(data);
            enCode.ShouldNotBe(string.Empty);
            var deCode = Base58CheckEncoding.Decode(enCode);
            deCode.ShouldBe(data);

            var deCode1 = Base58CheckEncoding.DecodePlain(enCode);
            deCode1.ShouldNotBe(data);
        }

        [Fact]
        public void EncodePlain_And_DecodePlain_Hash()
        {
            var hash = Hash.Generate();
            var data = hash.DumpByteArray();
            var bytes = new byte[]{0};

            var enCode = Base58CheckEncoding.EncodePlain(data);
            enCode.ShouldNotBe(string.Empty);
            var deCode = Base58CheckEncoding.DecodePlain(enCode);
            deCode.ShouldBe(data);
            
            Base58CheckEncoding.EncodePlain(bytes).ShouldBe("1");
            Should.Throw<FormatException>(() => {
                Base58CheckEncoding.DecodePlain(bytes.ToString());
            });

            Should.Throw<FormatException>(() => { Base58CheckEncoding.Decode(enCode); });
        }

        [Fact]
        public void EncodePlain_And_DecodePlain_Address()
        {
            var address = Address.Generate();
            var data = address.DumpByteArray();

            var enCode = Base58CheckEncoding.EncodePlain(data);
            enCode.ShouldNotBe(string.Empty);
            var deCode = Base58CheckEncoding.DecodePlain(enCode);
            deCode.ShouldBe(data);
        }

        [Fact]
        public void EncodePlan_And_DecodePlain_ByteString()
        {
            var bs = ByteString.CopyFromUtf8("test byte string");
            var encodeMessage = Base58CheckEncoding.EncodePlain(bs);

            var decodeMessage = Base58CheckEncoding.DecodePlain(encodeMessage);
            var bs1 = ByteString.CopyFrom(decodeMessage);
            bs.ShouldBe(bs1);
        }
    }
}