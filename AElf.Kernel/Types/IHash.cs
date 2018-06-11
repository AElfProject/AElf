using System;
using System.Collections.Generic;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    /// <summary>
    /// Hash result
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    public interface IHash : IEquatable<IHash>, IComparer<IHash>, IMessage
    {
        ByteString Value { get; set; }
    }

}