using System.Security.Cryptography;
using AElf.Contracts.MultiToken;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Sdk.CSharp;
using AElf.Types;
using Blake2Fast;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Nethereum.Util;
using Secp256k1Net;
using Wasmtime;
using Module = Wasmtime.Module;

namespace AElf.Runtime.WebAssembly.Contract;

public partial class WebAssemblyContractImplementation : WebAssemblyContract<WebAssemblyContractState>
{
    private readonly Store _store;
    private readonly Engine _engine;
    private readonly Linker _linker;
    private readonly Memory _memory;
    private readonly Module _module;

    public byte[] ReturnBuffer = Array.Empty<byte>();
    public ReturnFlags ReturnFlags = ReturnFlags.Empty;
    public List<string> DebugMessages => new();
    public List<(byte[], byte[])> Events => new();
    public byte[]? Input { get; set; } = Array.Empty<byte>();
    public long Value { get; set; } = 0;
    public bool Initialized => State.Initialized.Value;

    public WebAssemblyContractImplementation(byte[] wasmCode, bool withFuelConsumption = false, long memoryMin = 16,
        long memoryMax = 16)
    {
        _engine = new Engine(new Config().WithFuelConsumption(withFuelConsumption));
        _store = new Store(_engine);
        _linker = new Linker(_engine);
        _memory = new Memory(_store, memoryMin, memoryMax);
        _module = Module.FromBytes(_engine, "contract", wasmCode);
        DefineImportFunctions();
    }

