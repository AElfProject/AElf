namespace AElf.Kernel.Blockchain.Application;

public interface IBlockExtraDataProvider
{
    string BlockHeaderExtraDataKey { get; }

    /// <summary>
    ///     Get extra data from corresponding services.
    /// </summary>
    /// <param name="blockHeader"></param>
    /// <returns></returns>
    Task<ByteString> GetBlockHeaderExtraDataAsync(BlockHeader blockHeader);
}