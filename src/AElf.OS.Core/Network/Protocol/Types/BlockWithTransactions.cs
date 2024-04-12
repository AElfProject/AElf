using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;

namespace AElf.OS.Network;

public partial class BlockWithTransactions : IBlock, ICustomDiagnosticMessage
{
    public IEnumerable<Transaction> FullTransactionList => Transactions;

    public long Height => Header?.Height ?? 0;
    public IEnumerable<Hash> TransactionIds => Transactions.Select(tx => tx.GetHash());

    public BlockBody Body => new()
    {
        TransactionIds = { Transactions.Select(tx => tx.GetHash()).ToList() }
    };

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

    partial void OnConstruction()
    {
        Header = new BlockHeader();
    }
}