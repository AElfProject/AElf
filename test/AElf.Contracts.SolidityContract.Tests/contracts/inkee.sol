// ink! contracts are expected to return a value of type `Result<T, LangError>` (Rust's "Result" type).
// The ink! call builder only supports decoding the result into this `Result` type.
// This works fine for ink! because ink! contracts are supposed to always return this type.
// 
// In Solidity, not only does this type not exist, but also the concept of a "LangError" is inexistent.
// Hence we mock it, so we can (ab)use the ink! call builder for setting up the call to our contract.
// In SCALE, a preceding null-byte will decode to the `Ok` variant.
struct InkResult {
    uint8 ok;
    uint32 value;
}

// Helper function to convert the result value into an ink! compatible result type.
function ink_result(uint32 _value) pure returns (InkResult) {
    return InkResult({ok: 0, value: _value});
}

contract Inkee {
    // We use this selector just for convenience
    @selector([1, 2, 3, 4])
    function echo(uint32 v) public pure returns (InkResult) {
        return ink_result(v);
    }
}
