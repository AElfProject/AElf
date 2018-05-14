using System.Linq;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Change
    {
        public void AddHashBefore(Hash hash)
        {
            if (hash == null)
                return;
            
            befores_.Add(hash);
        }

        /// <summary>
        /// Before update the After, should add After to Befores.
        /// </summary>
        /// <param name="hash"></param>
        public void UpdateHashAfter(Hash hash)
        {
            AddHashBefore(after_);
            after_ = hash;
        }

        public void ClearChangeBefores()
        {
            befores_.Clear();
        }

        public Hash GetLastHashBefore()
        {
            return befores_.Last();
        }
    }
}