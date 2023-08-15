using NBitcoin.DataEncoders;
using Shouldly;

namespace AElf.Runtime.WebAssembly.Tests;

public class WebAssemblyRuntimeTests : WebAssemblyRuntimeTestBase
{
    [Fact]
    public void SealReturnWithSuccessStatus()
    {
        const string watFilePath = "watFiles/code_return_with_data.wat";
        var runtime = new Runtime(new ExternalEnvironment(), watFilePath, false, 1, 1);
        runtime.Input = Encoders.Hex.DecodeData("00000000445566778899");
        var instance = runtime.Instantiate();
        InvokeCall(instance.GetAction("call"));
        var hexReturn = Convert.ToHexString(runtime.ReturnBuffer);
        hexReturn.ShouldBe("445566778899");
    }

    [Fact]
    public void DebugMessageWorks()
    {
        const string watFilePath = "watFiles/code_debug_message.wat";
        var runtime = new Runtime(new ExternalEnvironment(), watFilePath, false, 1, 1);
        var instance = runtime.Instantiate();
        InvokeCall(instance.GetAction("call"));
        runtime.DebugMessages.Count.ShouldBe(1);
        runtime.DebugMessages.First().ShouldBe("Hello World!");
    }

    [Fact]
    public void GetStorageWorks()
    {
        const string watFilePath = "watFiles/get_storage_works.wat";
        var context = new ExternalEnvironment();
        var runtime = new Runtime(context, watFilePath, false, 1, 1);

        // value does not exist
        {
            runtime.Input = new byte[68];
            runtime.Input[0] = 63;
            for (var i = 0; i < 64; i++)
            {
                runtime.Input[i + 4] = 1;
            }

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.KeyNotFound);
        }

        // value exists
        {
            var key = new byte[64];
            for (var i = 0; i < 64; i++)
            {
                key[i] = 1;
            }

            runtime.SetStorage(key, new byte[] { 42 }, false);
            runtime.Input = ConstructInput(64, ConstructByteArray(64, 1));

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.Success);
        }

        // value exists (test for 0 sized)
        {
            var key = ConstructByteArray(19, 2);

            runtime.SetStorage(key, new byte[] { }, false);
            runtime.Input = ConstructInput(19, ConstructByteArray(19, 2));
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.Success);
        }
    }

    private byte[] ConstructInput(byte length, byte[] inputKey)
    {
        var input = new byte[length + 4];
        input[0] = length;
        for (var i = 0; i < length; i++)
        {
            input[i + 4] = inputKey[i];
        }

        return input;
    }

    private byte[] ConstructByteArray(int length, byte singleByte)
    {
        var result = new byte[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = singleByte;
        }

        return result;
    }

    private void InvokeCall(Action? call)
    {
        try
        {
            call?.Invoke();
        }
        catch (Exception)
        {
            // ignored
        }
    }
}