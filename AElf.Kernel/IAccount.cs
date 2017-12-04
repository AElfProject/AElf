using System.Numerics;

namespace AElf.Kernel
{
    using AddressType = IHash<IAccount>;
    using ValueType = System.Numerics.BigInteger;

    /// <summary>
    /// Every smart contract was an account
    /// </summary>
    public interface IAccount
    {
        /// <summary>
        /// Get Account's Address, the address is the id for a account
        /// </summary>
        /// <returns></returns>
        AddressType GetAddress();
    }
}