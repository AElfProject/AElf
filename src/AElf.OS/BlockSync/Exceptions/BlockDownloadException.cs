using System;
using AElf.Types;

namespace AElf.OS.BlockSync.Exceptions;

public class BlockDownloadException : Exception
{
    public BlockDownloadException()
    {
    }

    public BlockDownloadException(string message) : base(message)
    {
    }

    public BlockDownloadException(string message, Exception inner) : base(message, inner)
    {
    }

    public BlockDownloadException(Hash blockHash, long blockHeight, string peerPubkey) : this(
        $"Download block exception, block hash = {blockHash}, block height = {blockHeight}, peer pubkey = {peerPubkey}.")
    {
        BlockHash = blockHash;
        BlockHeight = blockHeight;
        PeerPubkey = peerPubkey;
    }

    public Hash BlockHash { get; }
    public long BlockHeight { get; }
    public string PeerPubkey { get; }
}