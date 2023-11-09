using System.Security.Cryptography;
using Blake2Fast;
using Nethereum.Util;

namespace AElf.Runtime.WebAssembly.Contract;

public partial class WebAssemblyContractImplementation
{
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
        var output = new Sha3Keccack().CalculateHash(input);
        WriteSandboxMemory(outputPtr, output);
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
        var output = Blake2b.ComputeHash(AElfConstants.HashByteArrayLength, input);
        WriteSandboxMemory(outputPtr, output);
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
}