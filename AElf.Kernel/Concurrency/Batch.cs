using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Concurrency
{
    public class Batch : IBatch
    {
        private readonly Dictionary<int, Job> _jobs = new Dictionary<int, Job>();
        private readonly Dictionary<Hash, UnionFindNode> _accountUnionFindSet = new Dictionary<Hash, UnionFindNode>();
        private readonly HashSet<Hash> _senderSet = new HashSet<Hash>();
        
        public void AddTransaction(ITransaction tx)
        {
            //A batch contains at most one tx from each sender
            if (_senderSet.Contains(tx.From))
            {
                throw new Exception("Try to add another transaction sent by " + tx.From + " where this batch already contains a tx sent by this account");
            }
            _senderSet.Add(tx.From);
            
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
            if (UnionFindNode.GetNewRootIdAndDiscardedIdIfUnion(fromNode.Find().NodeId, toNode.Find().NodeId, out var newRoot, out var discardedId))
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

        public List<Job> Jobs => _jobs.Values.ToList();

        public IEnumerator<Job> GetEnumerator()
        {
            return _jobs.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}