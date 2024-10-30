using System.Security.Cryptography;
using Blake2Fast;
using Nethereum.Util;
using Secp256k1Net;

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
        ErrorMessages.Add("Sr25519Verify not implemented.");
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
}