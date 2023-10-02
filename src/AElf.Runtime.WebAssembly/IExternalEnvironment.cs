using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly;

/// <summary>
/// Inspired by https://github.com/paritytech/substrate/blob/master/frame/contracts/src/exec.rs
/// </summary>
public interface IExternalEnvironment
{
    void SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext);

    Dictionary<string, ByteString> Writes { get; set; }
    Dictionary<string, bool> Reads { get; set; }
    Dictionary<string, bool> Deletes { get; set; }

    List<(byte[], byte[])> Events { get; }
    List<string> DebugMessages { get; }

    Address? Caller { get; }

    GasMeter GasMeter { get; set; }

    ExecuteReturnValue Call(Weight gasLimit, long depositLimit, Address to, long value, byte[] inputData,
        bool allowReentry);

    ExecuteReturnValue DelegateCall(Hash codeHash, byte[] data);

    (Address, ExecuteReturnValue) Instantiate(Weight gasLimit, long depositLimit, Hash codeHash, long value,
        byte[] inputData, byte[] salt);

    void Terminate(Address beneficiary);
    void Transfer(Address to, long value);
    WriteOutcome SetStorage(byte[] key, byte[]? value, bool takeOld);
    Task<byte[]?> GetStorageAsync(byte[] key);
    Task<int> GetStorageSizeAsync(byte[] key);
    bool IsContract(byte[] address);
    Hash? CodeHash(byte[] address);
    Hash OwnCodeHash();

    bool CallerIsOrigin();

    // TODO: -> Caller is zero contract or system contract.
    bool CallerIsRoot();

    /// <summary>
    /// Returns a reference to the account id of the current contract.
    /// </summary>
    /// <returns></returns>
    byte[] Address();

    /// <summary>
    /// Returns the balance of the current contract.
    /// </summary>
    /// <returns></returns>
    long Balance();

    long GetWeightPrice(Weight weight);

    /// <summary>
    /// Returns the value transferred along with this call.
    /// </summary>
    /// <returns></returns>
    long ValueTransferred();

    byte[]? EcdsaRecover(byte[] signature, byte[] messageHash);

    long Now();

    /// <summary>
    /// Returns the minimum balance that is required for creating an account.
    /// </summary>
    /// <returns></returns>
    long MinimumBalance();

    (byte[], long) Random(byte[] subject);

    /// <summary>
    /// Deposit an event with the given topics.
    /// <br/>
    /// There should not be any duplicates in `topics`.
    /// </summary>
    /// <param name="topics"></param>
    /// <param name="data"></param>
    void DepositEvent(byte[] topics, byte[] data);

    /// <summary>
    /// Returns the current block number.
    /// </summary>
    /// <returns></returns>
    long BlockNumber();

    /// <summary>
    /// Returns the maximum allowed size of a storage item.
    /// </summary>
    /// <returns></returns>
    int MaxValueSize();

    // 	fn get_weight_price(&self, weight: Weight) -> BalanceOf<Self::T>;
    // 	fn schedule(&self) -> &Schedule<Self::T>;
    // 	fn gas_meter(&self) -> &GasMeter<Self::T>;
    // 	fn gas_meter_mut(&mut self) -> &mut GasMeter<Self::T>;

    /// <summary>
    /// Append a string to the debug buffer.
    /// <br/>
    /// It is added as-is without any additional new line.
    /// <br/>
    /// This is a no-op if debug message recording is disabled which is always the case
    /// when the code is executing on-chain.
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Returns `true` if debug message recording is enabled. Otherwise `false` is returned.</returns>
    bool AppendDebugBuffer(string message);

    // 	fn call_runtime(&self, call: <Self::T as Config>::RuntimeCall) -> DispatchResultWithPostInfo;
    // 	fn sr25519_verify(&self, signature: &[u8; 64], message: &[u8], pub_key: &[u8; 32]) -> bool;
    byte[] EcdsaToEthAddress(byte[] pubkey);

    // 	fn contract_info(&mut self) -> &mut ContractInfo<Self::T>;

    void SetCodeHash(Hash hash);

    int ReentranceCount();
    int AccountReentranceCount(byte[] accountAddress);

    void AddDelegateDependency(Hash codeHash);
    void RemoveDelegateDependency(Hash codeHash);

    long Nonce();

    Task ChargeGasAsync(RuntimeCosts runtimeCosts, Weight weight);
    Task ChargeGasAsync(RuntimeCosts runtimeCosts, long size = 0);
}