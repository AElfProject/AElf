using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Kernel.Consensus;

namespace AElf.ChainController.TxMemPoolBM
{
    // ReSharper disable InconsistentNaming
    public class DPoSTxFilter
    {
        private readonly Round _currentRoundInfo;
        private readonly Hash _myAddress;
        private readonly Func<List<Transaction>, List<Transaction>> _txFilter;

        private readonly Func<List<Transaction>, List<Transaction>> _singleBPFilter = list =>
        {
            var toRemove = new List<Transaction>();
            if (list.Any(tx => tx.MethodName == ConsensusBehavior.UpdateAElfDPoS.ToString()))
            {

            }

            return toRemove;
        };

        public DPoSTxFilter(Round currentRoundInfo, Hash myAddress, bool multipleNodes = true)
        {
            _currentRoundInfo = currentRoundInfo;
            _myAddress = myAddress;
            if (multipleNodes)
            {
                
            }
            else
            {
                _txFilter += _singleBPFilter;
            }
        }

        public List<Transaction> Execute(List<Transaction> txs)
        {
            var filterList = _txFilter.GetInvocationList();
            var toRemove = new List<Transaction>();
            foreach (var @delegate in filterList)
            {
                var filter = (Func<List<Transaction>, List<Transaction>>) @delegate;
                try
                {
                    toRemove.AddRange(filter(txs));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return toRemove;
        }
    }
}