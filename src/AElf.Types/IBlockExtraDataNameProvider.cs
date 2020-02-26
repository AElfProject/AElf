namespace AElf
{
    /// <summary>
    /// The meaning of using an interface to manage block extra name provider
    /// is to enable going through all the implementations one by one.
    /// </summary>
    public interface IBlockExtraDataNameProvider
    {
        string ExtraDataName { get; }
    }
}