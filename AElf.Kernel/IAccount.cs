using System.Numerics;

namespace AElf.Kernel
{
    /// <summary>
    /// Every smart contract was an account
    /// </summary>
    public interface IAccount
    {
        /// <summary>
        /// Get Account's Address, the address is the id for a account
        /// </summary>
        /// <returns></returns>
        IHash<IAccount> GetAddress();

        /// <summary>
        /// Invoke the specified methodName and values.
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="methodName">Method name.</param>
        /// <param name="values">Values.</param>
        void Invoke(string methodName, params string[] values);
    }
}