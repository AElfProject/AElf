using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public interface IEvent<T> : IMessage<T> where T : IEvent<T>
    {
        IEnumerable<T> GetIndexed();
        T GetNonIndexed();
    }
}