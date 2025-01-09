using System.Text;
using AElf.Cryptography.Keccak;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AElf.Cryptography.Tests;

public class KeccakHelperTest
{
    [Fact]
    public void Keccak256_Test()
    {
        byte[] message = Encoding.UTF8.GetBytes("Test message");

        var expectedHash = Sha3Keccack.Current.CalculateHash(message);

        var computedHash = KeccakHelper.Keccak256(message);

        computedHash.ShouldBe(expectedHash);
    }
}