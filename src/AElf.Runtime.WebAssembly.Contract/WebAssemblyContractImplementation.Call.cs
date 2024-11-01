using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.WebAssembly.Contract;

public partial class WebAssemblyContractImplementation
{
    /// <summary>
    /// Make a call to another contract.
    /// <br/>
    /// New version available
    /// <br/>
    /// This is equivalent to calling the newer version of this function with
    /// `flags` set to `ALLOW_REENTRY`. See the newer version for documentation.
    /// <br/>
    /// Note
    /// <br/>
    /// The values `_callee_len` and `_value_len` are ignored because the encoded sizes
    /// of those types are fixed through
    /// [`codec::MaxEncodedLen`]. The fields exist
    /// for backwards compatibility. Consider switching to the newest version of this function.
    /// </summary>
    /// <returns>ReturnCode</returns>
    private int CallV0(int calleePtr, int calleeLen, long gas, int valuePtr, int valueLen, int inputDataPtr,
        int inputDataLen, int outputPtr, int outputLenPtr)
    {
        return (int)Call(CallFlags.AllowReentry,
            new Call(calleePtr, valuePtr, null, new Weight(gas, 0)),
            inputDataPtr,
            inputDataLen,
            outputPtr,
            outputLenPtr);
    }

    /// <summary>
    /// Make a call to another contract.
    ///
    /// Equivalent to the newer [`seal2`][`super::api_doc::Version2::call`] version but works with
    /// <b>ref_time</b> Weight only. It is recommended to switch to the latest version, once it's
    /// stabilized.
    /// </summary>
    /// <returns>ReturnCode</returns>
    private int CallV1(int flags, int calleePtr, long gas, int valuePtr, int inputDataPtr,
        int inputDataLen, int outputPtr, int outputLenPtr)
    {
        CustomPrints.Add("CallV1.");
        CustomPrints.Add(flags.ToString());
        return (int)Call((CallFlags)flags,
            new Call(calleePtr, valuePtr, null, new Weight(gas, 0)),
            inputDataPtr,
            inputDataLen,
            outputPtr,
            outputLenPtr);
    }

    /// <summary>
    /// Make a call to another contract.
    ///
    /// The callees output buffer is copied to `output_ptr` and its length to `output_len_ptr`.
    /// The copy of the output buffer can be skipped by supplying the sentinel value
    /// of `SENTINEL` to `output_ptr`.
    /// </summary>
    /// <param name="flags">See "T:SolidityWasmRuntime.CallFlags" for a documentation of the supported flags.</param>
    /// <param name="calleePtr">
    /// a pointer to the address of the callee contract. Should be decodable as an
    /// `T::AccountId`. Traps otherwise.
    /// </param>
    /// <param name="refTimeLimit">how much <b>ref_time</b> Weight to devote to the execution.</param>
    /// <param name="proofSizeLimit">how much <b>proof_size</b> Weight to devote to the execution.</param>
    /// <param name="depositPtr">
    /// a pointer to the buffer with value of the storage deposit limit for the
    /// call. Should be decodable as a `T::Balance`. Traps otherwise. Passing `SENTINEL` means
    /// setting no specific limit for the call, which implies storage usage up to the limit of the
    /// parent call.
    /// </param>
    /// <param name="valuePtr">
    /// a pointer to the buffer with value, how much value to send. Should be
    /// decodable as a `T::Balance`. Traps otherwise.
    /// </param>
    /// <param name="inputDataPtr">a pointer to a buffer to be used as input data to the callee.</param>
    /// <param name="inputDataLen">length of the input data buffer.</param>
    /// <param name="outputPtr">a pointer where the output buffer is copied to.</param>
    /// <param name="outputLenPtr">
    /// in-out pointer to where the length of the buffer is read from and the
    /// actual length is written to.
    /// </param>
    /// <returns>ReturnCode</returns>
    private int CallV2(int flags, int calleePtr, long refTimeLimit, long proofSizeLimit, int depositPtr, int valuePtr,
        int inputDataPtr, int inputDataLen, int outputPtr, int outputLenPtr)
    {
        CustomPrints.Add("CallV2.");
        return (int)Call((CallFlags)flags,
            new Call(calleePtr, valuePtr, depositPtr, new Weight(refTimeLimit, proofSizeLimit)),
            inputDataPtr,
            inputDataLen,
            outputPtr,
            outputLenPtr);
    }

    /// <summary>
    /// Execute code in the context (storage, caller, value) of the current contract.
    ///
    /// Reentrancy protection is always disabled since the callee is allowed
    /// to modify the callers storage. This makes going through a reentrancy attack
    /// unnecessary for the callee when it wants to exploit the caller.
    /// </summary>
    /// <param name="flags">See "T:SolidityWasmRuntime.CallFlags" for a documentation of the supported flags.</param>
    /// <param name="codeHashPtr">a pointer to the hash of the code to be called.</param>
    /// <param name="inputDataPtr">a pointer to a buffer to be used as input data to the callee.</param>
    /// <param name="inputDataLen">length of the input data buffer.</param>
    /// <param name="outputPtr">a pointer where the output buffer is copied to.</param>
    /// <param name="outputLenPtr">
    /// in-out pointer to where the length of the buffer is read from and the
    /// actual length is written to.
    /// </param>
    /// <returns></returns>
    private int DelegateCallV0(int flags, int codeHashPtr, int inputDataPtr, int inputDataLen, int outputPtr,
        int outputLenPtr)
    {
        return (int)Call((CallFlags)flags,
            new DelegateCall(codeHashPtr),
            inputDataPtr,
            inputDataLen,
            outputPtr,
            outputLenPtr);
    }

