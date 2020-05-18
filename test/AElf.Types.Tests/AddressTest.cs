﻿using System.Linq;
using System;
using System.Collections.Generic;
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
            var address1 = Address.FromBase58("2DZER7qHVwv3PUMFsHuQaQbE4wDFsCRzJsxLwYEk8rgM3HVn1S");
            var address2 = Address.FromBase58("xFqJD9R33mQBQPr1hCFUZMayXFQ577j34MPyUdXzbPpAYufG2");
            address1.ShouldNotBeSameAs(address2);

            //Generate from String
            var address3 = Address.FromBase58("z1NVbziJbekvcza3Zr4Gt4eAvoPBZThB68LHRQftrVFwjtGVM");
            address3.ShouldNotBe(null);

            //Generate from byte
            var bytes = Enumerable.Repeat((byte) 0xEF, 32).ToArray();
            var address4 = Address.FromBytes(bytes);
            address4.ShouldNotBe(null);

            bytes = Enumerable.Repeat((byte) 32, 20).ToArray();
            Should.Throw<ArgumentException>(() => { Address.FromBytes(bytes); });

            //Generate from public key
            var pk = CryptoHelper.GenerateKeyPair().PublicKey;
            var address5 = Address.FromPublicKey(pk);
            address5.ShouldNotBe(null);
            address5.ToByteArray().Length.ShouldBe(32);
        }

        [Fact]
        public void Get_Address_Info()
        {
            var pk = CryptoHelper.GenerateKeyPair().PublicKey;
            var address = Address.FromPublicKey(pk);
            var addressString = address.ToBase58();
            addressString.ShouldNotBe(string.Empty);
        }

        [Fact]
        public void Compare_Address()
        {
            var address1 = Address.FromBase58("z1NVbziJbekvcza3Zr4Gt4eAvoPBZThB68LHRQftrVFwjtGVM");
            var address2 = Address.FromBase58("nGmKp2ekysABSZAzVfXDrmaTNTaSSrfNmDhuaz7RUj5RTCYqy");
            address1.CompareTo(address2).ShouldNotBe(0);
            Should.Throw<InvalidOperationException>(() => { address1.CompareTo(null); });

            (address1 < null).ShouldBeFalse();
            (null < address2).ShouldBeTrue();
            (address1 > address2).ShouldBe(address1.CompareTo(address2) > 0);

            Address addressA = null;
            Address addressB = null;
            var value = addressA > addressB;
            value.ShouldBeFalse();
        }

        [Fact]
        public void Parse_Address_FromString()
        {
            string addStr = "ddnF1dEsp51QbASCqQKPZ7vs2zXxUxyu5BuGRKFQAsT9JKrra";
            var address = Address.FromBase58(addStr);
            address.ShouldNotBe(null);
            var addStr1 = address.ToBase58();
            addStr1.ShouldBe(addStr);

            addStr = "345678icdfvbghnjmkdfvgbhtn";
            Should.Throw<FormatException>(() => { address = Address.FromBase58(addStr); });
        }

        [Fact]
        public void Chain_Address()
        {
            var address = Address.FromBase58("nGmKp2ekysABSZAzVfXDrmaTNTaSSrfNmDhuaz7RUj5RTCYqy");
            var chainId = 0;
            var chainAddress1 = new ChainAddress(address, chainId);

            string str = chainAddress1.GetFormatted("ELF", chainAddress1.ChainId);
            var chainAddress2 = ChainAddress.Parse(str, "ELF");
            chainAddress1.Address.ShouldBe(chainAddress2.Address);
            chainAddress1.ChainId.ShouldBe(chainAddress2.ChainId);

            var strError = chainAddress1.ToString();
            Should.Throw<ArgumentException>(() => { chainAddress2 = ChainAddress.Parse(strError, "ELF"); });
        }

        [Fact]
        public void Verify_Address()
        {
            var address = Address.FromBase58("nGmKp2ekysABSZAzVfXDrmaTNTaSSrfNmDhuaz7RUj5RTCYqy");
            var formattedAddress = address.ToBase58();
            AddressHelper.VerifyFormattedAddress(formattedAddress).ShouldBeTrue();

            AddressHelper.VerifyFormattedAddress(formattedAddress + "ER").ShouldBeFalse();
            AddressHelper.VerifyFormattedAddress("AE" + formattedAddress).ShouldBeFalse();

            var formattedAddressCharArray = formattedAddress.ToCharArray();
            formattedAddressCharArray[4] = 'F';
            AddressHelper.VerifyFormattedAddress(new string(formattedAddressCharArray)).ShouldBeFalse();

            AddressHelper.VerifyFormattedAddress("").ShouldBeFalse();
            AddressHelper.VerifyFormattedAddress("I0I0").ShouldBeFalse();
        }
    }
}