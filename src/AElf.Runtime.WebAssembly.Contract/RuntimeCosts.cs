namespace AElf.Runtime.WebAssembly.Contract;

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
	Address,

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
	InputBase,

	// Weight of calling `seal_return` for the given output size.
	//Return(u32),
	Return,

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
	CallBase,

	// Weight of calling `seal_delegate_call` for the given input size.
	DelegateCallBase,

	// Weight of the transfer performed during a call.
	CallSurchargeTransfer,

	// Weight per byte that is cloned by supplying the `CLONE_INPUT` flag.
	//CallInputCloned(u32),
	CallInputCloned,

	// Weight of calling `seal_instantiate` for the given input length and salt.
	//InstantiateBase { input_data_len: u32, salt_len: u32 },
	InstantiateBase,

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
	HashBlake256,

	// Weight of calling `seal_hash_blake2_128` for the given input size.
	//HashBlake128(u32),
	HashBlake128,

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
	ReentrantCount,

	// Weight of calling `account_reentrance_count`
	AccountEntranceCount,

	// Weight of calling `instantiation_nonce`
	InstantationNonce,

	// Weight of calling `add_delegate_dependency`
	AddDelegateDependency,

	// Weight of calling `remove_delegate_dependency`
	RemoveDelegateDependency,
}