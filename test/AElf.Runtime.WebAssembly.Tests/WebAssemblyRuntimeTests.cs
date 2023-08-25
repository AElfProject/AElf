using AElf.Types;
using Google.Protobuf;
using NBitcoin.DataEncoders;
using Shouldly;
using Wasmtime;

namespace AElf.Runtime.WebAssembly.Tests;

public class WebAssemblyRuntimeTests : WebAssemblyRuntimeTestBase
{
    private List<string> _runtimeErrors = new List<string>();

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

        _runtimeErrors[0].ShouldContain("InputForwarded");
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

    [Fact]
    public async Task ContainsStorageWorks()
    {
        const string watFilePath = "watFiles/contains_storage_works.wat";
        var byteArrayBuilder = new ByteArrayBuilder();
        var externalEnvironment = new UnitTestExternalEnvironment();
        externalEnvironment.SetStorage(byteArrayBuilder.RepeatedBytes(1, 64), new byte[] { 42 }, false);
        externalEnvironment.SetStorage(byteArrayBuilder.RepeatedBytes(2, 19), Array.Empty<byte>(), false);

        var runtime = new WebAssemblyRuntime(externalEnvironment, watFilePath, false, 1, 1);

        //value does not exist (wrong key length)
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 64);
            var keyWithLength = ConstructKeyWithLengthInput(63, key);
            runtime.Input = keyWithLength;
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            runtime.ReturnBuffer.ToInt32(false).ShouldBe(int.MaxValue);
        }

        // value exists
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 64);
            var keyWithLength = ConstructKeyWithLengthInput(64, key);
            runtime.Input = keyWithLength;
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            runtime.ReturnBuffer.ToInt32(false).ShouldBe(1);
            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldBe(new byte[] { 42 });
        }

        // value exists (test for 0 sized)
        {
            var key = byteArrayBuilder.RepeatedBytes(2, 19);
            var keyWithLength = ConstructKeyWithLengthInput(19, key);
            runtime.Input = keyWithLength;
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            runtime.ReturnBuffer.ToInt32(false).ShouldBe(0);
            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldBe(Array.Empty<byte>());
        }
    }

    [Fact]
    public void ContractInstantiateTest()
    {
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/contract_instantiate.wat");
        externalEnvironment.Instantiates.Count.ShouldBe(1);
        externalEnvironment.Instantiates[0].CodeHash.ShouldBe(new Hash
        {
            Value = ByteString.CopyFrom(new ByteArrayBuilder().RepeatedBytes(0x11, 32))
        });
        externalEnvironment.Instantiates[0].Data.ShouldBe(new byte[] { 1, 2, 3, 4 });
        externalEnvironment.Instantiates[0].Salt.ShouldBe(new byte[] { 0x42, 0x43, 0x44, 0x45 });
        externalEnvironment.Instantiates[0].Value.ShouldBe(3);
    }

    [Fact]
    public void ContractTerminateTest()
    {
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/contract_terminate.wat");
        externalEnvironment.Terminations.Count.ShouldBe(1);
        externalEnvironment.Terminations[0].Beneficiary.ShouldBe(WebAssemblyRuntimeTestConstants.Alice);
    }

    [Fact]
    public void ContractCallLimitedGasTest()
    {
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/contract_call_limited_gas.wat");
        externalEnvironment.Calls.Count.ShouldBe(1);
        externalEnvironment.Calls[0].To.ShouldBe(WebAssemblyRuntimeTestConstants.Alice);
        externalEnvironment.Calls[0].Value.ShouldBe(6);
        externalEnvironment.Calls[0].Data.ShouldBe(new byte[] { 1, 2, 3, 4 });
        externalEnvironment.Calls[0].AllowReentry.ShouldBeTrue();
    }

    [Fact]
    public void ContractEcdsaRecoverTest()
    {
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/contract_ecdsa_recover.wat");
        externalEnvironment.EcdsaRecover.Item1.ShouldBe(new ByteArrayBuilder().RepeatedBytes(1, 65));
        externalEnvironment.EcdsaRecover.Item2.ShouldBe(new ByteArrayBuilder().RepeatedBytes(1, 32));
    }

    [Fact]
    public void ContractEcdsaToEthAddressTest()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/contract_ecdsa_to_eth_address.wat");
        runtime.ReturnBuffer.ShouldBe(new ByteArrayBuilder().RepeatedBytes(0x02, 20));
    }

    [Fact]
    public void ContractSr25519Test()
    {
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/contract_sr25519.wat");
        externalEnvironment.Sr25519Verify.Item1.ShouldBe(new ByteArrayBuilder().RepeatedBytes(1, 64));
        externalEnvironment.Sr25519Verify.Item2.ShouldBe(new ByteArrayBuilder().RepeatedBytes(1, 16));
        externalEnvironment.Sr25519Verify.Item3.ShouldBe(new ByteArrayBuilder().RepeatedBytes(1, 32));
    }

    [Fact]
    public void GetStoragePutsDataIntoBufTest()
    {
        const string watFilePath = "watFiles/get_storage_puts_data_into_buf.wat";

        var byteArrayBuilder = new ByteArrayBuilder();
        var externalEnvironment = new UnitTestExternalEnvironment();
        externalEnvironment.SetStorage(byteArrayBuilder.RepeatedBytes(0x11, 32),
            byteArrayBuilder.RepeatedBytes(0x22, 32), false);

        var runtime = new WebAssemblyRuntime(externalEnvironment, watFilePath, false, 1, 1);
        var instance = runtime.Instantiate();
        InvokeCall(instance.GetFunction<ActionResult>("call"));

        runtime.ReturnBuffer.ShouldBe(byteArrayBuilder.RepeatedBytes(0x22, 32));
    }

    [Fact]
    public void CallerTest()
    {
        ExecuteWatFile("watFiles/caller.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
    }

    [Fact]
    public void CallerTrapsWhenNoAccountIdTest()
    {

    }

    [Fact]
    public void AddressTest()
    {

    }

    [Fact]
    public void BalanceTest()
    {

    }

    [Fact]
    public void GasPriceTest()
    {

    }

    [Fact]
    public void GasLeftTest()
    {

    }

    [Fact]
    public void ValueTransferredTest()
    {

    }

    [Fact]
    public void StartFunctionIllegalTest()
    {

    }

    [Fact]
    public void NowTest()
    {

    }

    [Fact]
    public void MinimumBalanceTest()
    {

    }

    [Fact]
    public void RandomTest()
    {

    }

    [Fact]
    public void RandomTestV1()
    {

    }

    [Fact]
    public void DepositEventTest()
    {

    }

    [Fact]
    public void DepositEventMaxTopicsTest()
    {

    }

    [Fact]
    public void BlockNumberTest()
    {

    }

    [Fact]
    public void SealReturnWithSuccessStatusTest()
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
    public void ReturnWithRevertStatusTest()
    {

    }

    [Fact]
    public void ContractOutOfBoundsAccessTest()
    {

    }

    [Fact]
    public void ContractDecodeLengthIgnoredTest()
    {

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
    public void DebugMessageInvalidUtf8FailsTest()
    {

    }

    [Fact]
    public void CallRuntimeWorks()
    {

    }

    [Fact]
    public void CallRuntimePanicsOnInvalidCallTest()
    {

    }

    [Fact]
    public async Task SetStorageWorks()
    {
        const string watFilePath = "watFiles/set_storage_works.wat";
        var externalEnvironment = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(externalEnvironment, watFilePath, false, 1, 1);

        var byteArrayBuilder = new ByteArrayBuilder();
        // value did not exist before -> sentinel returned
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 32);
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

            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldNotBeNull();
            value[0].ShouldBe((byte)42);
            value[1].ShouldBe((byte)48);
        }

        // value do exist -> length of old value returned
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 32);
            var keyWithLength = ConstructKeyWithLengthInput(32, key);

            var input = new byte[37];
            Array.Copy(keyWithLength, input, 36);
            input[36] = 0;
            runtime.Input = input;

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            var hexReturn = runtime.ReturnBuffer.ToHex();
            hexReturn.ShouldBe("02000000");

            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldNotBeNull();
            value[0].ShouldBe((byte)0);
        }

        // value do exist -> length of old value returned (test for zero sized val)
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 32);
            var keyWithLength = ConstructKeyWithLengthInput(32, key);

            var input = new byte[37];
            Array.Copy(keyWithLength, input, 36);
            input[36] = 99;
            runtime.Input = input;

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            var hexReturn = runtime.ReturnBuffer.ToHex();
            hexReturn.ShouldBe("00000000");

            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldNotBeNull();
            value[0].ShouldBe((byte)99);
        }
    }

    [Fact]
    public void GetStorageWorks()
    {
        const string watFilePath = "watFiles/get_storage_works.wat";
        var context = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(context, watFilePath, false, 1, 1);

        var byteArrayBuilder = new ByteArrayBuilder();
        // value does not exist
        {
            runtime.Input = new byte[68];
            runtime.Input[0] = 63;
            var key = byteArrayBuilder.RepeatedBytes(1, 64);
            Array.Copy(key, 0, runtime.Input, 4, 64);

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.KeyNotFound);
        }

        // value exists
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 64);

            runtime.SetStorage(key, new byte[] { 42 }, false);
            runtime.Input = ConstructKeyWithLengthInput(64, byteArrayBuilder.RepeatedBytes(1, 64));

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.Success);
        }

        // value exists (test for 0 sized)
        {
            var key = byteArrayBuilder.RepeatedBytes(2, 19);

            runtime.SetStorage(key, new byte[] { }, false);
            runtime.Input = ConstructKeyWithLengthInput(19, byteArrayBuilder.RepeatedBytes(2, 19));
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.Success);
        }
    }

    [Fact]
    public void ClearStorageWorks()
    {

    }

    [Fact]
    public void TakeStorageWorks()
    {

    }

    [Fact]
    public void IsContractWorks()
    {

    }

    [Fact]
    public void CodeHashWorks()
    {

    }

    [Fact]
    public void OWnCodeHashWorks()
    {

    }

    [Fact]
    public void CallerIsOriginWorks()
    {

    }

    [Fact]
    public void CallerIsRootWorks()
    {

    }

    [Fact]
    public void SetCodeHashTest()
    {

    }

    [Fact]
    public void ReentranceCountWorks()
    {

    }

    [Fact]
    public void AccountReentranceCountWorks()
    {

    }

    [Fact]
    public void InstantiationNonceWorks()
    {

    }

    [Fact]
    public void CannotDeployUnstableTest()
    {

    }

    [Fact]
    public void CannotDeployDeprecatedTest()
    {

    }

    [Fact]
    public void AddRemoveDelegateDependencyTest()
    {

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
        InvokeCall(instance.GetFunction<ActionResult>("call"));
        return (externalEnvironment, runtime);
    }

    private byte[] ConstructKeyWithLengthInput(byte length, byte[] inputKey)
    {
        var input = new byte[length + 4];
        input[0] = length;
        Array.Copy(inputKey, 0, input, 4, length);

        return input;
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

    private void InvokeCall(Func<ActionResult>? call)
    {
        try
        {
            var result = call?.Invoke();
            if (result == null)
            {
                throw new WebAssemblyRuntimeException("Failed to invoke call.");
            }
            if (result.Value.Trap.Message.Contains("wasm `unreachable` instruction executed") &&
                result.Value.Trap.Frames?.Count == 1)
            {
                // Ignore.
            }
            else
            {
                _runtimeErrors.Add(result.Value.Trap.ToString());
            }
        }
        catch (Exception e)
        {
            _runtimeErrors.Add(e.ToString());
        }
    }
}