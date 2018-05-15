using System;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public class Batch : IBatch
    {
        private Dictionary<int, Job> _jobs = new Dictionary<int, Job>();
        private readonly Dictionary<Hash, UnionFindNode> _accountUnionFindSet = new Dictionary<Hash, UnionFindNode>();
        
        
        public List<Job> Jobs()
        {
            throw new System.NotImplementedException();
        }

        public void AddTransaction(ITransaction tx)
        {
            //union the connected component linked by the tx's input and output account
            if (!_accountUnionFindSet.TryGetValue(tx.From, out var fromNode))
            {
                fromNode = new UnionFindNode();
                _accountUnionFindSet.Add(tx.From, fromNode);
            }
            
            if (!_accountUnionFindSet.TryGetValue(tx.To, out var toNode))
            {
                toNode = new UnionFindNode();
                _accountUnionFindSet.Add(tx.To, toNode);
            }


            //Union the actual job according to the result of union operation
            if (UnionFindNode.GetNewRootIdAndDiscardedIdIfUnion(fromNode.NodeId, toNode.NodeId, out var newRoot, out var discardedId))
            {
                if(!_jobs.TryGetValue(newRoot, out var newRootJob))
                {
                    newRootJob = new Job();
                    _jobs.Add(newRoot, newRootJob);
                }
                if(_jobs.TryGetValue(discardedId, out var discardedJob))
                {
                    newRootJob.MergeJob(discardedJob);
                    _jobs.Remove(discardedId);
                }
            }
            
            fromNode.Union(toNode);
            
            //Add new tx into the unioned Job
            _jobs[newRoot].AddTx(tx);
        }
    }
}