namespace AElf.Sdk.CSharp.Tests;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class BuiltinsTests
{
    private List<TestObject> testData;

    public BuiltinsTests()
    {
        var json = File.ReadAllText("ed25519_testdata.json");
        testData = JsonConvert.DeserializeObject<List<TestObject>>(json);
    }

    [Fact]
    public void Ed25519Verify_Test()
    {
        foreach (var testObject in testData)
        {
            Assert.True(BuiltIns.Ed25519Verify(
                ByteArrayHelper.HexStringToByteArray(testObject.Signature),
                ByteArrayHelper.HexStringToByteArray(testObject.Message),
                ByteArrayHelper.HexStringToByteArray(testObject.PublicKey)
            ));
        }
    }
}

public class TestObject
{
    [JsonProperty("secret_key")] public string SecretKey { get; set; }
    [JsonProperty("public_key")] public string PublicKey { get; set; }
    [JsonProperty("message")] public string Message { get; set; }
    [JsonProperty("signed")] public string Signed { get; set; }
    [JsonProperty("signature")] public string Signature { get; set; }
}