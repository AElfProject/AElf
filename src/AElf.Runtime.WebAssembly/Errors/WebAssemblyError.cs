namespace AElf.Runtime.WebAssembly;

public enum WebAssemblyError
{
	// Invalid schedule supplied, e.g. with zero weight of a basic operation.
	InvalidSchedule,

	// Invalid combination of flags supplied to `seal_call` or `seal_delegate_call`.
	InvalidCallFlags,

	// The executed contract exhausted its gas limit.
	OutOfGas,

	// The output buffer supplied to a contract API call was too small.
	OutputBufferTooSmall,

	// Performing the requested transfer failed. Probably because there isn't enough
	// free balance in the sender's account.
	TransferFailed,

	// Performing a call was denied because the calling depth reached the limit
	// of what is specified in the schedule.
	MaxCallDepthReached,

	// No contract was found at the specified address.
	ContractNotFound,

	// The code supplied to `instantiate_with_code` exceeds the limit specified in the
	// current schedule.
	CodeTooLarge,

	// No code could be found at the supplied code hash.
	CodeNotFound,

	// No code info could be found at the supplied code hash.
	CodeInfoNotFound,

	// A buffer outside of sandbox memory was passed to a contract API function.
	OutOfBounds,

	// Input passed to a contract API function failed to decode as expected type.
	DecodingFailed,

	// Contract trapped during execution.
	ContractTrapped,

	// The size defined in `T::MaxValueSize` was exceeded.
	ValueTooLarge,

	// Termination of a contract is not allowed while the contract is already
	// on the call stack. Can be triggered by `seal_terminate`.
	TerminatedWhileReentrant,

	// `seal_call` forwarded this contracts input. It therefore is no longer available.
	InputForwarded,

	// The subject passed to `seal_random` exceeds the limit.
	RandomSubjectTooLong,

	// The amount of topics passed to `seal_deposit_events` exceeds the limit.
	TooManyTopics,

	// The chain does not provide a chain extension. Calling the chain extension results
	// in this error. Note that this usually  shouldn't happen as deploying such contracts
	// is rejected.
	NoChainExtension,

	// A contract with the same AccountId already exists.
	DuplicateContract,

	// A contract self destructed in its constructor.
	//
	// This can be triggered by a call to `seal_terminate`.
	TerminatedInConstructor,

	// A call tried to invoke a contract that is flagged as non-reentrant.
	// The only other cause is that a call from a contract into the runtime tried to call back
	// into `pallet-contracts`. This would make the whole pallet reentrant with regard to
	// contract code execution which is not supported.
	ReentranceDenied,

	// Origin doesn't have enough balance to pay the required storage deposits.
	StorageDepositNotEnoughFunds,

	// More storage was created than allowed by the storage deposit limit.
	StorageDepositLimitExhausted,

	// Code removal was denied because the code is still in use by at least one contract.
	CodeInUse,

	// The contract ran to completion but decided to revert its storage changes.
	// Please note that this error is only returned from extrinsics. When called directly
	// or via RPC an `Ok` will be returned. In this case the caller needs to inspect the flags
	// to determine whether a reversion has taken place.
	ContractReverted,

	// The contract's code was found to be invalid during validation.
	//
	// The most likely cause of this is that an API was used which is not supported by the
	// node. This happens if an older node is used with a new version of ink!. Try updating
	// your node to the newest available version.
	//
	// A more detailed error can be found on the node console if debug messages are enabled
	// by supplying `-lruntime::contracts=debug`.
	CodeRejected,

	// An indetermistic code was used in a context where this is not permitted.
	Indeterministic,

	// A pending migration needs to complete before the extrinsic can be called.
	MigrationInProgress,

	// Migrate dispatch call was attempted but no migration was performed.
	NoMigrationPerformed,

	// The contract has reached its maximum number of delegate dependencies.
	MaxDelegateDependenciesReached,

	// The dependency was not found in the contract's delegate dependencies.
	DelegateDependencyNotFound,

	// The contract already depends on the given delegate dependency.
	DelegateDependencyAlreadyExists,

	// Can not add a delegate dependency to the code hash of the contract itself.
	CannotAddSelfAsDelegateDependency,
}