using System.Text;

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
        if (State.Terminated.Value)
        {
            // TODO: Maybe not proper.
            HandleError(WebAssemblyError.TerminatedInConstructor);
        }

        if (Input == null)
        {
            HandleError(WebAssemblyError.InputForwarded);
        }

        WriteSandboxOutput(outPtr, outLenPtr, Input!, false, 
            len => (RuntimeCosts.CopyToContract, len));
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
        ChargeGas(RuntimeCosts.Return, dataLen);
        ReturnFlags = (ReturnFlags)flags;
        ReturnBuffer = ReadSandboxMemory(dataPtr, dataLen);
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
        var sender = Context.Sender.ToByteArray();
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
        if (dataLen > MaxValueSize())
        {
            HandleError(WebAssemblyError.ValueTooLarge);
            return;
        }

        var topicsBytes = ReadSandboxMemory(topicsPtr, topicsLen);
        var topicsCount = topicsBytes[0] / 4;
        var topics = topicsBytes.Take(new Range(1, topicsBytes[0] * 8 + 1)).ToArray();

        if (topicsCount > 4)
        {
            HandleError(WebAssemblyError.TooManyTopics);
            return;
        }

        var eventData = ReadSandboxMemory(dataPtr, dataLen);
        //topicsBytes = topics.Aggregate(Array.Empty<byte>(), (current, next) => current.Concat(next).ToArray());
        // DepositEvent(topics, eventData);
    }

    private int MaxValueSize()
    {
        return int.MaxValue;
    }

    private List<byte[]> DecodeEvent(byte[] topics)
    {
        var decodedTopics = new List<byte[]>();
        var length = topics[0] * 8;
        var topicCount = (topics.Length - 1) / length;
        for (var i = 0; i < topicCount; i++)
        {
            var topic = new byte[length];
            Array.Copy(topics, i * length + 1, topic, 0, length);
            decodedTopics.Add(topic);
        }

        return decodedTopics;
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
            // TODO: Find a way to valid utf8 bytes properly.
            var debugMessage = encoding.GetString(debugMessageBytes);
            DebugMessages.Add(debugMessage);
            return (int)ReturnCode.Success;
        }
        catch (DecoderFallbackException)
        {
            return (int)ReturnCode.CallRuntimeFailed;
        }
    }
    
    private void ChargeGas(RuntimeCosts runtimeCosts, long size = 0)
    {
        
    }
}