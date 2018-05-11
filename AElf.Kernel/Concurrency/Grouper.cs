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
        public List<TransactionParallelGroup> ProduceGroup(Dictionary<Hash, List<Transaction>> txList)
        {
            var groupsByAccount = MergeAccountTxList(txList);

            return groupsByAccount;
        }

        /// <summary>
        /// Divide the txList into groups according to accounts.
        /// Basic idea: build a undirected graph where vertex is account and edge is transaction, and those tx that their related account belongs to the same connected component are in same group.
        /// 
        /// <remarks>
        /// WARNING : This solution is under assumption that the TX only have ONE input account and ONE output account, if this assumption is not satified, further solution wiil be needed.
        /// <para>
        /// Solution without fully consideration: if tx have inputAccountList and outputAccountList, just union all the input account to make them all connectted.
        ///     e.g.: t_1 has input account {A, G} and output account {B, H}, t_2 has input {B} and output {C}, t_3 has input {H} and output {D}
        ///         if don't connect A+G, then we have two connectted components {A-B-C} and {G-H-D}, so the t_3 will not be in same group with t_1+t_2, which is not accepted.
        ///         Hence, we connect A+G, i.e. consider input account in the same tx is also dependent, making this account graph connected and solve the problem 
        /// </para>
        /// </remarks>
        /// </summary>
        /// <param name="txDict">Dictionary of Transaction list sent by accounts</param>
        /// <returns></returns>
        public List<TransactionParallelGroup> MergeAccountTxList(Dictionary<Hash, List<Transaction>> txList)
        {
            if (txList.Count == 0)
            {
                return new List<TransactionParallelGroup>();
            }
            Dictionary<Hash, UnionFindNode> accountUnionSet = new Dictionary<Hash, UnionFindNode>();
            
            //set up the union find set
            foreach (var accountTxList in txList.Values)
            {
                foreach (var tx in accountTxList)
                {
                    if (!accountUnionSet.TryGetValue(tx.From, out var fromNode))
                    {
                        fromNode = new UnionFindNode();
                        accountUnionSet.Add(tx.From, fromNode);
                    }

                    if (!accountUnionSet.TryGetValue(tx.To, out var toNode))
                    {
                        toNode = new UnionFindNode();
                        accountUnionSet.Add(tx.To, toNode);
                    }
                    
                    fromNode.Union(toNode);
                }
            }

            //set up the result group and init the first group
            var groupList = new List<TransactionParallelGroup>();
            bool firstElement = true;
            //if two txs' account in the same set, then these two are in the same group
            foreach (var accountTxList in txList)
            {
                if (firstElement)
                {
                    var firstGroup = new TransactionParallelGroup();
                    firstGroup.AddAccountTxList(accountTxList);
                    groupList.Add(firstGroup);
                    firstElement = false;
                }
                else
                {
                    bool createNewGroup = true;
                    foreach (var group in groupList)
                    {
                        if (accountUnionSet[accountTxList.Key].IsUnionedWith(accountUnionSet[group.GetOneAccountInGroup()]))
                        {
                            group.AddAccountTxList(accountTxList);
                            createNewGroup = false;
                        }
                    }

                    if (createNewGroup)
                    {
                        var newGroup = new TransactionParallelGroup();
                        newGroup.AddAccountTxList(accountTxList);
                        groupList.Add(newGroup);
                    }
                }
            }
    
            return groupList;
        }
        
        
    }
}