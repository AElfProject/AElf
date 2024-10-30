using AElf.Kernel;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Contract;

public partial class WebAssemblyContractImplementation
{
    /// <summary>
    /// Instantiate a contract with the specified code hash.
    /// <br/>
    /// <b>New version available</b>
    /// <br/>
    /// This is equivalent to calling the newer version of this function. The newer version
    /// drops the now unnecessary length fields.
    /// <br/>
    /// <b>Note</b>
    /// <br/>
    /// The values `_code_hash_len` and `_value_len` are ignored because the encoded sizes
    /// of those types are fixed through [`codec::MaxEncodedLen`]. The fields exist
    /// for backwards compatibility. Consider switching to the newest version of this function.
    /// </summary>
    /// <returns>ReturnCode</returns>
    private int InstantiateV0(int codeHashPtr, int codeHashLen, long gas, int valuePtr, int valueLen, int inputDataPtr,
        int inputDataLen, int addressPtr, int addressLenPtr, int outputPtr, int outputLenPtr, int saltPtr,
        int saltLen)
    {
        return (int)Instantiate(codeHashPtr, new Weight(gas, 0), null, valuePtr,
            inputDataPtr, inputDataLen, addressPtr, addressLenPtr, outputPtr, outputLenPtr, saltPtr, saltLen);
    }

    /// <summary>
    /// Instantiate a contract with the specified code hash.
    ///
    /// Equivalent to the newer [`seal2`][`super::api_doc::Version2::instantiate`] version but works
    /// with *ref_time* Weight only. It is recommended to switch to the latest version, once it's
    /// stabilized.
    /// </summary>
    /// <returns>ReturnCode</returns>
    private int InstantiateV1(int codeHashPtr, long gas, int valuePtr, int inputDataPtr, int inputDataLen,
        int addressPtr, int addressLenPtr, int outputPtr, int outputLenPtr, int saltPtr, int saltLen)
    {
        return (int)Instantiate(codeHashPtr, new Weight(gas, 0), null, valuePtr,
            inputDataPtr, inputDataLen, addressPtr, addressLenPtr, outputPtr, outputLenPtr, saltPtr, saltLen);
    }

    /// <summary>
    /// Instantiate a contract with the specified code hash.
    /// <br/>
    /// This function creates an account and executes the constructor defined in the code specified
    /// by the code hash. The address of this new account is copied to `address_ptr` and its length
    /// to `address_len_ptr`. The constructors output buffer is copied to `output_ptr` and its
    /// length to `output_len_ptr`. The copy of the output buffer and address can be skipped by
    /// supplying the sentinel value of `SENTINEL` to `output_ptr` or `address_ptr`.
    /// </summary>
    /// <param name="codeHashPtr">a pointer to the buffer that contains the initializer code.</param>
    /// <param name="refTimeLimit">how much <b>ref_time</b> Weight to devote to the execution.</param>
    /// <param name="proofSizeLimit">how much <b>proof_size</b> Weight to devote to the execution.</param>
    /// <param name="depositPtr">
    /// a pointer to the buffer with value of the storage deposit limit for the
    /// call. Should be decodable as a `T::Balance`. Traps otherwise. Passing `SENTINEL` means
    /// setting no specific limit for the call, which implies storage usage up to the limit of the
    /// parent call.
    /// </param>
    /// <param name="valuePtr">
    /// a pointer to the buffer with value, how much value to send. Should be
    /// decodable as a `T::Balance`. Traps otherwise.
    /// </param>
    /// <param name="inputDataPtr">a pointer to a buffer to be used as input data to the initializer code.</param>
    /// <param name="inputDataLen">length of the input data buffer.</param>
    /// <param name="addressPtr">a pointer where the new account's address is copied to. `SENTINEL` means not to copy.</param>
    /// <param name="addressLenPtr">pointer to where put the length of the address.</param>
    /// <param name="outputPtr">a pointer where the output buffer is copied to. `SENTINEL` means not to copy.</param>
    /// <param name="outputLenPtr">in-out pointer to where the length of the buffer is read from and the actual length is written to.</param>
    /// <param name="saltPtr">Pointer to raw bytes used for address derivation. See `fn contract_address`.</param>
    /// <param name="saltLen">length in bytes of the supplied salt.</param>
    /// <returns></returns>
    private int InstantiateV2(int codeHashPtr, long refTimeLimit, long proofSizeLimit, int depositPtr, int valuePtr,
        int inputDataPtr, int inputDataLen, int addressPtr, int addressLenPtr, int outputPtr, int outputLenPtr,
        int saltPtr, int saltLen)
    {
        return (int)Instantiate(codeHashPtr, new Weight(refTimeLimit, proofSizeLimit), depositPtr, valuePtr,
            inputDataPtr, inputDataLen, addressPtr, addressLenPtr, outputPtr, outputLenPtr, saltPtr, saltLen);
    }

    private ReturnCode Instantiate(int codeHashPtr, Weight weight, int? depositPtr, int valuePtr, int inputDataPtr,
        int inputDataLen, int addressPtr, int addressLenPtr, int outputPtr, int outputLenPtr, int saltPtr, int saltLen)
    {
        var depositLimit = depositPtr == null ? 0 : ReadSandboxMemory(depositPtr.Value, 8).ToInt64(false);
        var value = ReadSandboxMemory(valuePtr, 8).ToInt64(false);
        var codeHash = ReadSandboxMemory(codeHashPtr, AElfConstants.HashByteArrayLength).ToHash();
        var inputData = ReadSandboxMemory(inputDataPtr, inputDataLen);
        var salt = ReadSandboxMemory(saltPtr, saltLen);
        var (address, output) = Instantiate(weight, depositLimit, codeHash, value, inputData, salt);
        if (output.Flags != ReturnFlags.Revert)
        {
            WriteSandboxOutput(addressPtr, addressLenPtr, address.ToByteArray(), true);
        }

        WriteSandboxOutput(outputPtr, outputLenPtr, output.Data, true);
        return output.ToReturnCode();
    }

    private (Address, ExecuteReturnValue) Instantiate(Weight gasLimit, long depositLimit, Hash codeHash, long value,
        byte[] inputData, byte[] salt)
    {
        CustomPrints.Add("Instantiate.");
        ChargeGas(new Instantiate());
        var parameter = new byte[inputData.Length - 4];
        Array.Copy(inputData, 4, parameter, 0, parameter.Length);
        var addressBytes = Context.CallMethod(Context.Sender, Context.GetZeroSmartContractAddress(),
            nameof(State.SolidityContractManager.InstantiateSoliditySmartContract), new InstantiateSoliditySmartContractInput
            {
                CodeHash = codeHash,
                Category = KernelConstants.WasmRunnerCategory,
                Salt = ByteString.CopyFrom(salt)
            }.ToByteString());
        var contractAddress = new Address();
        contractAddress.MergeFrom(addressBytes);
        // Execute the constructor manually.
        Context.CallMethod(Context.Self, contractAddress, "deploy", ByteString.CopyFrom(parameter));
        return (contractAddress, new ExecuteReturnValue
        {
            Data = addressBytes
        });
    }
}