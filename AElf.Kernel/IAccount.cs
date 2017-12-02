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

        /// <summary>
        /// Gets the total supply.
        /// </summary>
        /// <returns>The total supply.</returns>
        ValueType GetTotalSupply();

        /// <summary>
        /// Gets the balance of.
        /// </summary>
        /// <returns>The balance of.</returns>
        /// <param name="owner">Owner.</param>
        ValueType GetBalanceOf(AddressType owner);

        /// <summary>
        /// Transfer the specified to and value.
        /// </summary>
        /// <returns>The transfer.</returns>
        /// <param name="to">To.</param>
        /// <param name="value">Value.</param>
        bool Transfer(AddressType to, ValueType value);

        /// <summary>
        /// Transfers from.
        /// </summary>
        /// <returns><c>true</c>, if from was transfered, <c>false</c> otherwise.</returns>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="value">Value.</param>
        bool TransferFrom(AddressType from, AddressType to, ValueType value);

        /// <summary>
        /// Approve the specified spender and value.
        /// </summary>
        /// <returns>The approve.</returns>
        /// <param name="spender">Spender.</param>
        /// <param name="value">Value.</param>
        bool Approve(AddressType spender, ValueType value);

        /// <summary>
        /// Gets the allowance.
        /// </summary>
        /// <returns>The allowance.</returns>
        /// <param name="owner">Owner.</param>
        /// <param name="spender">Spender.</param>
        ValueType GetAllowance(AddressType owner, AddressType spender);
    }
}