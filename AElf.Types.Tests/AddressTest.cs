using System;
using AElf.Common;
using AElf.Cryptography;
using Shouldly;
using Xunit;

namespace AElf.Types.Tests
{
    public class AddressTest
    {
        [Fact]
        public void Generate_Address()
        {
            //Generate default
            var address1 = Address.Generate();
            var address2 = Address.Generate();
            address1.ShouldNotBeSameAs(address2);

            //Generate from String
            var address3 = Address.FromString("Test");
            address3.ShouldNotBe(null);

            //Generate from byte
            var bytes = new byte[30];
            new Random().NextBytes(bytes);
            var address4 = Address.FromBytes(bytes);
            address4.ShouldNotBe(null);
            
            bytes = new byte[10];
            Should.Throw<ArgumentOutOfRangeException>(() => {Address.FromBytes(bytes); });

            //Generate from public key
            var pk = CryptoHelpers.GenerateKeyPair().PublicKey;
            var address5 = Address.FromPublicKey(pk);
            address5.ShouldNotBe(null);
        }

        [Fact]
        public void Get_Address_Info()
        {
            var pk = CryptoHelpers.GenerateKeyPair().PublicKey;
            var address = Address.FromPublicKey(pk);
            var addressString = address.GetFormatted();
            addressString.ShouldNotBe(string.Empty);
        }
        
        [Fact]
        public void Compare_Address()
        {
            var address1 = Address.Generate();
            var address2 = Address.Generate();
            address1.CompareTo(address2).ShouldNotBe(0);
            Should.Throw<InvalidOperationException>(() => { address1.CompareTo(null);});

            (address1 < null).ShouldBeFalse();
            (null < address2).ShouldBeTrue();
            (address1 > address2).ShouldBe(address1.CompareTo(address2)>0);
        }

        [Fact]
        public void Parse_Address_FromString()
        {
            string addStr = "5rYq3rGiULxGS51xAYF6Una1RH2bhm3REEZdda6o5NJwvRF";
            var address = Address.Parse(addStr);
            address.ShouldNotBe(null);
            var addStr1 = address.GetFormatted();
            addStr1.ShouldBe(addStr);

            addStr = "345678icdfvbghnjmkdfvgbhtn";
            Should.Throw<FormatException>(() => { address = Address.Parse(addStr); });
        }
        
        [Fact]
        public void Chain_Address()
        {
            var address = Address.Generate();
            var chainId = ChainHelpers.GetRandomChainId();
            var chainAddress1 = new ChainAddress(address, chainId);

            string str = chainAddress1.GetFormatted();
            var chainAddress2 = ChainAddress.Parse(str);
            chainAddress1.Address.ShouldBe(chainAddress2.Address);
            chainAddress1.ChainId.ShouldBe(chainAddress2.ChainId);

            var strError = chainAddress1.ToString();
            Should.Throw<ArgumentException>(() => { chainAddress2 = ChainAddress.Parse(strError); });
        }        
    }
}