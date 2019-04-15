using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.CSharp.Core
{
    public interface IEvent<T> : IMessage<T> where T : IEvent<T>
    {
        IEnumerable<T> GetIndexed();
        T GetNonIndexed();
    }
}