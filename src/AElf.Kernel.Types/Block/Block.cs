using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel;

public partial class Block : ICustomDiagnosticMessage, IBlock
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="T:AElf.Kernel.Block" /> class.
    ///     A previous block must be referred, except for the genesis block.
    /// </summary>
    /// <param name="preBlockHash">Pre block hash.</param>
    public Block(Hash preBlockHash)
    {
        Header = new BlockHeader(preBlockHash);
        Body = new BlockBody();
    }

    public long Height => Header?.Height ?? 0;

    public IEnumerable<Hash> TransactionIds => Body.TransactionIds;


    public Hash GetHash()
    {
        return Header.GetHash();
    }

    /// <summary>
    ///     Used to override IMessage's default string representation.
    /// </summary>
    /// <returns></returns>
    public string ToDiagnosticString()
    {
        return $"{{ id: {GetHash()}, height: {Height} }}";
    }

    public Hash GetHashWithoutCache()
    {
        return Header.GetHashWithoutCache();
    }
}