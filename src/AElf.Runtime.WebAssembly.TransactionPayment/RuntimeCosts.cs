namespace AElf.Runtime.WebAssembly.TransactionPayment;

public abstract record RuntimeCost(RuntimeCosts RuntimeCosts);
public record CopyFromContract(int ContractBytesSize) : RuntimeCost(RuntimeCosts.CopyFromContract);
public record CopyToContract(int ContractBytesSize) : RuntimeCost(RuntimeCosts.CopyToContract);
public record Caller() : RuntimeCost(RuntimeCosts.Caller);
public record IsContract() : RuntimeCost(RuntimeCosts.IsContract);
public record CodeHash() : RuntimeCost(RuntimeCosts.CodeHash);
public record OwnCodeHash() : RuntimeCost(RuntimeCosts.OwnCodeHash);
public record CallerIsOrigin() : RuntimeCost(RuntimeCosts.CallerIsOrigin);
public record CallerIsRoot() : RuntimeCost(RuntimeCosts.CallerIsRoot);
public record GetAddress() : RuntimeCost(RuntimeCosts.GetAddress);
public record GasLeft() : RuntimeCost(RuntimeCosts.GasLeft);
public record Balance() : RuntimeCost(RuntimeCosts.Balance);
public record ValueTransferred() : RuntimeCost(RuntimeCosts.ValueTransferred);
public record MinimumBalance() : RuntimeCost(RuntimeCosts.MinimumBalance);
public record BlockNumber() : RuntimeCost(RuntimeCosts.BlockNumber);
public record Now() : RuntimeCost(RuntimeCosts.Now);
public record WeightToFee() : RuntimeCost(RuntimeCosts.WeightToFee);
public record SealInput() : RuntimeCost(RuntimeCosts.Input);
public record SealInputPerByte() : RuntimeCost(RuntimeCosts.InputPerByte);
public record SealReturn(int BytesSize) : RuntimeCost(RuntimeCosts.Return);
public record SealReturnPerByte(int BytesSize) : RuntimeCost(RuntimeCosts.ReturnPerByte);
public record Terminate() : RuntimeCost(RuntimeCosts.Terminate);
public record Random() : RuntimeCost(RuntimeCosts.Random);
public record DepositEvent(int TopicNumber, int Length) : RuntimeCost(RuntimeCosts.DepositEvent);
public record DebugMessage(int BytesSize) : RuntimeCost(RuntimeCosts.DebugMessage);
public record SetStorage(int OldBytesSize, int NewBytesSize) : RuntimeCost(RuntimeCosts.SetStorage);
public record ClearStorage(int BytesSize) : RuntimeCost(RuntimeCosts.ClearStorage);
public record ContainsStorage(int BytesSize) : RuntimeCost(RuntimeCosts.ContainsStorage);
public record GetStorage(int BytesSize) : RuntimeCost(RuntimeCosts.GetStorage);
public record TakeStorage(int BytesSize) : RuntimeCost(RuntimeCosts.TakeStorage);
public record Transfer() : RuntimeCost(RuntimeCosts.Transfer);
public record Call() : RuntimeCost(RuntimeCosts.Call);
public record DelegateCall() : RuntimeCost(RuntimeCosts.DelegateCall);
public record CallSurchargeTransfer() : RuntimeCost(RuntimeCosts.CallSurchargeTransfer);
public record CallInputCloned(int BytesSize) : RuntimeCost(RuntimeCosts.CallInputCloned);
public record Instantiate() : RuntimeCost(RuntimeCosts.Instantiate);
public record InstantiateSurchargeTransfer() : RuntimeCost(RuntimeCosts.InstantiateSurchargeTransfer);
public record HashSha256(int BytesSize) : RuntimeCost(RuntimeCosts.HashSha256);
public record HashKeccak256(int BytesSize) : RuntimeCost(RuntimeCosts.HashKeccak256);
public record HashBlake2256(int BytesSize) : RuntimeCost(RuntimeCosts.HashBlake2256);
public record HashBlake2128(int BytesSize) : RuntimeCost(RuntimeCosts.HashBlake2128);
public record EcdsaRecovery() : RuntimeCost(RuntimeCosts.EcdsaRecovery);
public record Sr25519Verify(int BytesSize) : RuntimeCost(RuntimeCosts.Sr25519Verify);
public record ChainExtension(int Weight) : RuntimeCost(RuntimeCosts.ChainExtension);
public record CallRuntime(int Weight) : RuntimeCost(RuntimeCosts.CallRuntime);
public record SetCodeHash() : RuntimeCost(RuntimeCosts.SetCodeHash);
public record EcdsaToEthAddress() : RuntimeCost(RuntimeCosts.EcdsaToEthAddress);
public record ReentranceCount() : RuntimeCost(RuntimeCosts.ReentranceCount);
public record AccountReentranceCount() : RuntimeCost(RuntimeCosts.AccountReentranceCount);
public record InstantiationNonce() : RuntimeCost(RuntimeCosts.InstantiationNonce);
public record AddDelegateDependency() : RuntimeCost(RuntimeCosts.AddDelegateDependency);
public record RemoveDelegateDependency() : RuntimeCost(RuntimeCosts.RemoveDelegateDependency);

