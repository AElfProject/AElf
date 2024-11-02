using System.Text;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS10;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Contract;

public partial class WebAssemblyContractImplementation
{
    /// <summary>
    /// Stores the input passed by the caller into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    /// <br/>
    /// <b>Note</b>
    /// <br/>
    /// This function traps if the input was previously forwarded by a [`call()`][`Self::call()`].
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void InputV0(int outPtr, int outLenPtr)
    {
        CustomPrints.Add("Start Input.");
        if (State.Terminated.Value)
        {
            // TODO: Maybe not proper.
            HandleError(WebAssemblyError.TerminatedInConstructor);
        }

        if (InputData == null)
        {
            CustomPrints.Add("InputData is null.");
            ErrorMessages.Add(WebAssemblyError.InputForwarded.ToString());
            return;
        }

        WriteSandboxOutput(outPtr, outLenPtr, InputData!, false,
            len => new CopyToContract(len));
        CustomPrints.Add("End input.");
    }

    /// <summary>
    /// Cease contract execution and save a data buffer as a result of the execution.
    ///
    /// This function never returns as it stops execution of the caller.
    /// This is the only way to return a data buffer to the caller. Returning from
    /// execution without calling this function is equivalent to calling:
    /// <code>
    /// nocompile
    /// seal_return(0, 0, 0);
    /// </code>>
    ///
    /// The flags argument is a bitfield that can be used to signal special return
    /// conditions to the supervisor:
    /// --- lsb ---
    /// bit 0      : REVERT - Revert all storage changes made by the caller.
    /// bit [1, 31]: Reserved for future use.
    /// --- msb ---
    ///
    /// Using a reserved bit triggers a trap.
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="dataPtr"></param>
    /// <param name="dataLen"></param>
    private void SealReturnV0(int flags, int dataPtr, int dataLen)
    {
        ChargeGas(new SealReturn(dataLen));
        ReturnFlags = (ReturnFlags)flags;
        if (dataLen > 0)
        {
            ReturnBuffer = ReadSandboxMemory(dataPtr, dataLen);
            if (ReturnBuffer.ToHex().StartsWith("4e487b71"))
            {
                ErrorMessages.Add("Panic(uint256)");
            }
        }

        ConsumedFuel = (long)_store.GetConsumedFuel();

        Context.CallMethod(Context.Sender,
            Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName),
            nameof(State.TreasuryContract.Donate), new DonateInput
            {
                Symbol = Context.Variables.NativeSymbol,
                Amount = ConsumedFuel
            }.ToByteString());
    }

    /// <summary>
    /// Deposit a contract event with the data buffer and optional list of topics. There is a limit
    /// on the maximum number of topics specified by `event_topics`.
    /// </summary>
    /// <param name="topicsPtr">
    /// a pointer to the buffer of topics encoded as `Vec T::Hash`. The value of
    /// this is ignored if `topics_len` is set to `0`. The topics list can't contain duplicates.
    /// </param>
    /// <param name="topicsLen">the length of the topics buffer. Pass 0 if you want to pass an empty vector.</param>
    /// <param name="dataPtr">a pointer to a raw data buffer which will saved along the event.</param>
    /// <param name="dataLen">the length of the data buffer.</param>
    private void DepositEvent(int topicsPtr, int topicsLen, int dataPtr, int dataLen)
    {
        var topicNumber = topicsLen.Div(AElfConstants.HashByteArrayLength);
        ChargeGas(new DepositEvent(topicNumber, dataLen));

        if (dataLen > MaxValueSize())
        {
            HandleError(WebAssemblyError.ValueTooLarge);
            return;
        }

        var topics = ReadSandboxMemory(topicsPtr, topicsLen);
        var data = ReadSandboxMemory(dataPtr, dataLen);

        Events.Add((topics, data));
    }

    private int MaxValueSize()
    {
        return 16 * 1024;
    }

    /// <summary>
    /// Emit a custom debug message.
    ///
    /// No newlines are added to the supplied message.
    /// Specifying invalid UTF-8 just drops the message with no trap.
    ///
    /// This is a no-op if debug message recording is disabled which is always the case
    /// when the code is executing on-chain. The message is interpreted as UTF-8 and
    /// appended to the debug buffer which is then supplied to the calling RPC client.
    ///
    /// # Note
    ///
    /// Even though no action is taken when debug message recording is disabled there is still
    /// a non trivial overhead (and weight cost) associated with calling this function. Contract
    /// languages should remove calls to this function (either at runtime or compile time) when
    /// not being executed as an RPC. For example, they could allow users to disable logging
    /// through compile time flags (cargo features) for on-chain deployment. Additionally, the
    /// return value of this function can be cached in order to prevent further calls at runtime.
    /// </summary>
    /// <param name="strPtr"></param>
    /// <param name="strLen"></param>
    /// <returns></returns>
    private int DebugMessage(int strPtr, int strLen)
    {
        var encoding = new UTF8Encoding(false, true);
        var debugMessageBytes = ReadSandboxMemory(strPtr, strLen);
        try
        {
            var message = encoding.GetString(debugMessageBytes);
            if (message.Contains("error"))
            {
                ErrorMessages.Add(message);
                return (int)ReturnCode.CallRuntimeFailed;
            }

            if (message.Contains("print"))
            {
                CustomPrints.Add(message);
            }
            else if (message.Contains("call"))
            {
                RuntimeLogs.Add(message);
            }
            else
            {
                DebugMessages.Add(message);
            }

            return (int)ReturnCode.Success;
        }
        catch (DecoderFallbackException)
        {
            return (int)ReturnCode.CallRuntimeFailed;
        }
    }
}