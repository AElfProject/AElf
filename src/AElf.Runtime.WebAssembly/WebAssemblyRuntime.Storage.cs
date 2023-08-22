using Volo.Abp.Threading;

namespace AElf.Runtime.WebAssembly;

public partial class WebAssemblyRuntime
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

    #endregion

    private byte[] DecodeKey(Key key, int keyPtr)
    {
        return ReadBytes(keyPtr, key.KeyType == KeyType.Fix ? 32 : key.KeyValue.Length);
    }

    private int SetStorage(Key key, int keyPtr, int valuePtr, int valueLen)
    {
        var keyBytes = DecodeKey(key, keyPtr);
        var value = ReadBytes(valuePtr, valueLen);
        var writeOutcome = _externalEnvironment.SetStorage(keyBytes, value, false);
        return writeOutcome.OldLenWithSentinel();
    }

    public WriteOutcome SetStorage(byte[] key, byte[] value, bool takeOld)
    {
        return _externalEnvironment.SetStorage(key, value, takeOld);
    }

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
        throw new NotImplementedException();
    }

    /// <summary>
    /// Clear the value at the given key in the contract storage.
    /// </summary>
    /// <param name="keyPtr">pointer into the linear memory where the key is placed.</param>
    /// <param name="keyLen">the length of the key in bytes.</param>
    /// <returns></returns>
    private int ClearStorageV1(int keyPtr, int keyLen)
    {
        throw new NotImplementedException();
    }

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
        throw new NotImplementedException();
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
        Console.WriteLine($"{keyPtr}, {keyLen}, {outPtr}, {outLenPtr}");
        var key = ReadBytes(keyPtr, keyLen);
        var outcome = AsyncHelper.RunSync(() => _externalEnvironment.GetStorageAsync(key));
        if (outcome != null)
        {
            Console.WriteLine($"GetStorage Success: \nKey: {key} \nValue: {outcome}");
            WriteBytes(outPtr, outcome);
            WriteUInt32(outLenPtr, Convert.ToUInt32(outcome?.Length));
            return (int)ReturnCode.Success;
        }

        return (int)ReturnCode.KeyNotFound;
    }

    #endregion

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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

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
        throw new NotImplementedException();
    }
}