public enum RuntimeCosts
{
	// Weight charged for copying data from the sandbox.
	//CopyFromContract(u32),
	CopyFromContract,

	// Weight charged for copying data to the sandbox.
	//CopyToContract(u32),
	CopyToContract,

	// Weight of calling `seal_caller`.
	Caller,

	// Weight of calling `seal_is_contract`.
	IsContract,

	// Weight of calling `seal_code_hash`.
	CodeHash,

	// Weight of calling `seal_own_code_hash`.
	OwnCodeHash,

	// Weight of calling `seal_caller_is_origin`.
	CallerIsOrigin,

	// Weight of calling `caller_is_root`.
	CallerIsRoot,

	// Weight of calling `seal_address`.
	GetAddress,

	// Weight of calling `seal_gas_left`.
	GasLeft,

	// Weight of calling `seal_balance`.
	Balance,

	// Weight of calling `seal_value_transferred`.
	ValueTransferred,

	// Weight of calling `seal_minimum_balance`.
	MinimumBalance,

	// Weight of calling `seal_block_number`.
	BlockNumber,

	// Weight of calling `seal_now`.
	Now,

	// Weight of calling `seal_weight_to_fee`.
	WeightToFee,

	// Weight of calling `seal_input` without the weight of copying the input.
	Input,
	InputPerByte,

	// Weight of calling `seal_return` for the given output size.
	//Return(u32),
	Return,
	ReturnPerByte,

	// Weight of calling `seal_terminate`.
	Terminate,

	// Weight of calling `seal_random`. It includes the weight for copying the subject.
	Random,

	// Weight of calling `seal_deposit_event` with the given number of topics and event size.
	//DepositEvent { num_topic: u32, len: u32 },
	DepositEvent,

	// Weight of calling `seal_debug_message` per byte of passed message.
	//DebugMessage(u32),
	DebugMessage,

	// Weight of calling `seal_set_storage` for the given storage item sizes.
	//SetStorage { old_bytes: u32, new_bytes: u32 },
	SetStorage,

	// Weight of calling `seal_clear_storage` per cleared byte.
	//ClearStorage(u32),
	ClearStorage,

	// Weight of calling `seal_contains_storage` per byte of the checked item.
	//ContainsStorage(u32),
	ContainsStorage,

	// Weight of calling `seal_get_storage` with the specified size in storage.
	//GetStorage(u32),
	GetStorage,

	// Weight of calling `seal_take_storage` for the given size.
	//TakeStorage(u32),
	TakeStorage,

	// Weight of calling `seal_transfer`.
	Transfer,

	// Base weight of calling `seal_call`.
	Call,

	// Weight of calling `seal_delegate_call` for the given input size.
	DelegateCall,

	// Weight of the transfer performed during a call.
	CallSurchargeTransfer,

	// Weight per byte that is cloned by supplying the `CLONE_INPUT` flag.
	//CallInputCloned(u32),
	CallInputCloned,

	// Weight of calling `seal_instantiate` for the given input length and salt.
	//InstantiateBase { input_data_len: u32, salt_len: u32 },
	Instantiate,

	// Weight of the transfer performed during an instantiate.
	InstantiateSurchargeTransfer,

	// Weight of calling `seal_hash_sha_256` for the given input size.
	//HashSha256(u32),
	HashSha256,

	// Weight of calling `seal_hash_keccak_256` for the given input size.
	//HashKeccak256(u32),
	HashKeccak256,

	// Weight of calling `seal_hash_blake2_256` for the given input size.
	//HashBlake256(u32),
	HashBlake2256,

	// Weight of calling `seal_hash_blake2_128` for the given input size.
	//HashBlake128(u32),
	HashBlake2128,

	// Weight of calling `seal_ecdsa_recover`.
	EcdsaRecovery,

	// Weight of calling `seal_sr25519_verify` for the given input size.
	//Sr25519Verify(u32),
	Sr25519Verify,

	// Weight charged by a chain extension through `seal_call_chain_extension`.
	//ChainExtension(Weight),
	ChainExtension,

	// Weight charged for calling into the runtime.
	//CallRuntime(Weight),
	CallRuntime,

	// Weight of calling `seal_set_code_hash`
	SetCodeHash,

	// Weight of calling `ecdsa_to_eth_address`
	EcdsaToEthAddress,

	// Weight of calling `reentrance_count`
	ReentranceCount,

	// Weight of calling `account_reentrance_count`
	AccountReentranceCount,

	// Weight of calling `instantiation_nonce`
	InstantiationNonce,

	// Weight of calling `add_delegate_dependency`
	AddDelegateDependency,

	// Weight of calling `remove_delegate_dependency`
	RemoveDelegateDependency,
}