using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Concurrency
{

    public class ParallelGroupService : IParallelGroupService
    {
        /// <summary>
        /// Produce the sub group where conflicting tx inside same group so that there will be no conflict among tx that belong to different groups.
        /// <para>
        /// Strategy:
        ///     1. First TODO: Use MetaData to divide the tx into group where different groups use different data.
        ///     1. Second divide by account, those tx that may modify same accounts' data will be in same group
        ///     
        /// </para>
        /// </summary>
        /// <param name="txList"></param>
        /// <returns></returns>
        public List<ITransactionParallelGroup> ProduceGroup(Dictionary<Hash, List<ITransaction>> txList)
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
        public List<ITransactionParallelGroup> MergeAccountTxList(Dictionary<Hash, List<ITransaction>> txList)
        {
            if (txList.Count == 0)
            {
                return new List<ITransactionParallelGroup>();
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
            
			Dictionary<int, ITransactionParallelGroup> grouped = new Dictionary<int, ITransactionParallelGroup>();         
			foreach(var senderTxList in txList){
				int nodeId = accountUnionSet[senderTxList.Key].Find().NodeId;
				if(!grouped.TryGetValue(nodeId, out var paraGroup)){
				    paraGroup = new TransactionParallelGroup();
					grouped.Add(nodeId, paraGroup);
                }
			    paraGroup.AddAccountTxList(senderTxList);
			}
         
			return grouped.Values.ToList();
       }
        
    }
}