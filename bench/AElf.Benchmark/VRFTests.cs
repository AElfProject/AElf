using System;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark;

[MarkdownExporterAttribute.GitHub]
public class VrfTests : BenchmarkTestBase
{
    private ECKeyPair _key;
    private byte[] _alphaBytes;
    private byte[] _piBytes;
    
    [Params(1,10,100,1000)] public int VectorCount;
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        _key = CryptoHelper.FromPrivateKey(
            ByteArrayHelper.HexStringToByteArray("2eaeb403475a9962ad59302ab53fa2b668d2d24bf5a2a917707e5b2f4ded392b"));
        var alpha = "fb779e58991c424eaaf10b3d364c3b3e756c7b435109a985e547c058964d7bd5";
        _alphaBytes = Convert.FromHexString(alpha);
        var pi = "03ea6c4bdb4a9e1ae0a17c427ec074f68cdac7a57e4f3fded1b07d20dd5385baf05a9d1e4064cd1c2c5e8608e96b7e3e2058500f178b414b8e910178f17a7a77af7e88befeabceb77cae3e9fd2e1a6c051";
        _piBytes = Convert.FromHexString(pi);
    }
    
    [Benchmark]
    public void VrfProveTest()
    {
        for (var i = 0; i < VectorCount; i++)
        {
            CryptoHelper.ECVrfProve(_key, _alphaBytes);
        }
    }

    [Benchmark]
    public void VrfVerifyTest()
    {
        for (var i = 0; i < VectorCount; i++)
        {
            CryptoHelper.ECVrfVerify(_key.PublicKey, _alphaBytes, _piBytes);
        }
    }
}