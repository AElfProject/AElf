using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Domain
{
    public class ReturnSetCollection
    {
        private List<ExecutionReturnSet> _executed = new List<ExecutionReturnSet>();
        private List<ExecutionReturnSet> _unexecutable = new List<ExecutionReturnSet>();

        public List<ExecutionReturnSet> Executed => _executed;

        public List<ExecutionReturnSet> Unexecutable => _unexecutable;

        public ReturnSetCollection(IEnumerable<ExecutionReturnSet> returnSets)
        {
            AddRange(returnSets);
        }

        public void AddRange(IEnumerable<ExecutionReturnSet> returnSets)
        {
            foreach (var returnSet in returnSets)
            {
                if (returnSet.Status == TransactionResultStatus.Mined ||
                    returnSet.Status == TransactionResultStatus.Failed)
                {
                    _executed.Add(returnSet);
                }
                else if (returnSet.Status == TransactionResultStatus.Unexecutable)
                {
                    _unexecutable.Add(returnSet);
                }
            }
        }

        public BlockStateSet ToBlockStateSet()
        {
            var blockStateSet = new BlockStateSet();
            foreach (var returnSet in _executed)
            {
                foreach (var change in returnSet.StateChanges)
                {
                    blockStateSet.Changes[change.Key] = change.Value;
                    blockStateSet.Deletes.Remove(change.Key);
                }

                foreach (var delete in returnSet.StateDeletes)
                {
                    blockStateSet.Deletes.AddIfNotContains(delete.Key);
                    blockStateSet.Changes.Remove(delete.Key);
                }
            }

            return blockStateSet;
        }
    }
}