namespace AElf.Runtime.WebAssembly;

public partial class WebAssemblyRuntime
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
        WriteSandboxOutput(outPtr, outLenPtr, Input);
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
        Console.WriteLine($"SealReturn: {flags}, {dataPtr}, {dataLen}");
        ReturnBuffer = new byte[dataLen];
        for (var offset = dataLen - 1; offset >= 0; offset--)
        {
            ReturnBuffer[offset] = _memory.ReadByte(dataPtr + offset);
        }
    }

    /// <summary>
    /// Stores the address of the caller into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    ///
    /// If this is a top-level call (i.e. initiated by an extrinsic) the origin address of the
    /// extrinsic will be returned. Otherwise, if this call is initiated by another contract then
    /// the address of the contract will be returned. The value is encoded as T::AccountId.
    ///
    /// If there is no address associated with the caller (e.g. because the caller is root) then
    /// it traps with `BadOrigin`.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void Caller(int outPtr, int outLenPtr)
    {
        var sender = _hostSmartContractBridgeContext.Sender.ToByteArray();
        WriteSandboxOutput(outPtr, outLenPtr, sender);
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
        var topics = ReadSandboxMemory(topicsPtr, topicsLen);
        var data = ReadSandboxMemory(dataPtr, dataLen);
        _externalEnvironment.DepositEvent(topics, data);
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
        DebugMessages.Add(_memory.ReadString(strPtr, strLen));
        return (int)ReturnCode.Success;
    }
}