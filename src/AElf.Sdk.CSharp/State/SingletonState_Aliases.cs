using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class BoolState : SingletonState<bool>
    {
    }

    public class Int32State : SingletonState<int>
    {
    }

    public class UInt32State : SingletonState<uint>
    {
    }

    public class Int64State : SingletonState<long>
    {
    }

    public class UInt64State : SingletonState<ulong>
    {
    }

    public class StringState : SingletonState<string>
    {
    }

    public class BytesState : SingletonState<byte[]>
    {
    }

    // ReSharper disable once IdentifierTypo
    public class ProtobufState<TEntity> : SingletonState<TEntity> where TEntity : IMessage, new()
    {
    }
}