    private ReturnCode Call(CallFlags callFlags, ICallType callType, int inputDataPtr, int inputDataLen, int outputPtr,
        int outputLenPtr)
    {
        CustomPrints.Add("Start Call.");

        byte[]? inputData;
        if (callFlags.HasFlag(CallFlags.CloneInput))
        {
            CustomPrints.Add("Has CloneInput.");

            if (InputData.IsNullOrEmpty())
            {
                ErrorMessages.Add(WebAssemblyError.InputForwarded.ToString());
                return ReturnCode.Exception;
            }

            var input = InputData!;
            ChargeGas(new CallInputCloned(input.Length));
            inputData = (byte[])input.Clone();
        }
        else if (callFlags.HasFlag(CallFlags.ForwardInput))
        {
            CustomPrints.Add("Has ForwardInput.");

            // transfer ownership.
            inputData = InputData;
            InputData = null;
        }
        else
        {
            CustomPrints.Add("No CloneInput & ForwardInput.");

            ChargeGas(new CopyFromContract(inputDataLen));

            inputData = ReadSandboxMemory(inputDataPtr, inputDataLen);
        }

        ExecuteReturnValue? outcome = null;
        switch (callType)
        {
            case Call call:
            {
                CustomPrints.Add("CallType: Call");

                var callee = ReadSandboxMemory(call.CalleePtr, 32).ToAddress();
                var depositLimit = call.DepositPtr == null ? 0 : ReadSandboxMemory((int)call.DepositPtr, 8).ToInt32(false);
                var value = ReadSandboxMemory(call.ValuePtr, 8).ToInt32(false);
                outcome = Call(call.Weight, depositLimit, callee, value, inputData!,
                    callFlags.HasFlag(CallFlags.AllowReentry));
                break;
            }
            case DelegateCall delegateCall:
            {
                CustomPrints.Add("CallType: DelegateCall");

                if (callFlags.HasFlag(CallFlags.AllowReentry))
                {
                    ErrorMessages.Add(WebAssemblyError.InvalidCallFlags.ToString());
                    return ReturnCode.Exception;
                }

                var codeHash = ReadSandboxMemory(delegateCall.CodeHashPtr, AElfConstants.HashByteArrayLength).ToHash();
                outcome = DelegateCall(codeHash, inputData!);
                break;
            }
        }

        if (callFlags.HasFlag(CallFlags.TailCall))
        {
            CustomPrints.Add("Has TailCall.");

            if (outcome != null)
            {
                ReturnBuffer = outcome.Data;
                return ReturnCode.CalleeTrapped;
            }
        }

        if (outcome is { Flags: ReturnFlags.Empty })
        {
            CustomPrints.Add("ReturnFlags.Empty");

            WriteSandboxOutput(outputPtr, outputLenPtr, outcome.Data, true);
        }

        CustomPrints.Add("Success");
        return ReturnCode.Success;
    }

    private ExecuteReturnValue Call(Weight gasLimit, long depositLimit, Address to, long value, byte[] inputData,
        bool allowReentry)
    {
        AllowReentry = allowReentry;
        var inputDataHex = inputData.ToHex();
        var methodName = inputDataHex[..8];
        var isAllowReentry = BoolValue.Parser
            .ParseFrom(Context.CallMethod(Context.Self, to, "is_allow_reentry",
                new StringValue { Value = methodName }.ToByteString())).Value;
        if (!isAllowReentry)
        {
            ErrorMessages.Add($"{WebAssemblyError.ReentranceDenied.ToString()}: {to}");
            return new ExecuteReturnValue
            {
                Flags = ReturnFlags.Revert
            };
        }

        var parameter = new byte[inputData.Length - 4];
        Array.Copy(inputData, 4, parameter, 0, parameter.Length);
        var parameterWithValue = new SolidityTransactionParameter
        {
            Parameter = ByteString.CopyFrom(parameter),
            Value = value
        }.ToByteString();
        var result = Context.CallMethod(Context.Self, to, methodName, parameterWithValue);
        return new ExecuteReturnValue
        {
            Data = result
        };
    }

    private ExecuteReturnValue DelegateCall(Hash codeHash, byte[] inputData)
    {
        ChargeGas(new TransactionPayment.DelegateCall());
        var inputDataHex = inputData.ToHex();
        var methodName = inputDataHex[..8];
        var parameter = new byte[inputData.Length - 4];
        Array.Copy(inputData, 4, parameter, 0, parameter.Length);
        var to = Types.Address.FromBytes(codeHash.ToByteArray());
        var parameterWithValue = new SolidityTransactionParameter
        {
            Parameter = ByteString.CopyFrom(parameter),
            DelegateCallValue = Value
        }.ToByteString();
        var result =
            Context.DelegateCall(Context.Sender, to, methodName, parameterWithValue);
        return new ExecuteReturnValue
        {
            Data = result
        };
    }
}

public interface ICallType
{
}

public record Call(int CalleePtr, int ValuePtr, int? DepositPtr, Weight Weight) : ICallType;
public record DelegateCall(int CodeHashPtr) : ICallType;

[Flags]
public enum CallFlags
{
    ForwardInput = 1 << 0,
    CloneInput = 1 << 1,
    TailCall = 1 << 2,
    AllowReentry = 1 << 3
}