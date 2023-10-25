using AElf.Contracts.MultiToken;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.WebAssembly;

public partial class WebAssemblyContract
{
    #region SetStorage

    /// <summary>
    /// Set the value at the given key in the contract storage.
    ///
    /// Equivalent to the newer [`seal1`][`super::api_doc::Version1::set_storage`] version with the
    /// exception of the return type. Still a valid thing to call when not interested in the return
    /// value.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the location to store the value is placed.</param>
    /// <param name="valuePtr">pointer into the linear memory where the value to set is placed.</param>
    /// <param name="valueLen">the length of the value in bytes.</param>
    private void SetStorageV0(int keyPtr, int valuePtr, int valueLen)
    {
        SetStorage(new Key { KeyType = KeyType.Fix }, keyPtr, valuePtr, valueLen);
    }

    /// <summary>
    /// Set the value at the given key in the contract storage.
    ///
    /// This version is to be used with a fixed sized storage key. For runtimes supporting
    /// transparent hashing, please use the newer version of this function.
    ///
    /// The value length must not exceed the maximum defined by the contracts module parameters.
    /// Specifying a `valueLen` of zero will store an empty value.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the location to store the value is placed.</param>
    /// <param name="valuePtr">pointer into the linear memory where the value to set is placed.</param>
    /// <param name="valueLen">the length of the value in bytes.</param>
    /// <returns>
    /// Returns the size of the pre-existing value at the specified key if any. Otherwise
    /// `SENTINEL` is returned as a sentinel value.
    /// </returns>
    private int SetStorageV1(int keyPtr, int valuePtr, int valueLen)
    {
        return SetStorage(new Key { KeyType = KeyType.Fix }, keyPtr, valuePtr, valueLen);
    }

    /// <summary>
    /// Set the value at the given key in the contract storage.
    ///
    /// The key and value lengths must not exceed the maximums defined by the contracts module
    /// parameters. Specifying a `valueLen` of zero will store an empty value.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the location to store the value is placed.</param>
    /// <param name="keyLen">the length of the key in bytes.</param>
    /// <param name="valuePtr">pointer into the linear memory where the value to set is placed.</param>
    /// <param name="valueLen">the length of the value in bytes.</param>
    /// <returns>
    /// Returns the size of the pre-existing value at the specified key if any. Otherwise
    /// `SENTINEL` is returned as a sentinel value.
    /// </returns>
    private int SetStorageV2(int keyPtr, int keyLen, int valuePtr, int valueLen)
    {
        Console.WriteLine($"SetStorage: {keyPtr}, {keyLen}, {valuePtr}, {valueLen}");
        var key = new Key { KeyType = KeyType.Var, KeyValue = new byte[keyLen] };
        return SetStorage(key, keyPtr, valuePtr, valueLen);
    }

    private byte[] DecodeKey(Key key, int keyPtr)
    {
        return ReadSandboxMemory(keyPtr, key.KeyType == KeyType.Fix ? 32 : key.KeyValue.Length);
    }

    private int SetStorage(Key key, int keyPtr, int valuePtr, int valueLen)
    {
        var keyBytes = DecodeKey(key, keyPtr);
        var value = ReadSandboxMemory(valuePtr, valueLen);
        var writeOutcome = SetStorage(keyBytes, new BytesValue { Value = ByteString.CopyFrom(value) }, false);
        return writeOutcome.OldLenWithSentinel();
    }

    private WriteOutcome SetStorage(byte[] key, BytesValue? value, bool takeOld)
    {
        WriteOutcome writeOutcome;
        var keyHash = HashHelper.ComputeFrom(key);
        var oldValue = State.Database[keyHash];
        if (oldValue != null)
        {
            if (takeOld)
            {
                writeOutcome = new WriteOutcome
                {
                    WriteOutcomeType = WriteOutcomeType.Taken,
                    Value = oldValue.Value.ToHex()
                };
            }
            else
            {
                var length = oldValue.Value.Length.ToString();
                if (oldValue.Value.ToHex().All(c => c == '0'))
                {
                    length = "0";
                }

                writeOutcome = new WriteOutcome
                {
                    WriteOutcomeType = WriteOutcomeType.Overwritten,
                    Value = length
                };
            }

            if (value != null)
            {
                State.Database[keyHash] = value;
            }
            else
            {
                State.Database.Remove(keyHash);
            }
        }
        else
        {
            writeOutcome = new WriteOutcome { WriteOutcomeType = WriteOutcomeType.New };
            if (value != null)
            {
                State.Database[keyHash] = value;
            }
        }

        return writeOutcome;
    }

    #endregion

    #region ClearStorage

    /// <summary>
    /// Clear the value at the given key in the contract storage.
    ///
    /// Equivalent to the newer [`seal1`][`super::api_doc::Version1::clear_storage`] version with
    /// the exception of the return type. Still a valid thing to call when not interested in the
    /// return value.
    /// </summary>
    /// <param name="keyPtr"></param>
    private void ClearStorageV0(int keyPtr)
    {
        ClearStorage(keyPtr, 32);
    }

    /// <summary>
    /// Clear the value at the given key in the contract storage.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the key is placed.</param>
    /// <param name="keyLen">the length of the key in bytes.</param>
    /// <returns></returns>
    private int ClearStorageV1(int keyPtr, int keyLen)
    {
        return ClearStorage(keyPtr, keyLen);
    }

