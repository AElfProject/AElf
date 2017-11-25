using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ITransaction
    {
        /// <summary>
        /// Get hash of the transaction
        /// </summary>
        /// <returns></returns>
        IHash GetHash();
    }
}