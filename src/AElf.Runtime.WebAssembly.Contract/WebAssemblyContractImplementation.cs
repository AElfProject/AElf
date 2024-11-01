using AElf.Contracts.MultiToken;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Sdk.CSharp;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Wasmtime;
using Module = Wasmtime.Module;

namespace AElf.Runtime.WebAssembly.Contract;

public partial class WebAssemblyContractImplementation : WebAssemblyContract<WebAssemblyContractState>
{
    private const long MemoryMin = 16;
    private const long MemoryMax = 16;

    private readonly byte[] _wasmCode;

    private readonly Engine _engine;
    private Store _store;
    private Linker _linker;
    private Memory _memory;
    private Module _module;

    public byte[] ReturnBuffer = Array.Empty<byte>();
    public ReturnFlags ReturnFlags = ReturnFlags.Empty;
    public long Value { get; set; } = 0;
    public long DelegateCallValue { get; set; } = 0;
    public bool Initialized => State.Initialized.Value;

    public WebAssemblyContractImplementation(byte[] wasmCode, bool withFuelConsumption = false)
    {
        _wasmCode = wasmCode;
        _engine = new Engine(new Config().WithFuelConsumption(withFuelConsumption));
    }

    public Instance Instantiate(byte[] inputData)
    {
        _store = new Store(_engine, inputData);
        _linker = new Linker(_engine);
        _memory = new Memory(_store, MemoryMin, MemoryMax);
        _module = Module.FromBytes(_engine, "contract", _wasmCode);
        DefineImportFunctions();
        State.Initialized.Value = true;
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        State.SolidityContractManager.Value = State.GenesisContract.Value;
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.RandomNumberContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        InputData = (byte[])_store.GetData()!;
        return _linker.Instantiate(_store, _module);
    }

    #region API functions

    /// <summary>
    /// Transfer some value to another account.
    /// </summary>
    /// <param name="accountPtr">
    /// a pointer to the address of the beneficiary account Should be decodable as
    /// an "T:AccountId". Traps otherwise.
    /// </param>
    /// <param name="accountLen">length of the address buffer.</param>
    /// <param name="valuePtr">
    /// a pointer to the buffer with value, how much value to send. Should be
    /// decodable as a `T::Balance`. Traps otherwise.
    /// </param>
    /// <param name="valueLen">length of the value buffer.</param>
    /// <returns>ReturnCode</returns>
    private int TransferV0(int accountPtr, int accountLen, int valuePtr, int valueLen)
    {
        var account = ReadSandboxMemory(accountPtr, accountLen).ToAddress();
        var valueBytes = ReadSandboxMemory(valuePtr, valueLen);
        var value = valueBytes.ToInt64(false);
        State.TokenContract.Transfer.Send(new TransferInput
        {
            To = account,
            Symbol = Context.Variables.NativeSymbol,
            Amount = value
        });
        return (int)ReturnCode.Success;
    }

    /// <summary>
    /// Remove the calling account and transfer remaining balance.
    /// <br/>
    /// <b>New version available</b>
    /// <br/>
    /// This is equivalent to calling the newer version of this function. The newer version
    /// drops the now unnecessary length fields.
    /// <br/>
    /// <b>Note</b>
    /// <br/>
    /// The value `_beneficiary_len` is ignored because the encoded sizes
    /// this type is fixed through `[`MaxEncodedLen`]. The field exist for backwards
    /// compatibility. Consider switching to the newest version of this function.
    /// </summary>
    /// <param name="beneficiaryPtr"></param>
    /// <param name="beneficiaryLen"></param>
    private void TerminateV0(int beneficiaryPtr, int beneficiaryLen)
    {
        Terminate(beneficiaryPtr);
    }

    /// <summary>
    /// Remove the calling account and transfer remaining **free** balance.
    /// <br/>
    /// This function never returns. Either the termination was successful and the
    /// execution of the destroyed contract is halted. Or it failed during the termination
    /// which is considered fatal and results in a trap + rollback.
    /// <br/>
    /// <b>Traps</b>
    /// <br/>
    /// - The contract is live i.e is already on the call stack.
    /// <br/>
    /// - Failed to send the balance to the beneficiary.
    /// <br/>
    /// - The deletion queue is full.
    ///
    /// </summary>
    /// 
    /// <param name="beneficiaryPtr">
    /// a pointer to the address of the beneficiary account where all where all
    /// remaining funds of the caller are transferred. Should be decodable as an `T::AccountId`.
    /// Traps otherwise.
    /// </param>
    private void TerminateV1(int beneficiaryPtr)
    {
        Terminate(beneficiaryPtr);
    }

