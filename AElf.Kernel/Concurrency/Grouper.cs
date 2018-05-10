using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public class Grouper : IGrouper
    {
        /// <summary>
        /// Produce the sub group where conflicting tx inside same group so that there will be no conflict among tx that belong to different groups.
        /// <para>
        /// Strategy:
        ///     1. first divide by account, those tx that may modify same accounts' data will be in same group
        ///     TODO: Use MetaData to further divide the groups.
        /// </para>
        /// </summary>
        /// <param name="txList"></param>
        /// <returns></returns>
        public List<List<Transaction>> ProduceGroup(List<Transaction> txList)
        {
            var groupsByAccount = MergeByAccount(txList);

            return groupsByAccount;
        }

        /// <summary>
        /// Divide the txList into groups according to accounts.
        /// Basic idea: build a undirected graph where vertex is account and edge is transaction, and those tx that their related account belongs to the same connected component are in same group.
        /// 
        /// <remarks>WARNING : This solution is under assumption that the TX only have ONE input account and ONE output account, if this assumption is not satified, further solution wiil be needed. </remarks>
        /// </summary>
        /// <param name="txList">Transaction list</param>
        /// <returns></returns>
        public List<List<Transaction>> MergeByAccount(List<Transaction> txList)
        {
			if(txList.Count == 0) return new List<List<Transaction>>();
         
            Dictionary<Hash, UnionFindNode> accountUnionSet = new Dictionary<Hash, UnionFindNode>();
            
            //set up the union find set
            foreach (var tx in txList)
            {
				if (!accountUnionSet.TryGetValue(tx.From, out var fromNode))
                {
					fromNode = new UnionFindNode();
                }

				if (!accountUnionSet.TryGetValue(tx.To, out var toNode))
                {
					toNode = new UnionFindNode();
                }

				fromNode.Union(toNode);
            }

            //set up the result group and init the first group
            var groups = new List<List<Transaction>>();
            groups.Add(new List<Transaction>());
            groups[0].Add(txList[0]);

            //if two txs' account in the same set, then these two are in the same group
            for(int index = 1; index < txList.Count; index++)
            {
                var tx = txList[index];
                bool createNewGroup = true;
                foreach (var group in groups)
                {
                    if (accountUnionSet[tx.From].IsUnionedWith(accountUnionSet[group[0].From]))
                    {
                        group.Add(tx);
                        createNewGroup = false;
                    }
                }

                if (createNewGroup)
                {
                    var newGroup = new List<Transaction>();
                    newGroup.Add(tx);
                    groups.Add(newGroup);
                }
            }
    
            return groups;
        }
        
        
    }
}