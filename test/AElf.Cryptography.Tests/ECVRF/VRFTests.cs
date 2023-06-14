using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AElf.Cryptography.ECVRF;
using Secp256k1Net;
using Xunit;

namespace AElf.Cryptography.Tests.ECVRF;

public class VRFTests
{
    public class TestVector
    {
        [JsonPropertyName("sk")]
        public string Sk { get; set; }
        
        [JsonPropertyName("pk")]
        public string Pk { get; set; }
        
        [JsonPropertyName("alpha")]
        public string Alpha { get; set; }
        
        [JsonPropertyName("beta")]
        public string Beta { get; set; }
        
        [JsonPropertyName("pi")]
        public string Pi { get; set; }
    }

    [Fact]
    public void HashToCurveTryAndIncrement_Test()
    {
        var pkSerialized = Convert.FromHexString("0360fed4ba255a9d31c961eb74c6356d68c049b8923b61fa6ce669622e60f29fb6");
        var alpha = Encoding.ASCII.GetBytes("sample");
        var expectedHashPoint = Convert.FromHexString("027AD7D4C3A454D9ECC905F1E5436A328F2A106A2606EC4B44111CF9DC72A5B9FF");
        
        using var secp256k1 = new Secp256k1();
        var cfg = new VrfConfig( 0xfe);
        var vrf = new Vrf(cfg);
        var output = vrf.HashToCurveTryAndIncrement(Point.FromSerialized(secp256k1, pkSerialized), alpha);
        var outputSerialized = output.Serialize(secp256k1, true);
        Assert.Equal(outputSerialized, expectedHashPoint);
    }

    [Fact]
    public void Prove_Test()
    {
       var path = Path.Combine( Directory.GetCurrentDirectory(), "secp256_k1_sha256_tai.json");
       var text = File.ReadAllText(path);
       var vectors = JsonSerializer.Deserialize<List<TestVector>>(text);
       using var secp256k1 = new Secp256k1();
       foreach (var vector in vectors)
       {
           var sk = AddLeadingZeros(Convert.FromHexString(vector.Sk));
           var kp = CryptoHelper.FromPrivateKey(sk);
           var alpha = Convert.FromHexString(vector.Alpha);
           var expectedPi = Convert.FromHexString(vector.Pi);
           var expectedBeta = Convert.FromHexString(vector.Beta);
           var cfg = new VrfConfig( 0xfe);
           var vrf = new Vrf(cfg);
           var proof = vrf.Prove(kp, alpha);
           Assert.Equal(expectedPi, proof.Pi);
           Assert.Equal(expectedBeta, proof.Beta);
       }
    }

    [Fact]
    public void Verify_Test()
    {
        var path = Path.Combine( Directory.GetCurrentDirectory(), "secp256_k1_sha256_tai.json");
        var text = File.ReadAllText(path);
        var vectors = JsonSerializer.Deserialize<List<TestVector>>(text);
        using var secp256k1 = new Secp256k1();
        foreach (var vector in vectors)
        {
            var pk =Convert.FromHexString(vector.Pk);
            var alpha = Convert.FromHexString(vector.Alpha);
            var pi = Convert.FromHexString(vector.Pi);
            var expectedBeta = Convert.FromHexString(vector.Beta);
            var cfg = new VrfConfig( 0xfe);
            var vrf = new Vrf(cfg);
            var beta = vrf.Verify(pk, alpha, pi);
            Assert.Equal(expectedBeta, beta);
        }
    }

    private byte[] AddLeadingZeros(byte[] sk)
    {
        if (sk.Length < 32)
        {
            var output = new byte[32];
            var zeroBytesLength = 32 - sk.Length;
            for (int i = zeroBytesLength - 1; i >= 0; i--)
            {
                output[i] = 0;
            }
            Buffer.BlockCopy(sk, 0, output, zeroBytesLength, sk.Length);
            return output;
        }

        return sk;

    }
}