    public Instance Instantiate()
    {
        State.Initialized.Value = true;
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        State.GenesisContract.Value = State.GenesisContract.Value;
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.RandomNumberContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
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
    /// Checks whether a specified address belongs to a contract.
    ///
    /// Returned value is a `u32`-encoded boolean: (0 = false, 1 = true).
    /// </summary>
    /// <param name="accountPtr">
    /// a pointer to the address of the beneficiary account Should be decodable as
    /// an `T::AccountId`. Traps otherwise.
    /// </param>
    /// <returns></returns>
    private int IsContract(int accountPtr)
    {
        var address = ReadSandboxMemory(accountPtr, 32);
        var contractHash = State.GenesisContract.GetContractHash.Call(Types.Address.FromBytes(address));
        var isContract = contractHash.Value.Any();
        return isContract ? 1 : 0;
    }

    /// <summary>
    /// Retrieve the code hash for a specified contract address.
    ///
    /// # Errors
    /// - `ReturnCode::KeyNotFound`
    /// </summary>
    /// <param name="accountPtr">
    /// a pointer to the address in question. Should be decodable as an
    /// `T::AccountId`. Traps otherwise.
    /// </param>
    /// <param name="outPtr">pointer to the linear memory where the returning value is written to.</param>
    /// <param name="outLenPtr">
    /// in-out pointer into linear memory where the buffer length is read from and
    /// the value length is written to.
    /// </param>
    /// <returns>ReturnCode</returns>
    private int CodeHash(int accountPtr, int outPtr, int outLenPtr)
    {
        var address = ReadSandboxMemory(accountPtr, 32);
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        var value = State.GenesisContract.GetContractHash.Call(Types.Address.FromBytes(address));
        if (value == null)
        {
            return (int)ReturnCode.KeyNotFound;
        }

        WriteSandboxOutput(outPtr, outLenPtr, value.Value.ToByteArray());
        return (int)ReturnCode.Success;
    }

    /// <summary>
    /// Retrieve the code hash of the currently executing contract.
    /// </summary>
    /// <param name="outPtr">pointer to the linear memory where the returning value is written to.</param>
    /// <param name="outLenPtr">
    /// in-out pointer into linear memory where the buffer length is read from and
    /// the value length is written to.
    /// </param>
    /// <returns></returns>
    private void OwnCodeHash(int outPtr, int outLenPtr)
    {
        var codeHash = State.GenesisContract.GetContractHash.Call(Context.Self);
        WriteSandboxOutput(outPtr, outLenPtr, codeHash.ToByteArray().ToArray(), false);
    }

    /// <summary>
    /// Checks whether the caller of the current contract is the origin of the whole call stack.
    ///
    /// Prefer this over [`is_contract()`][`Self::is_contract`] when checking whether your contract
    /// is being called by a contract or a plain account. The reason is that it performs better
    /// since it does not need to do any storage lookups.
    ///
    /// A return value of `true` indicates that this contract is being called by a plain account
    /// and `false` indicates that the caller is another contract.
    /// 
    /// </summary>
    /// <returns>Returned value is a `u32`-encoded boolean: (`0 = false`, `1 = true`).</returns>
    private int CallerIsOrigin()
    {
        return Context.Origin == Context.Sender ? 1 : 0;
    }

    /// <summary>
    /// Checks whether the caller of the current contract is root.
    ///
    /// Note that only the origin of the call stack can be root. Hence this function returning
    /// `true` implies that the contract is being called by the origin.
    ///
    /// A return value of `true` indicates that this contract is being called by a root origin,
    /// and `false` indicates that the caller is a signed origin.
    /// 
    /// </summary>
    /// <returns>Returned value is a `u32`-encoded boolean: (`0 = false`, `1 = true`).</returns>
    private int CallerIsRoot()
    {
        return Context.Sender == Context.GetZeroSmartContractAddress() ? 1 : 0;
    }

    /// <summary>
    /// Stores the address of the current contract into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void Address(int outPtr, int outLenPtr)
    {
        WriteSandboxOutput(outPtr, outLenPtr, Context.Self.ToByteArray());
    }

    /// <summary>
    /// Stores the price for the specified amount of gas into the supplied buffer.
    ///
    /// Equivalent to the newer [`seal1`][`super::api_doc::Version2::weight_to_fee`] version but
    /// works with *ref_time* Weight only. It is recommended to switch to the latest version, once
    /// it's stabilized.
    /// </summary>
    /// <param name="gas"></param>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void WeightToFeeV0(long gas, int outPtr, int outLenPtr)
    {
        WriteSandboxOutput(outPtr, outLenPtr,
            new FeeService(new IFeeProvider[]
            {
                new LengthFeeProvider(),
                new WeightFeeProvider(new FeeFunctionProvider())
            }).CalculateFees(new Weight(gas, 0)));
    }

    /// <summary>
    /// Stores the price for the specified amount of weight into the supplied buffer.
    ///
    /// # Parameters
    ///
    /// - `out_ptr`: pointer to the linear memory where the returning value is written to. If the
    ///   available space at `out_ptr` is less than the size of the value a trap is triggered.
    /// - `out_len_ptr`: in-out pointer into linear memory where the buffer length is read from and
    ///   the value length is written to.
    ///
    /// The data is encoded as `T::Balance`.
    ///
    /// # Note
    ///
    /// It is recommended to avoid specifying very small values for `ref_time_limit` and
    /// `proof_size_limit` as the prices for a single gas can be smaller than the basic balance
    /// unit.
    /// </summary>
    /// <param name="refTimeLimit"></param>
    /// <param name="proofSizeLimit"></param>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void WeightToFeeV1(long refTimeLimit, long proofSizeLimit, int outPtr, int outLenPtr)
    {

    }

    /// <summary>
    /// Stores the weight left into the supplied buffer.
    ///
    /// Equivalent to the newer [`seal1`][`super::api_doc::Version2::gas_left`] version but
    /// works with *ref_time* Weight only. It is recommended to switch to the latest version, once
    /// it's stabilized.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void GasLeftV0(int outPtr, int outLenPtr)
    {

    }

    /// <summary>
    /// Stores the amount of weight left into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    ///
    /// The data is encoded as Weight.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void GasLeftV1(int outPtr, int outLenPtr)
    {

    }

    /// <summary>
    /// Stores the *free* balance of the current account into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    ///
    /// The data is encoded as `T::Balance`.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void Balance(int outPtr, int outLenPtr)
    {
        var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Owner = Context.Self,
            Symbol = Context.Variables.NativeSymbol
        }).Balance;
        WriteSandboxOutput(outPtr, outLenPtr, balance);
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
        if (Value > 0)
        {
            WriteSandboxOutput(valuePtr, valueLengthPtr, Value);
        }
    }

    /// <summary>
    /// Stores a random number for the current block and the given subject into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    ///
    /// The data is encoded as `T::Hash`.
    /// </summary>
    /// <param name="subjectPtr"></param>
    /// <param name="subjectLenPtr"></param>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    /// <returns></returns>
    private void RandomV0(int subjectPtr, int subjectLenPtr, int outPtr, int outLenPtr)
    {
        var subject = ReadSandboxMemory(subjectPtr, subjectLenPtr);
        var random = State.RandomNumberContract.GetRandomBytes
            .Call(new BytesValue { Value = ByteString.CopyFrom(subject) }).ToByteArray();
        WriteSandboxOutput(outPtr, outLenPtr, random);
    }

    /// <summary>
    /// Stores a random number for the current block and the given subject into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    ///
    /// The data is encoded as (T::Hash, frame_system::pallet_prelude::BlockNumberFor::<T>).
    ///
    /// # Changes from v0
    ///
    /// In addition to the seed it returns the block number since which it was determinable
    /// by chain observers.
    ///
    /// # Note
    ///
    /// The returned seed should only be used to distinguish commitments made before
    /// the returned block number. If the block number is too early (i.e. commitments were
    /// made afterwards), then ensure no further commitments may be made and repeatedly
    /// call this on later blocks until the block number returned is later than the latest
    /// commitment.
    /// </summary>
    /// <param name="subjectPtr"></param>
    /// <param name="subjectLenPtr"></param>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    /// <returns></returns>
    private void RandomV1(int subjectPtr, int subjectLenPtr, int outPtr, int outLenPtr)
    {
        var subject = ReadSandboxMemory(subjectPtr, subjectLenPtr);
        var blockNumber = Context.CurrentHeight;
        var random =
            State.RandomNumberContract.GetRandomBytes.Call(new BytesValue
            {
                Value = ByteString.CopyFrom(subject)
            }).ToByteArray();
        var output = random.Concat(blockNumber.ToBytes(false)).ToArray();
        WriteSandboxOutput(outPtr, outLenPtr, output);
    }

    /// <summary>
    /// Load the latest block timestamp into the supplied buffer
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void Now(int outPtr, int outLenPtr)
    {
        var blockTime = Context.CurrentBlockTime.Seconds * 1000;
        WriteSandboxOutput(outPtr, outLenPtr, blockTime);
    }

    /// <summary>
    /// Stores the minimum balance (a.k.a. existential deposit) into the supplied buffer.
    ///
    /// The data is encoded as `T::Balance`.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void MinimumBalance(int outPtr, int outLenPtr)
    {
        WriteSandboxOutput(outPtr, outLenPtr, 0);
    }

    /// <summary>
    /// Stores the current block number of the current contract into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void BlockNumber(int outPtr, int outLenPtr)
    {
        WriteSandboxOutput(outPtr, outLenPtr, Context.CurrentHeight);
    }

    /// <summary>
    /// Computes the SHA2 256-bit hash on the given input buffer.
    ///
    /// Returns the result directly into the given output buffer.
    ///
    /// # Note
    ///
    /// - The `input` and `output` buffer may overlap.
    /// - The output buffer is expected to hold at least 32 bytes (256 bits).
    /// - It is the callers responsibility to provide an output buffer that is large enough to hold
    ///   the expected amount of bytes returned by the chosen hash function.
    /// </summary>
    /// <param name="inputPtr">the pointer into the linear memory where the input data is placed.</param>
    /// <param name="inputLen">the length of the input data in bytes.</param>
    /// <param name="outputPtr">
    /// the pointer into the linear memory where the output data is placed. The
    /// function will write the result directly into this buffer.
    /// </param>
    private void HashSha2_256(int inputPtr, int inputLen, int outputPtr)
    {
        var input = ReadSandboxMemory(inputPtr, inputLen);
        WriteSandboxMemory(outputPtr, SHA256.Create().ComputeHash(input));
    }

    /// <summary>
    /// Computes the KECCAK 256-bit hash on the given input buffer.
    ///
    /// Returns the result directly into the given output buffer.
    ///
    /// # Note
    ///
    /// - The `input` and `output` buffer may overlap.
    /// - The output buffer is expected to hold at least 32 bytes (256 bits).
    /// - It is the callers responsibility to provide an output buffer that is large enough to hold
    ///   the expected amount of bytes returned by the chosen hash function.
    /// </summary>
    /// <param name="inputPtr">the pointer into the linear memory where the input data is placed.</param>
    /// <param name="inputLen">the length of the input data in bytes.</param>
    /// <param name="outputPtr">
    /// the pointer into the linear memory where the output data is placed. The
    /// function will write the result directly into this buffer.
    /// </param>
    private void HashKeccak256(int inputPtr, int inputLen, int outputPtr)
    {
        var input = ReadSandboxMemory(inputPtr, inputLen);
        WriteSandboxMemory(outputPtr, new Sha3Keccack().CalculateHash(input));
    }

    /// <summary>
    /// Computes the BLAKE2 256-bit hash on the given input buffer.
    ///
    /// Returns the result directly into the given output buffer.
    ///
    /// # Note
    ///
    /// - The `input` and `output` buffer may overlap.
    /// - The output buffer is expected to hold at least 32 bytes (256 bits).
    /// - It is the callers responsibility to provide an output buffer that is large enough to hold
    ///   the expected amount of bytes returned by the chosen hash function.
    /// </summary>
    /// <param name="inputPtr">the pointer into the linear memory where the input data is placed.</param>
    /// <param name="inputLen">the length of the input data in bytes.</param>
    /// <param name="outputPtr">
    /// the pointer into the linear memory where the output data is placed. The
    /// function will write the result directly into this buffer.
    /// </param>
    private void HashBlake2_256(int inputPtr, int inputLen, int outputPtr)
    {
        var input = ReadSandboxMemory(inputPtr, inputLen);
        // var hashBlake256 = Blake2BFactory.Instance.Create(new Blake2BConfig {HashSizeInBits = 256}).ComputeHash(input).Hash;
        // Blake2B.ComputeHash(input);
        WriteSandboxMemory(outputPtr, Blake2b.ComputeHash(32, input));
    }

    /// <summary>
    /// Computes the BLAKE2 128-bit hash on the given input buffer.
    ///
    /// Returns the result directly into the given output buffer.
    ///
    /// # Note
    ///
    /// - The `input` and `output` buffer may overlap.
    /// - The output buffer is expected to hold at least 32 bytes (256 bits).
    /// - It is the callers responsibility to provide an output buffer that is large enough to hold
    ///   the expected amount of bytes returned by the chosen hash function.
    /// </summary>
    /// <param name="inputPtr">the pointer into the linear memory where the input data is placed.</param>
    /// <param name="inputLen">the length of the input data in bytes.</param>
    /// <param name="outputPtr">
    /// the pointer into the linear memory where the output data is placed. The
    /// function will write the result directly into this buffer.
    /// </param>
    private void HashBlake2_128(int inputPtr, int inputLen, int outputPtr)
    {
        var input = ReadSandboxMemory(inputPtr, inputLen);
        WriteSandboxMemory(outputPtr, Blake2s.ComputeHash(16, input));
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
        throw new NotImplementedException();
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
        var call = ReadSandboxMemory(callPtr, callLen);
        return (int)ReturnCode.Success;
    }

    /// <summary>
    /// Recovers the ECDSA public key from the given message hash and signature.
    ///
    /// Writes the public key into the given output buffer.
    /// Assumes the secp256k1 curve.
    /// </summary>
    /// <param name="signaturePtr">
    /// the pointer into the linear memory where the signature is placed. Should
    /// be decodable as a 65 bytes. Traps otherwise.
    /// </param>
    /// <param name="messageHashPtr">
    /// the pointer into the linear memory where the message hash is placed.
    /// Should be decodable as a 32 bytes. Traps otherwise.
    /// </param>
    /// <param name="outputPtr">
    /// the pointer into the linear memory where the output data is placed. The
    /// buffer should be 33 bytes. The function will write the result directly into this buffer.
    /// </param>
    /// <returns>ReturnCode</returns>
    private int EcdsaRecover(int signaturePtr, int messageHashPtr, int outputPtr)
    {
        var signature = ReadSandboxMemory(signaturePtr, 65);
        var messageHash = ReadSandboxMemory(messageHashPtr, AElfConstants.HashByteArrayLength);
        var pubkey = Context.RecoverPublicKey(signature, messageHash);
        if (pubkey != null)
        {
            WriteSandboxMemory(outputPtr, pubkey);
            ReturnBuffer = pubkey;
            return (int)ReturnCode.Success;
        }

        return (int)ReturnCode.EcdsaRecoverFailed;
    }

    /// <summary>
    /// Verify a sr25519 signature
    /// </summary>
    /// <param name="signaturePtr">
    /// the pointer into the linear memory where the signature is placed. Should
    /// be a value of 64 bytes.
    /// </param>
    /// <param name="pubKeyPtr">
    /// the pointer into the linear memory where the public key is placed. Should
    /// be a value of 32 bytes.
    /// </param>
    /// <param name="messageLen">the length of the message payload.</param>
    /// <param name="messagePtr">the pointer into the linear memory where the message is placed.</param>
    /// <returns>ReturnCode</returns>
    private int Sr25519Verify(int signaturePtr, int pubKeyPtr, int messageLen, int messagePtr)
    {
        throw new NotImplementedException();
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
        //_externalEnvironment.SetCodeHash(codeHash);
        // TODO: Update contract.
        return (int)ReturnCode.Success;
    }

    /// <summary>
    /// Calculates Ethereum address from the ECDSA compressed public key and stores
    /// it into the supplied buffer.
    /// <br/>
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// If the available space at `out_ptr` is less than the size of the value a trap is triggered.
    /// </summary>
    /// <param name="keyPtr">
    /// a pointer to the ECDSA compressed public key. Should be decodable as a 33 bytes
    /// value. Traps otherwise.
    /// </param>
    /// <param name="outPtr">
    /// the pointer into the linear memory where the output data is placed. The
    /// function will write the result directly into this buffer.
    /// </param>
    /// <returns></returns>
    private int EcdsaToEthAddress(int keyPtr, int outPtr)
    {
        var compressedKey = ReadSandboxMemory(keyPtr, 33);
        var ethAddress = EcdsaToEthAddress(compressedKey);
        WriteSandboxMemory(outPtr, ethAddress);
        return (int)ReturnCode.Success;
    }

    private byte[] EcdsaToEthAddress(byte[] pubkey)
    {
        if (pubkey.Length != Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH)
        {
            throw new ArgumentException("Incorrect pubkey size.");
        }

        var pubkeyNoPrefixCompressed = new byte[pubkey.Length - 1];
        Array.Copy(pubkey, 1, pubkeyNoPrefixCompressed, 0, pubkeyNoPrefixCompressed.Length);
        var initAddress = new Sha3Keccack().CalculateHash(pubkeyNoPrefixCompressed);
        var address = new byte[initAddress.Length - 12];
        Array.Copy(initAddress, 12, address, 0, initAddress.Length - 12);
        return address;
    }

    /// <summary>
    /// Returns the number of times the currently executing contract exists on the call stack in
    /// addition to the calling instance.
    /// </summary>
    /// <returns>Returns `0` when there is no reentrancy.</returns>
    private int ReentranceCount()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the number of times specified contract exists on the call stack. Delegated calls are
    /// not counted as separate calls.
    /// </summary>
    /// <param name="accountPtr">a pointer to the contract address.</param>
    /// <returns>Returns `0` when the contract does not exist on the call stack.</returns>
    private int AccountReentranceCount(int accountPtr)
    {
        var address = ReadSandboxMemory(accountPtr, 32);
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a nonce that is unique per contract instantiation.
    ///
    /// The nonce is incremented for each successful contract instantiation. This is a
    /// sensible default salt for contract instantiations.
    /// </summary>
    /// <returns></returns>
    private int InstantiationNonce()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds a new delegate dependency to the contract.
    /// </summary>
    /// <param name="codeHashPtr">A pointer to the code hash of the dependency.</param>
    private void AddDelegateDependency(int codeHashPtr)
    {
        var codeHash = Hash.LoadFromByteArray(ReadSandboxMemory(codeHashPtr, AElfConstants.HashByteArrayLength));
        throw new NotImplementedException();
    }

    /// <summary>
    /// Removes the delegate dependency from the contract.
    /// </summary>
    /// <param name="codeHashPtr">A pointer to the code hash of the dependency.</param>
    private void RemoveDelegateDependency(int codeHashPtr)
    {
        var codeHash = Hash.LoadFromByteArray(ReadSandboxMemory(codeHashPtr, AElfConstants.HashByteArrayLength));
        throw new NotImplementedException();
    }

    #endregion

    #region Helper functions

    private void WriteSandboxOutput(int outPtr, int outLenPtr, byte[] buf, bool allowSkip = false,
        Func<int, (RuntimeCosts, int)>? createToken = null)
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
            var (runtimeCosts, size) = createToken.Invoke(len);
            //_externalEnvironment.ChargeGasAsync(runtimeCosts, size);
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

    private void HandleError(DispatchError error)
    {
        DebugMessages.Add(error.ToString());
        throw new WebAssemblyRuntimeException(error.ToString());
    }
}