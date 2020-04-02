using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    /// <summary>
    /// Wrapper around boolean values for use in smart contract state.
    /// </summary>
    public class BoolState : SingletonState<bool>
    {
    }

    /// <summary>
    /// Wrapper around 32-bit integer values for use in smart contract state.
    /// </summary>
    public class Int32State : SingletonState<int>
    {
    }

    /// <summary>
    /// Wrapper around unsigned 32-bit integer values for use in smart contract state.
    /// </summary>
    public class UInt32State : SingletonState<uint>
    {
    }

    /// <summary>
    /// Wrapper around 64-bit integer values for use in smart contract state.
    /// </summary>
    public class Int64State : SingletonState<long>
    {
    }

    /// <summary>
    /// Wrapper around unsigned 64-bit integer values for use in smart contract state.
    /// </summary>
    public class UInt64State : SingletonState<ulong>
    {
    }

    /// <summary>
    /// Wrapper around string values for use in smart contract state.
    /// </summary>
    public class StringState : SingletonState<string>
    {
    }

    /// <summary>
    /// Wrapper around byte arrays for use in smart contract state.
    /// </summary>
    public class BytesState : SingletonState<byte[]>
    {
    }

    // ReSharper disable once IdentifierTypo
    public class ProtobufState<TEntity> : SingletonState<TEntity> where TEntity : IMessage, new()
    {
    }
}