using NBitcoin.DataEncoders;
using Shouldly;

namespace AElf.Runtime.WebAssembly.Tests;

public class WebAssemblyRuntimeTests : WebAssemblyRuntimeTestBase
{
    [Fact]
    public void ContractTransferTest()
    {
        const string watFilePath = "watFiles/contract_transfer.wat";
        var externalEnvironment = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(externalEnvironment, watFilePath, false, 1, 1);
        var instance = runtime.Instantiate();
        InvokeCall(instance.GetAction("call"));
        externalEnvironment.Transfers.Count.ShouldBe(1);
        externalEnvironment.Transfers[0].Value.ShouldBe(153);
        externalEnvironment.Transfers[0].To.ShouldBe(WebAssemblyRuntimeTestConstants.Alice);
    }

    [Fact]
    public void ContractCallTest()
    {
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/contract_call.wat");
        externalEnvironment.Calls.Count.ShouldBe(1);
        externalEnvironment.Calls[0].To.ShouldBe(WebAssemblyRuntimeTestConstants.Alice);
        externalEnvironment.Calls[0].Value.ShouldBe(6);
        externalEnvironment.Calls[0].Data.ShouldBe(new byte[] { 1, 2, 3, 4 });
    }

    [Fact]
    public void ContractDelegateCallTest()
    {
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/contract_delegate_call.wat");
        externalEnvironment.DelegateCalls.Count.ShouldBe(1);
        externalEnvironment.DelegateCalls[0].CodeHash.Value.ShouldAllBe(b => b == 0x11);
        externalEnvironment.DelegateCalls[0].Data.ShouldBe(new byte[] { 1, 2, 3, 4 });
    }