    private void Terminate(int beneficiaryPtr)
    {
        var beneficiary = ReadSandboxMemory(beneficiaryPtr, AElfConstants.AddressHashLength);
        var amount = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Owner = Context.Self,
            Symbol = Context.Variables.NativeSymbol
        }).Balance;
        State.TokenContract.Transfer.Send(new TransferInput
        {
            To = new Address { Value = ByteString.CopyFrom(beneficiary) },
            Amount = amount,
            Symbol = Context.Variables.NativeSymbol
        });
        State.Terminated.Value = true;
    }

    /// <summary>
    /// Stores the value transferred along with this call/instantiate into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a `u32` value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    ///
    /// The data is encoded as `T::Balance`.
    /// </summary>
    /// <param name="valuePtr"></param>
    /// <param name="valueLengthPtr"></param>
    private void ValueTransferred(int valuePtr, int valueLengthPtr)
    {

        if (Value <= 0)
        {
            if (DelegateCallValue > 0)
            {
                WriteSandboxOutput(valuePtr, valueLengthPtr, DelegateCallValue);
            }

            return;
        }

        WriteSandboxOutput(valuePtr, valueLengthPtr, Value);

        if (AlreadyTransferred)
        {
            return;
        }
        var transferInput = new TransferInput
        {
            To = Context.Self,
            Symbol = Context.Variables.NativeSymbol,
            Amount = Value
        };
        Context.CallMethod(Context.Sender, State.TokenContract.Value, nameof(State.TokenContract.Transfer),
            transferInput.ToByteString());
        RuntimeLogs.Add($"Valued transferred: {transferInput.Amount}");

        AlreadyTransferred = true;
    }

    /// <summary>
    /// Call into the chain extension provided by the chain if any.
    ///
    /// Handling of the input values is up to the specific chain extension and so is the
    /// return value. The extension can decide to use the inputs as primitive inputs or as
    /// in/out arguments by interpreting them as pointers. Any caller of this function
    /// must therefore coordinate with the chain that it targets.
    ///
    /// # Note
    ///
    /// If no chain extension exists the contract will trap with the `NoChainExtension`
    /// module error.
    /// </summary>
    private void CallChainExtension(int id, int inputPtr, int inputLen, int outputPtr, int outputLenPtr)
    {
        ErrorMessages.Add("CallChainExtension not implemented.");
    }

    /// <summary>
    /// Call some dispatchable of the runtime.
    ///
    /// This function decodes the passed in data as the overarching `Call` type of the
    /// runtime and dispatches it. The weight as specified in the runtime is charged
    /// from the gas meter. Any weight refunds made by the dispatchable are considered.
    ///
    /// The filter specified by `Config::CallFilter` is attached to the origin of
    /// the dispatched call.
    ///
    /// # Comparison with `ChainExtension`
    ///
    /// Just as a chain extension this API allows the runtime to extend the functionality
    /// of contracts. While making use of this function is generally easier it cannot be
    /// used in all cases. Consider writing a chain extension if you need to do perform
    /// one of the following tasks:
    ///
    /// - Return data.
    /// - Provide functionality **exclusively** to contracts.
    /// - Provide custom weights.
    /// - Avoid the need to keep the `Call` data structure stable.
    /// </summary>
    /// <param name="callPtr">the pointer into the linear memory where the input data is placed.</param>
    /// <param name="callLen">the length of the input data in bytes.</param>
    /// <returns>
    /// Returns `ReturnCode::Success` when the dispatchable was successfully executed and
    /// returned `Ok`. When the dispatchable was exeuted but returned an error
    /// `ReturnCode::CallRuntimeFailed` is returned. The full error is not
    /// provided because it is not guaranteed to be stable.
    /// </returns>
    private int CallRuntime(int callPtr, int callLen)
    {
        ErrorMessages.Add("CallRuntime not implemented.");
        return (int)ReturnCode.Success;
    }

    /// <summary>
    /// Replace the contract code at the specified address with new code.
    ///
    /// # Note
    ///
    /// There are a couple of important considerations which must be taken into account when
    /// using this API:
    ///
    /// 1. The storage at the code address will remain untouched. This means that contract
    /// developers must ensure that the storage layout of the new code is compatible with that of
    /// the old code.
    ///
    /// 2. Contracts using this API can't be assumed as having deterministic addresses. Said another
    /// way, when using this API you lose the guarantee that an address always identifies a specific
    /// code hash.
    ///
    /// 3. If a contract calls into itself after changing its code the new call would use
    /// the new code. However, if the original caller panics after returning from the sub call it
    /// would revert the changes made by [`set_code_hash()`][`Self::set_code_hash`] and the next
    /// caller would use the old code.
    /// </summary>
    /// <param name="codeHashPtr">A pointer to the buffer that contains the new code hash.</param>
    /// <returns>ReturnCode</returns>
    private int SetCodeHash(int codeHashPtr)
    {
        var codeHash = Hash.LoadFromByteArray(ReadSandboxMemory(codeHashPtr, AElfConstants.HashByteArrayLength));
        var registration = State.GenesisContract.GetSmartContractRegistrationByCodeHash.Call(codeHash);
        if (registration.Category == 0)
        {
            HandleError(WebAssemblyError.CodeNotFound);
            return (int)ReturnCode.CallRuntimeFailed;
        }

        State.SolidityContractManager.UpdateSoliditySmartContract.Send(new UpdateSoliditySmartContractInput
        {
            ContractAddress = Context.Self,
            CodeHash = codeHash
        });
        return (int)ReturnCode.Success;
    }

    /// <summary>
    /// Adds a new delegate dependency to the contract.
    /// </summary>
    /// <param name="codeHashPtr">A pointer to the code hash of the dependency.</param>
    private void AddDelegateDependency(int codeHashPtr)
    {
        var codeHash = Hash.LoadFromByteArray(ReadSandboxMemory(codeHashPtr, AElfConstants.HashByteArrayLength));
        
        ErrorMessages.Add("AddDelegateDependency not implemented.");
    }

    /// <summary>
    /// Removes the delegate dependency from the contract.
    /// </summary>
    /// <param name="codeHashPtr">A pointer to the code hash of the dependency.</param>
    private void RemoveDelegateDependency(int codeHashPtr)
    {
        var codeHash = Hash.LoadFromByteArray(ReadSandboxMemory(codeHashPtr, AElfConstants.HashByteArrayLength));        ErrorMessages.Add("AddDelegateDependency not implemented.");

        ErrorMessages.Add("RemoveDelegateDependency not implemented.");
    }

    #endregion

    #region Helper functions

    private void WriteSandboxOutput(int outPtr, int outLenPtr, byte[] buf, bool allowSkip = false,
        Func<int, RuntimeCost>? createToken = null)
    {
        if (allowSkip && outPtr == -1)
        {
            return;
        }

        var bufLen = (uint)buf.Length;
        var lenBytes = ReadSandboxMemory(outLenPtr, 4);
        var len = lenBytes.ToInt32(false);

        if (len < bufLen)
        {
            HandleError(WebAssemblyError.OutOfBounds);
            throw new OutputBufferTooSmallException();
        }

        if (createToken != null)
        {
            GasMeter?.ChargeGas(createToken.Invoke(len));
        }

        WriteSandboxMemory(outPtr, buf);
        WriteSandboxMemory(outLenPtr, bufLen);
    }

    private void WriteSandboxOutput(int outPtr, int outLenPtr, long num, bool allowSkip = false)
    {
        WriteSandboxOutput(outPtr, outLenPtr, num.ToBytes(false), allowSkip);
    }

    private void WriteSandboxMemory(int ptr, byte[] buf)
    {
        foreach (var (offset, byt) in buf.Select((b, i) => (i, b)))
        {
            _memory.Write(ptr + offset, byt);
        }
    }

    private void WriteSandboxMemory(int ptr, uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        WriteSandboxMemory(ptr, bytes);
    }

    private byte[] ReadSandboxMemory(int ptr, int len)
    {
        var value = new byte[len];
        for (var i = 0; i < len; i++)
        {
            var index = ptr + i;
            if (index > _memory.GetLength())
            {
                HandleError(WebAssemblyError.OutOfBounds);
                return value;
            }

            value[i] = _memory.ReadByte(index);
        }

        return value;
    }

    #endregion

    private void HandleError(WebAssemblyError error)
    {
        DebugMessages.Add(error.ToString());
        throw new WebAssemblyRuntimeException(error.ToString());
    }
}