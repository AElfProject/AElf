using System;
using System.Linq;
using AElf.ContractTestKit;
using Nethereum.Util;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class AddressTest
{
    [Fact]
    public void Test()
    {
        var aelfECKeyPair = SampleAccount.Accounts.First().KeyPair;
        var ethECKeyPair = new Nethereum.Web3.Accounts.Account(aelfECKeyPair.PrivateKey, 1);
        aelfECKeyPair.PublicKey.ToHex().ShouldBe(ethECKeyPair.PublicKey);
        var ethAddress = ByteArrayHelper.HexStringToByteArray(ethECKeyPair.Address);
        ethAddress.Length.ShouldBe(20);

        PubkeyToEthAddress(ByteArrayHelper.HexStringToByteArray(ethECKeyPair.PublicKey)).ShouldBe(ethAddress);
    }

    private byte[] PubkeyToEthAddress(byte[] pubkey)
    {
        var pubkeyNoPrefixCompressed = new byte[pubkey.Length - 1];
        Array.Copy(pubkey, 1, pubkeyNoPrefixCompressed, 0, pubkeyNoPrefixCompressed.Length);
        var initAddress = new Sha3Keccack().CalculateHash(pubkeyNoPrefixCompressed);
        var address = new byte[initAddress.Length - 12];
        Array.Copy(initAddress, 12, address, 0, initAddress.Length - 12);
        return address;
    }
}