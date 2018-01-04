using System;
using System.Collections.Generic;
using System.Text;
namespace AElf.Kernel
{
    /// <summary>
    /// Tree中的节点
    /// </summary>
    public class Mproof:Node
    {
        /// <summary>
        /// 从这个节点到根节点所有需要计算用到的朋友节点
        /// </summary>
        public List<Node> NodeList { get; set; }
        /// <summary>
        /// 这个节点自身的Hash值
        /// </summary>
        public string ProofHashValue { get; set; }
        /// <summary>
        /// 节点的唯一编码
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// 这个节点的下级节点
        /// </summary>
        public List<int> ChildCodeList { get; set; }
        //这个节点距离根节点有多远
        public int Distance { get; set; }
        //父级节点code
        public int ParentProofCode { get; set; }
        //朋友节点code
        public int FriendCode { get; set; }

        public void AddFriendNode(Node friendNode, Dictionary<int, Mproof> MProofDic,int friendCode)
        {
            this.FriendCode = friendCode;
            this.AddFriendNode(friendNode, MProofDic);
        }

        public void AddFriendNode(Node friendNode, Dictionary<int, Mproof> MProofDic)
        {
            if (this.NodeList == null)
                this.NodeList = new List<Node>();
            this.NodeList.Add(friendNode);
            this.Distance++;
            if (this.ChildCodeList != null && this.ChildCodeList.Count > 0)
            {
                foreach (int code in this.ChildCodeList)
                {
                    MProofDic[code].AddFriendNode(friendNode, MProofDic);
                }
            }
        }
        //更新朋友节点的值
        public void ModifyFriendNode(string newStr,int nodeIndex, Dictionary<int, Mproof> MProofDic)
        {
            if (this.NodeList != null && this.NodeList.Count > nodeIndex)
            {
                this.NodeList[nodeIndex].HashValue = newStr;
                if(this.ChildCodeList!=null && this.ChildCodeList.Count>0)
                {
                    nodeIndex++;
                    foreach (int code in this.ChildCodeList)
                    {
                        MProofDic[code].ModifyFriendNode(newStr,nodeIndex, MProofDic);
                    }
                }
                
            }
        }
    }
}
