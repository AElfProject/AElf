using Wasmtime;

namespace AElf.Runtime.WebAssembly;

public partial class WebAssemblyContract
{
    private void DefineImportFunctions()
    {
        _linker.Define("env", "memory", _memory);
        _linker.DefineWasi();

        _linker.DefineFunction("seal0", "set_storage", (Action<int, int, int>)SetStorageV0);
        _linker.DefineFunction("seal1", "set_storage", (Func<int, int, int, int>)SetStorageV1);
        _linker.DefineFunction("seal2", "set_storage", (Func<int, int, int, int, int>)SetStorageV2);

        _linker.DefineFunction("seal0", "clear_storage", (Action<int>)ClearStorageV0);
        _linker.DefineFunction("seal1", "clear_storage", (Func<int, int, int>)ClearStorageV1);

        _linker.DefineFunction("seal0", "get_storage", (Func<int, int, int, int>)GetStorageV0);
        _linker.DefineFunction("seal1", "get_storage", (Func<int, int, int, int, int>)GetStorageV1);

        _linker.DefineFunction("seal0", "contains_storage", (Func<int, int>)ContainsStorageV0);
        _linker.DefineFunction("seal1", "contains_storage", (Func<int, int, int>)ContainsStorageV1);

        _linker.DefineFunction("seal0", "take_storage", (Func<int, int, int, int, int>)TakeStorageV0);

        _linker.DefineFunction("seal0", "transfer", (Func<int, int, int, int, int>)TransferV0);

        _linker.DefineFunction("seal0", "seal_call", (Func<int, int, long, int, int, int, int, int, int, int>)CallV0);
        _linker.DefineFunction("seal1", "seal_call", (Func<int, int, long, int, int, int, int, int, int>)CallV1);
        _linker.DefineFunction("seal2", "seal_call", (Func<int, int, long, long, int, int, int, int, int, int, int>)CallV2);

        _linker.DefineFunction("seal0", "delegate_call", (Func<int, int, int, int, int, int, int>)DelegateCallV0);

        _linker.DefineFunction("seal0", "instantiate", (caller, args, results) =>
            {
                InstantiateV0(args[0].AsInt32(), args[1].AsInt32(), args[2].AsInt64(), args[3].AsInt32(),
                    args[4].AsInt32(), args[5].AsInt32(), args[6].AsInt32(), args[7].AsInt32(), args[8].AsInt32(),
                    args[9].AsInt32(), args[10].AsInt32(), args[11].AsInt32(), args[12].AsInt32());
            },
            new[]
            {
                ValueKind.Int32, ValueKind.Int32, ValueKind.Int64, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32,
                ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32,
                ValueKind.Int32
            }, new List<ValueKind> { ValueKind.Int32 });
        _linker.DefineFunction("seal1", "instantiate",
            (Func<int, long, int, int, int, int, int, int, int, int, int, int>)InstantiateV1);
        _linker.DefineFunction("seal2", "instantiate", (caller, args, results) =>
            {
                InstantiateV2(args[0].AsInt32(), args[1].AsInt64(), args[2].AsInt64(), args[3].AsInt32(),
                    args[4].AsInt32(), args[5].AsInt32(), args[6].AsInt32(), args[7].AsInt32(), args[8].AsInt32(),
                    args[9].AsInt32(), args[10].AsInt32(), args[11].AsInt32(), args[12].AsInt32());
            },
            new[]
            {
                ValueKind.Int32, ValueKind.Int64, ValueKind.Int64, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32,
                ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32,
                ValueKind.Int32
            }, new List<ValueKind> { ValueKind.Int32 });

        _linker.DefineFunction("seal0", "terminate", (Action<int, int>)TerminateV0);
        _linker.DefineFunction("seal1", "terminate", (Action<int>)TerminateV1);

        _linker.DefineFunction("seal0", "input", (Action<int, int>)InputV0);

        _linker.DefineFunction("seal0", "seal_return", (Action<int, int, int>)SealReturnV0);

        _linker.DefineFunction("seal0", "caller", (Action<int, int>)Caller);

        _linker.DefineFunction("seal0", "is_contract", (Func<int, int>)IsContract);

        _linker.DefineFunction("seal0", "code_hash", (Func<int, int, int, int>)CodeHash);

        _linker.DefineFunction("seal0", "own_code_hash", (Action<int, int>)OwnCodeHash);

        _linker.DefineFunction("seal0", "caller_is_origin", CallerIsOrigin);

        _linker.DefineFunction("seal0", "caller_is_root", CallerIsRoot);

        _linker.DefineFunction("seal0", "address", (Action<int, int>)Address);

        _linker.DefineFunction("seal0", "weight_to_fee", (Action<long, int, int>)WeightToFeeV0);
        _linker.DefineFunction("seal1", "weight_to_fee", (Action<long, long, int, int>)WeightToFeeV1);

        _linker.DefineFunction("seal0", "gas_left", (Action<int, int>)GasLeftV0);
        _linker.DefineFunction("seal1", "gas_left", (Action<int, int>)GasLeftV1);

        _linker.DefineFunction("seal0", "balance", (Action<int, int>)Balance);

        _linker.DefineFunction("seal0", "value_transferred", (Action<int, int>)ValueTransferred);

        _linker.DefineFunction("seal0", "random", (Action<int, int, int, int>)RandomV0);
        _linker.DefineFunction("seal1", "random", (Action<int, int, int, int>)RandomV1);

        _linker.DefineFunction("seal0", "now", (Action<int, int>)Now);

        _linker.DefineFunction("seal0", "minimum_balance", (Action<int, int>)MinimumBalance);

        _linker.DefineFunction("seal0", "deposit_event", (Action<int, int, int, int>)DepositEvent);

        _linker.DefineFunction("seal0", "block_number", (Action<int, int>)BlockNumber);

        _linker.DefineFunction("seal0", "hash_sha2_256", (Action<int, int, int>)HashSha2_256);
        _linker.DefineFunction("seal0", "hash_keccak_256", (Action<int, int, int>)HashKeccak256);
        _linker.DefineFunction("seal0", "hash_blake2_256", (Action<int, int, int>)HashBlake2_256);
        _linker.DefineFunction("seal0", "hash_blake2_128", (Action<int, int, int>)HashBlake2_128);

        _linker.DefineFunction("seal0", "call_chain_extension", (Action<int, int, int, int, int>)CallChainExtension);

        _linker.DefineFunction("seal0", "debug_message", (Func<int, int, int>)DebugMessage);

        _linker.DefineFunction("seal0", "call_runtime", (Func<int, int, int>)CallRuntime);

        _linker.DefineFunction("seal0", "ecdsa_recover", (Func<int, int, int, int>)EcdsaRecover);

        _linker.DefineFunction("seal0", "sr25519_verify", (Func<int, int, int, int, int>)Sr25519Verify);

        _linker.DefineFunction("seal0", "set_code_hash", (Func<int, int>)SetCodeHash);

        _linker.DefineFunction("seal0", "ecdsa_to_eth_address", (Func<int, int, int>)EcdsaToEthAddress);

        _linker.DefineFunction("seal0", "reentrance_count", ReentranceCount);
        _linker.DefineFunction("seal0", "account_reentrance_count", (Func<int, int>)AccountReentranceCount);

        _linker.DefineFunction("seal0", "instantiation_nonce", InstantiationNonce);

        _linker.DefineFunction("seal0", "add_delegate_dependency", (Action<int>)AddDelegateDependency);
        _linker.DefineFunction("seal0", "remove_delegate_dependency", (Action<int>)RemoveDelegateDependency);
    }
}