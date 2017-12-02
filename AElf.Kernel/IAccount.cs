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
        /// Gets the balance of this account
        /// </summary>
        /// <returns>The balance.</returns>
        ValueType GetBalance();

        /// <summary>
        /// Gets the total token supply.
        /// </summary>
        /// <returns>The total token supply.</returns>
        ValueType GetTotalTokenSupply();

        /// <summary>
        /// Gets the token balance of.
        /// </summary>
        /// <returns>The token balance of.</returns>
        /// <param name="owner">Owner Address.</param>
        ValueType GetTokenBalanceOf(AddressType owner);

        /// <summary>
        /// Transfers the token.
        /// </summary>
        /// <returns><c>true</c>, if token was transfered, <c>false</c> otherwise.</returns>
        /// <param name="to">To.</param>
        /// <param name="value">Value.</param>
        bool TransferToken(AddressType to, ValueType value);

        /// <summary>
        /// Transfers the token from one address to another if approved
        /// </summary>
        /// <returns><c>true</c>, if token from was transfered, <c>false</c> otherwise.</returns>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="value">Value.</param>
        bool TransferTokenFrom(AddressType from, AddressType to, ValueType value);

        /// <summary>
        /// Approve the specified spender of value.
        /// </summary>
        /// <returns>The approve.</returns>
        /// <param name="spender">Spender.</param>
        /// <param name="_value">Value.</param>
        bool ApproveToken(AddressType spender, ValueType value);

        /// <summary>
        /// Gets the allowance token.
        /// </summary>
        /// <returns>The allowance token.</returns>
        /// <param name="owner">Owner.</param>
        /// <param name="spender">Spender.</param>
        ValueType GetAllowanceToken(AddressType owner, AddressType spender);
    }
}