    [Fact]
    public void ContractCallInputForwardTest()
    {
        var input = new byte[] { 0xff, 0x2a, 0x99, 0x88 };
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/contract_call_input_forward.wat",
            input);
        externalEnvironment.DebugMessages.Count.ShouldBe(1);
        externalEnvironment.DebugMessages[0].ShouldContain("InputForwarded");
        externalEnvironment.Calls.Count.ShouldBe(1);
        externalEnvironment.Calls[0].Data.ShouldBe(input);
        externalEnvironment.Calls[0].To.ShouldBe(WebAssemblyRuntimeTestConstants.Alice);
        externalEnvironment.Calls[0].Value.ShouldBe(0x2a);
        externalEnvironment.Calls[0].AllowReentry.ShouldBeFalse();
    }

    [Fact]
    public void ContractCallCloneInputTest()
    {
        var input = new byte[] { 0xff, 0x2a, 0x99, 0x88 };
        var (externalEnvironment, runtime) = ExecuteWatFile("watFiles/contract_call_clone_input.wat",
            input);
        runtime.ReturnBuffer.ShouldBe(input);
        externalEnvironment.Calls.Count.ShouldBe(1);
        externalEnvironment.Calls[0].Data.ShouldBe(input);
        externalEnvironment.Calls[0].To.ShouldBe(WebAssemblyRuntimeTestConstants.Alice);
        externalEnvironment.Calls[0].Value.ShouldBe(0x2a);
        externalEnvironment.Calls[0].AllowReentry.ShouldBeTrue();
    }

    [Fact]
    public void ContractCallTailCallTest()
    {
        var input = new byte[] { 0xff, 0x2a, 0x99, 0x88 };
        var (externalEnvironment, runtime) = ExecuteWatFile("watFiles/contract_call_tail_call.wat",
            input);
        runtime.ReturnBuffer.ShouldBe(WebAssemblyRuntimeTestConstants.CallReturnData);
        externalEnvironment.Calls.Count.ShouldBe(1);
        externalEnvironment.Calls[0].Data.ShouldBe(input);
        externalEnvironment.Calls[0].To.ShouldBe(WebAssemblyRuntimeTestConstants.Alice);
        externalEnvironment.Calls[0].Value.ShouldBe(0x2a);
        externalEnvironment.Calls[0].AllowReentry.ShouldBeFalse();
    }

    private (UnitTestExternalEnvironment, WebAssemblyRuntime) ExecuteWatFile(string watFilePath, byte[]? input = null)
    {
        var externalEnvironment = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(externalEnvironment, watFilePath, false, 1, 1);
        if (input != null)
        {
            runtime.Input = input;
        }
        var instance = runtime.Instantiate();
        InvokeCall(instance.GetAction("call"));
        return (externalEnvironment, runtime);
    }

    [Fact]
    public void SealReturnWithSuccessStatus()
    {
        const string watFilePath = "watFiles/code_return_with_data.wat";
        var runtime = new WebAssemblyRuntime(new UnitTestExternalEnvironment(), watFilePath, false, 1, 1);
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
        var runtime = new WebAssemblyRuntime(new UnitTestExternalEnvironment(), watFilePath, false, 1, 1);
        var instance = runtime.Instantiate();
        InvokeCall(instance.GetAction("call"));
        runtime.DebugMessages.Count.ShouldBe(1);
        runtime.DebugMessages.First().ShouldBe("Hello World!");
    }

    [Fact]
    public void GetStorageWorks()
    {
        const string watFilePath = "watFiles/get_storage_works.wat";
        var context = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(context, watFilePath, false, 1, 1);

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
            runtime.Input = ConstructKeyWithLengthInput(64, ConstructByteArray(64, 1));

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.Success);
        }

        // value exists (test for 0 sized)
        {
            var key = ConstructByteArray(19, 2);

            runtime.SetStorage(key, new byte[] { }, false);
            runtime.Input = ConstructKeyWithLengthInput(19, ConstructByteArray(19, 2));
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.Success);
        }
    }

    [Fact]
    public async Task SetStorageWorks()
    {
        const string watFilePath = "watFiles/set_storage_works.wat";
        var context = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(context, watFilePath, false, 1, 1);

        // value did not exist before -> sentinel returned
        {
            var key = ConstructByteArray(32, 1);
            var keyWithLength = ConstructKeyWithLengthInput(32, key);

            var input = new byte[38];
            Array.Copy(keyWithLength, input, 36);
            input[36] = 42;
            input[37] = 48;
            runtime.Input = input;

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            var hexReturn = runtime.ReturnBuffer.ToHex();
            hexReturn.ShouldBe("00000000");

            var value = await context.GetStorageAsync(key);
            value.ShouldNotBeNull();
            value[0].ShouldBe((byte)42);
            value[1].ShouldBe((byte)48);
        }

        // value do exist -> length of old value returned
        {
            var key = ConstructByteArray(32, 1);
            var keyWithLength = ConstructKeyWithLengthInput(32, key);

            var input = new byte[37];
            Array.Copy(keyWithLength, input, 36);
            input[36] = 0;
            runtime.Input = input;

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            var hexReturn = runtime.ReturnBuffer.ToHex();
            hexReturn.ShouldBe("02000000");

            var value = await context.GetStorageAsync(key);
            value.ShouldNotBeNull();
            value[0].ShouldBe((byte)0);
        }

        // value do exist -> length of old value returned (test for zero sized val)
        {
            var key = ConstructByteArray(32, 1);
            var keyWithLength = ConstructKeyWithLengthInput(32, key);

            var input = new byte[37];
            Array.Copy(keyWithLength, input, 36);
            input[36] = 99;
            runtime.Input = input;

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            var hexReturn = runtime.ReturnBuffer.ToHex();
            hexReturn.ShouldBe("00000000");

            var value = await context.GetStorageAsync(key);
            value.ShouldNotBeNull();
            value[0].ShouldBe((byte)99);
        }
    }

    private byte[] ConstructKeyWithLengthInput(byte length, byte[] inputKey)
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