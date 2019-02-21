using System.Collections.Generic;
using Xunit;
using AElf.Common;
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

            var enCode = Base58CheckEncoding.EncodePlain(data);
            enCode.ShouldNotBe(string.Empty);
            var deCode = Base58CheckEncoding.DecodePlain(enCode);
            deCode.ShouldBe(data);

            Should.Throw<System.FormatException>(() => { Base58CheckEncoding.Decode(enCode); });
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
    }
}