    private int ClearStorage(int keyPtr, int keyLen)
    {
        var key = ReadSandboxMemory(keyPtr, keyLen);
        var writeOutcome = SetStorage(key, null, false);
        return writeOutcome.OldLenWithSentinel();
    }

    #endregion

    #region GetStorage

    /// <summary>
    /// Retrieve the value under the given key from storage.
    ///
    /// This version is to be used with a fixed sized storage key. For runtimes supporting
    /// transparent hashing, please use the newer version of this function.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the key of the requested value is placed.</param>
    /// <param name="outPtr">pointer to the linear memory where the value is written to.</param>
    /// <param name="outLenPtr">
    /// in-out pointer into linear memory where the buffer length is read from and
    /// the value length is written to.
    /// </param>
    /// <returns>ReturnCode</returns>
    private int GetStorageV0(int keyPtr, int outPtr, int outLenPtr)
    {
        return GetStorage(keyPtr, 32, outPtr, outLenPtr);
    }

    /// <summary>
    /// Retrieve the value under the given key from storage.
    ///
    /// This version is to be used with a fixed sized storage key. For runtimes supporting
    /// transparent hashing, please use the newer version of this function.
    ///
    /// The key length must not exceed the maximum defined by the contracts module parameter.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the key of the requested value is placed.</param>
    /// <param name="keyLen">the length of the key in bytes.</param>
    /// <param name="outPtr">pointer to the linear memory where the value is written to.</param>
    /// <param name="outLenPtr">
    /// in-out pointer into linear memory where the buffer length is read from and
    /// the value length is written to.
    /// </param>
    /// <returns>ReturnCode</returns>
    private int GetStorageV1(int keyPtr, int keyLen, int outPtr, int outLenPtr)
    {
        return GetStorage(keyPtr, keyLen, outPtr, outLenPtr);
    }

    private int GetStorage(int keyPtr, int keyLen, int outPtr, int outLenPtr)
    {
        var key = ReadSandboxMemory(keyPtr, keyLen);
        var outcome = ReadState(key);
        if (outcome.Value.Length == 0)
        {
            return (int)ReturnCode.KeyNotFound;
        }

        ReturnBuffer = outcome.ToByteArray();
        WriteSandboxOutput(outPtr, outLenPtr, outcome.ToByteArray());
        return (int)ReturnCode.Success;
    }

    #endregion

    #region ContainsStorage

    /// <summary>
    /// Checks whether there is a value stored under the given key.
    ///
    /// This version is to be used with a fixed sized storage key. For runtimes supporting
    /// transparent hashing, please use the newer version of this function.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the key of the requested value is placed.</param>
    /// <returns>
    /// Returns the size of the pre-existing value at the specified key if any. Otherwise
    /// `SENTINEL` is returned as a sentinel value.
    /// </returns>
    private int ContainsStorageV0(int keyPtr)
    {
        return ContainsStorage(keyPtr, AElfConstants.HashByteArrayLength);
    }

    /// <summary>
    /// Checks whether there is a value stored under the given key.
    ///
    /// The key length must not exceed the maximum defined by the contracts module parameter.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the key of the requested value is placed.</param>
    /// <param name="keyLen">the length of the key in bytes.</param>
    /// <returns>
    /// Returns the size of the pre-existing value at the specified key if any. Otherwise
    /// `SENTINEL` is returned as a sentinel value.
    /// </returns>
    private int ContainsStorageV1(int keyPtr, int keyLen)
    {
        return ContainsStorage(keyPtr, keyLen);
    }

    private int ContainsStorage(int keyPtr, int keyLen)
    {
        var key = ReadSandboxMemory(keyPtr, keyLen);
        var outcome = ReadState(key);
        return outcome.Value.Length;
    }

    #endregion

    #region TakeStorage

    /// <summary>
    /// Retrieve and remove the value under the given key from storage.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the key of the requested value is placed.</param>
    /// <param name="keyLen">the length of the key in bytes.</param>
    /// <param name="outPtr">pointer to the linear memory where the value is written to.</param>
    /// <param name="outLenPtr">
    /// in-out pointer into linear memory where the buffer length is read from and
    /// the value length is written to.
    /// </param>
    /// <returns>ReturnCode</returns>
    private int TakeStorageV0(int keyPtr, int keyLen, int outPtr, int outLenPtr)
    {
        if (keyLen > WebAssemblyRuntimeConstants.MaxStorageKeyLength)
        {
            HandleError(WebAssemblyError.DecodingFailed);
            return (int)ReturnCode.CallRuntimeFailed;
        }

        var key = ReadSandboxMemory(keyPtr, keyLen);
        var writeOutcome = SetStorage(key, null, true);
        if (writeOutcome.WriteOutcomeType == WriteOutcomeType.Taken)
        {
            WriteSandboxOutput(outPtr, outLenPtr, ByteArrayHelper.HexStringToByteArray(writeOutcome.Value));
            return (int)ReturnCode.Success;
        }

        return (int)ReturnCode.KeyNotFound;
    }

    #endregion

    private BytesValue ReadState(byte[] key)
    {
        return State.Database[HashHelper.ComputeFrom(key)];
    }
}