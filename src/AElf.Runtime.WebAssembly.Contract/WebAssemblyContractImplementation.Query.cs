using AElf.Contracts.MultiToken;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.WebAssembly.Contract;

public partial class WebAssemblyContractImplementation
{
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
        CustomPrints.Add($"Call: {Context.Sender}");
        var sender = Context.Sender.ToByteArray();
        WriteSandboxOutput(outPtr, outLenPtr, sender);
    }

    /// <summary>
    /// Returns the number of times the currently executing contract exists on the call stack in
    /// addition to the calling instance.
    /// </summary>
    /// <returns>Returns `0` when there is no reentrancy.</returns>
    private int ReentranceCount()
    {
        ErrorMessages.Add("ReentranceCount not implemented.");
        return (int)ReturnCode.Success;
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
        ErrorMessages.Add("AccountReentranceCount not implemented.");
        return (int)ReturnCode.Success;
    }

    /// <summary>
    /// Returns a nonce that is unique per contract instantiation.
    ///
    /// The nonce is incremented for each successful contract instantiation. This is a
    /// sensible default salt for contract instantiations.
    /// </summary>
    /// <returns></returns>
    private long InstantiationNonce()
    {
        var result = Context.CallMethod(Context.Self, State.SolidityContractManager.Value,
            nameof(State.SolidityContractManager.InstantiationNonce), ByteString.Empty);
        var nonce = new Int64Value();
        nonce.MergeFrom(result);
        return nonce.Value;
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
        WriteSandboxOutput(outPtr, outLenPtr, codeHash.ToByteArray().ToArray());
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
}