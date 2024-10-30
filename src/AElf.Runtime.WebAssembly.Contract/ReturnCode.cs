namespace AElf.Runtime.WebAssembly.Contract;

public enum ReturnCode
{
    // API call successful.
    Success = 0,

    // The called function trapped and has its state changes reverted.
    // In this case no output buffer is returned.
    CalleeTrapped = 1,

    // The called function ran to completion but decided to revert its state.
    // An output buffer is returned when one was supplied.
    CalleeReverted = 2,

    // The passed key does not exist in storage.
    KeyNotFound = 3,

    // Performing the requested transfer failed. Probably because there isn't enough
    // free balance in the sender's account.
    TransferFailed = 5,

    // No code could be found at the supplied code hash.
    CodeNotFound = 7,

    // The contract that was called is no contract (a plain account).
    NotCallable = 8,

    // The call dispatched by `seal_call_runtime` was executed but returned an error.
    CallRuntimeFailed = 10,

    // ECDSA pubkey recovery failed (most probably wrong recovery id or signature), or
    // ECDSA compressed pubkey conversion into Ethereum address failed (most probably
    // wrong pubkey provided).
    EcdsaRecoverFailed = 11,

    // sr25519 signature verification failed.
    Sr25519VerifyFailed = 12,
    
    Exception = 13
}