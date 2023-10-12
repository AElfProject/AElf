using System.Security.Cryptography;
using AElf.Types;
using Blake2Fast;
using Google.Protobuf;
using Nethereum.Util;
using Shouldly;
using Wasmtime;

namespace AElf.Runtime.WebAssembly.Tests;

public class WebAssemblyRuntimeTests : WebAssemblyRuntimeTestBase
{
    private readonly List<string> _runtimeErrors = new();

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
        InvokeCall(runtime.Instantiate().GetFunction<ActionResult>("call"));

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
        var externalEnvironment = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(externalEnvironment, "watFiles/caller.wat", false, 1, 1);
        externalEnvironment.Caller = null;
        var invokeResult = InvokeCall(runtime.Instantiate().GetFunction<ActionResult>("call"));
        invokeResult.Success.ShouldBeFalse();
        invokeResult.DebugMessage.ShouldContain(DispatchError.RootNotAllowed.ToString());
        _runtimeErrors[0].ShouldContain(DispatchError.RootNotAllowed.ToString());
    }

    [Fact]
    public void AddressTest()
    {
        ExecuteWatFile("watFiles/address.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
    }

    [Fact]
    public void BalanceTest()
    {
        ExecuteWatFile("watFiles/balance.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
    }

    [Fact]
    public void GasPriceTest()
    {
        ExecuteWatFile("watFiles/gas_price.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
    }

    [Fact]
    public void GasLeftTest()
    {
        ExecuteWatFile("watFiles/gas_left.wat");
    }

    [Fact]
    public void ValueTransferredTest()
    {
        ExecuteWatFile("watFiles/value_transferred.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
    }

    [Fact]
    public void StartFunctionIllegalTest()
    {
        var trapException = Assert.ThrowsAny<TrapException>(() => ExecuteWatFile("watFiles/start_fn_illegal.wat"));
        trapException.Message.ShouldContain("start");
    }

    [Fact]
    public void NowTest()
    {
        ExecuteWatFile("watFiles/timestamp_now.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
        
        ExecuteWatFile("watFiles/timestamp_now_unprefixed.wat");
        _runtimeErrors.Count.ShouldBe(2);
        _runtimeErrors[1].ShouldNotContain("assert");
    }

    [Fact]
    public void MinimumBalanceTest()
    {
        ExecuteWatFile("watFiles/minimum_balance.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
    }

    [Fact]
    public void RandomTest()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/random.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
        runtime.ReturnBuffer.ShouldBe(
            ByteArrayHelper.HexStringToByteArray("000102030405060708090A0B0C0D0E0F000102030405060708090A0B0C0D0E0F"));
    }

    [Fact]
    public void RandomTestV1()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/random_v1.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
        runtime.ReturnBuffer.ShouldBe(ByteArrayHelper
            .HexStringToByteArray("000102030405060708090A0B0C0D0E0F000102030405060708090A0B0C0D0E0F")
            .Concat(42.ToBytes(false)));
    }

    [Fact]
    public void DepositEventTest()
    {
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/deposit_event.wat");
        externalEnvironment.Events.Count.ShouldBe(1);
        externalEnvironment.Events[0].Item1
            .ShouldBe(new ByteArrayBuilder().RepeatedBytes(0x33, AElfConstants.HashByteArrayLength));
        externalEnvironment.Events[0].Item2.ShouldBe(new byte[]
            { 0x00, 0x01, 0x2a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xe5, 0x14, 0x00 });
    }

    [Fact]
    public void DepositEventDuplicatesAllowedTest()
    {
        var (externalEnvironment, _) = ExecuteWatFile("watFiles/deposit_event_duplicates.wat");
        externalEnvironment.Events[0].Item1.ShouldBe(new ByteArrayBuilder()
            .RepeatedBytes(0x01, AElfConstants.HashByteArrayLength)
            .Concat(new ByteArrayBuilder().RepeatedBytes(0x02, AElfConstants.HashByteArrayLength))
            .Concat(new ByteArrayBuilder().RepeatedBytes(0x01, AElfConstants.HashByteArrayLength))
            .Concat(new ByteArrayBuilder().RepeatedBytes(0x04, AElfConstants.HashByteArrayLength))
        );
        externalEnvironment.Events[0].Item2.ShouldBe(new byte[]
            { 0x00, 0x01, 0x2a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xe5, 0x14, 0x00 });
    }

    [Fact]
    public void DepositEventMaxTopicsTest()
    {
        ExecuteWatFile("watFiles/deposit_event_max_topics.wat");
        _runtimeErrors[0].ShouldContain(WebAssemblyError.TooManyTopics.ToString());
    }

    [Fact]
    public void BlockNumberTest()
    {
        ExecuteWatFile("watFiles/block_number.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
    }

    [Fact]
    public void SealReturnWithSuccessStatusTest()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/code_return_with_data.wat",
            ByteArrayHelper.HexStringToByteArray("00000000445566778899"));
        runtime.ReturnBuffer.ToHex().ShouldBe("445566778899");
        runtime.ReturnFlags.ShouldNotBe(ReturnFlags.Revert);
    }

    [Fact]
    public void ReturnWithRevertStatusTest()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/code_return_with_data.wat",
            ByteArrayHelper.HexStringToByteArray("010000005566778899"));
        runtime.ReturnBuffer.ToHex().ShouldBe("5566778899");
        runtime.ReturnFlags.ShouldBe(ReturnFlags.Revert);
    }

    [Fact]
    public void ContractOutOfBoundsAccessTest()
    {
        ExecuteWatFile("watFiles/out_of_bounds_access.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldContain(WebAssemblyError.OutOfBounds.ToString());
    }

    [Fact]
    public void ContractDecodeLengthIgnoredTest()
    {
        ExecuteWatFile("watFiles/decode_failure.wat");
        _runtimeErrors.Count.ShouldBe(1);
        _runtimeErrors[0].ShouldNotContain("assert");
    }

    [Fact]
    public void DebugMessageWorks()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/debug_message.wat");
        runtime.DebugMessages.Count.ShouldBe(1);
        runtime.DebugMessages.First().ShouldBe("Hello World!");
    }

    [Fact]
    public void DebugMessageInvalidUtf8FailsTest()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/debug_message_fail.wat");
        runtime.DebugMessages.ShouldBeEmpty();
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
        var externalEnvironment = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(externalEnvironment, watFilePath, false, 1, 1);

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

            externalEnvironment.SetStorage(key, new byte[] { 42 }, false);
            runtime.Input = ConstructKeyWithLengthInput(64, byteArrayBuilder.RepeatedBytes(1, 64));

            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.Success);
        }

        // value exists (test for 0 sized)
        {
            var key = byteArrayBuilder.RepeatedBytes(2, 19);

            externalEnvironment.SetStorage(key, new byte[] { }, false);
            runtime.Input = ConstructKeyWithLengthInput(19, byteArrayBuilder.RepeatedBytes(2, 19));
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetAction("call"));
            Convert.ToInt32(runtime.ReturnBuffer[0]).ShouldBe((int)ReturnCode.Success);
        }
    }

    [Fact]
    public async Task ClearStorageWorks()
    {
        const string watFilePath = "watFiles/clear_storage_works.wat";
        var externalEnvironment = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(externalEnvironment, watFilePath, false, 1, 1);

        var byteArrayBuilder = new ByteArrayBuilder();

        externalEnvironment.SetStorage(byteArrayBuilder.RepeatedBytes(1, 64), new byte[] { 42 }, false);
        externalEnvironment.SetStorage(byteArrayBuilder.RepeatedBytes(2, 19), Array.Empty<byte>(), false);
        
        // value does not exist
        {
            var key = byteArrayBuilder.RepeatedBytes(3, 32);
            runtime.Input = ConstructKeyWithLengthInput(32, key);
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetFunction<ActionResult>("call"));
            runtime.ReturnBuffer.ToInt32(false).ShouldBe(int.MaxValue);
            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldBeNull();
        }

        // value did exist
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 64);
            runtime.Input = ConstructKeyWithLengthInput(64, key);
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetFunction<ActionResult>("call"));
            runtime.ReturnBuffer.ToInt32(false).ShouldBe(1);
            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldBeNull();
        }
        
        // value did not exist (wrong key length)
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 64);
            runtime.Input = ConstructKeyWithLengthInput(63, key);
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetFunction<ActionResult>("call"));
            runtime.ReturnBuffer.ToInt32(false).ShouldBe(int.MaxValue);
            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldBeNull();
        }

        // value exists
        {
            var key = byteArrayBuilder.RepeatedBytes(2, 19);
            runtime.Input = ConstructKeyWithLengthInput(19, key);
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetFunction<ActionResult>("call"));
            runtime.ReturnBuffer.ToInt32(false).ShouldBe(0);
            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldBeNull();
        }
    }

    [Fact]
    public async Task TakeStorageWorks()
    {
        const string watFilePath = "watFiles/take_storage_works.wat";
        var externalEnvironment = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(externalEnvironment, watFilePath, false, 1, 1);

        var byteArrayBuilder = new ByteArrayBuilder();

        externalEnvironment.SetStorage(byteArrayBuilder.RepeatedBytes(1, 64), new byte[] { 42 }, false);
        externalEnvironment.SetStorage(byteArrayBuilder.RepeatedBytes(2, 19), Array.Empty<byte>(), false);

        // value does not exist -> error returned
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 64);
            runtime.Input = ConstructKeyWithLengthInput(63, key);
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetFunction<ActionResult>("call"));
            runtime.ReturnBuffer[..4].ToInt32(false).ShouldBe((int)ReturnCode.KeyNotFound);
        }

        // value did exist -> value returned.
        {
            var key = byteArrayBuilder.RepeatedBytes(1, 64);
            runtime.Input = ConstructKeyWithLengthInput(64, key);
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetFunction<ActionResult>("call"));
            runtime.ReturnBuffer[..4].ToInt32(false).ShouldBe((int)ReturnCode.Success);
            runtime.ReturnBuffer[4..].ShouldBe(new byte[] { 42 });
            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldBeNull();
        }

        // value did exist -> length returned (test for 0 sized)
        {
            var key = byteArrayBuilder.RepeatedBytes(2, 19);
            runtime.Input = ConstructKeyWithLengthInput(19, key);
            var instance = runtime.Instantiate();
            InvokeCall(instance.GetFunction<ActionResult>("call"));
            runtime.ReturnBuffer[..4].ToInt32(false).ShouldBe((int)ReturnCode.Success);
            runtime.ReturnBuffer[4..].ShouldBe(Array.Empty<byte>());
            var value = await externalEnvironment.GetStorageAsync(key);
            value.ShouldBeNull();
        }
    }

    [Fact]
    public void IsContractWorks()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/is_contract_works.wat");
        runtime.ReturnBuffer.ToInt32(false).ShouldBe(1);
    }

    [Fact]
    public void CodeHashWorks()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/code_hash_works.wat");

    }

    [Fact]
    public void OWnCodeHashWorks()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/own_code_hash_works.wat");

    }

    [Fact]
    public void CallerIsOriginWorks()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/caller_is_origin_works.wat");
        runtime.ReturnBuffer.ToInt32(false).ShouldBe(0);
    }

    [Fact]
    public void CallerIsRootWorks()
    {
        {
            var (_, runtime) = ExecuteWatFile("watFiles/caller_is_root_works.wat");
            runtime.ReturnBuffer.ToInt32(false).ShouldBe(0);
        }

        {
            var externalEnvironment = new UnitTestExternalEnvironment();
            var runtime = new WebAssemblyRuntime(externalEnvironment, "watFiles/caller_is_root_works.wat", false, 1, 1);
            externalEnvironment.Caller = null;
            InvokeCall(runtime.Instantiate().GetFunction<ActionResult>("call"));
            runtime.ReturnBuffer.ToInt32(false).ShouldBe(1);
        }
    }

    [Fact]
    public void SetCodeHashTest()
    {
        var (externalEnvironment, _) =
            ExecuteWatFile("watFiles/set_code_hash_works.wat", new ByteArrayBuilder().RepeatedBytes(0, 32));
        externalEnvironment.CodeHashes.Count.ShouldBe(1);
        externalEnvironment.CodeHashes[0].ShouldBe(new ByteArrayBuilder().RepeatedBytes(17, 32));
    }

    [Fact]
    public void ReentranceCountWorks()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/reentrance_count_works.wat");
    }

    [Fact]
    public void AccountReentranceCountWorks()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/account_reentrance_count_works.wat");
    }

    [Fact]
    public void InstantiationNonceWorks()
    {
        var (_, runtime) = ExecuteWatFile("watFiles/instantiation_nonce_works.wat");
    }

    [Fact]
    public void AddRemoveDelegateDependencyTest()
    {
        var (externalEnvironment, runtime) = ExecuteWatFile("watFiles/add_remove_delegate_dependency_works.wat");
        externalEnvironment.DelegateDependencies.Count.ShouldBe(1);
        externalEnvironment.DelegateDependencies[0].Value.ToByteArray()
            .ShouldBe(new ByteArrayBuilder().RepeatedBytes(1, 32));
    }
    
    [Fact]
    public void Sha256Test()
    {
        String input = "hello";
        // get this result by online tool https://tools.keycdn.com/sha256-online-generator
        String expectHash = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";
        var hash = SHA256.Create().ComputeHash(input.GetBytes()).ToHex();
        hash.ShouldBe(expectHash);
    }
    
    [Fact]
    public void Keccak256Test()
    {
        String input = "hello";
        // get this result by online tool https://emn178.github.io/online-tools/keccak_256.html
        String expectHash = "1c8aff950685c2ed4bc3174f3472287b56d9517b9c948127319a09a7a36deac8";
        var hash = new Sha3Keccack().CalculateHash(input.GetBytes()).ToHex();
        hash.ShouldBe(expectHash);
    }
    
    [Fact]
    public void Blake256Test()
    {
        String input = "hello";
        // get this result by online tool https://toolkitbay.com/tkb/tool/BLAKE2b_256
        String expectHash = "324dcf027dd4a30a932c441f365a25e86b173defa4b8e58948253471b81b72cf";
        var hash = Blake2b.ComputeHash(32, input.GetBytes()).ToHex();
        hash.ShouldBe(expectHash);
    }
    
    [Fact]
    public void Blake128Test()
    {
        String input = "hello";
        // get this result by online tool https://toolkitbay.com/tkb/tool/BLAKE2s_128
        String expectHash = "b00ece7999660332a8958b76533d1f78";
        var hash = Blake2s.CreateHMAC(16, "a".GetBytes()).ComputeHash(input.GetBytes()).ToHex();
        hash.ShouldBe(expectHash);
    }

    private (UnitTestExternalEnvironment, WebAssemblyRuntime) ExecuteWatFile(string watFilePath, byte[]? input = null)
    {
        var externalEnvironment = new UnitTestExternalEnvironment();
        var runtime = new WebAssemblyRuntime(externalEnvironment, watFilePath, false, 1, 1);
        if (input != null)
        {
            runtime.Input = input;
        }

        InvokeCall(runtime.Instantiate().GetFunction<ActionResult>("call"));
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

    private InvokeResult InvokeCall(Func<ActionResult>? action)
    {
        var invokeResult = new RuntimeActionInvoker().Invoke(action);
        if (!invokeResult.Success)
        {
            _runtimeErrors.Add(invokeResult.DebugMessage);
        }

        return invokeResult;
    }
}