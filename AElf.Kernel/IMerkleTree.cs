namespace AElf.Kernel
{
    public class MerkleTree
    {
        public Dictionary<int, Mproof> MProofDic;
        Mproof lastChildProof;
        int currentCode = 0;
        public string root;
        public MerkleTree(List<string> baseChildValues)
        {
            MProofDic = new Dictionary<int, Mproof>();
            if (baseChildValues.Count>0)
            {
                for(int i=0;i<baseChildValues.Count;i++)
                {
                    Mproof proof = new Mproof() { ProofHashValue = CalcMd5(baseChildValues[i]), Code = i+1 ,ChildCodeList=new List<int>()};
                    MProofDic.Add(i+1, proof);
                    lastChildProof = proof;
                }
                
            }
            currentCode = baseChildValues.Count+1;

            CreatTree();
        }


        /// <summary>
        /// 构建树
        /// </summary>
        void CreatTree()
        {
            List<Mproof> proofList = MProofDic.Values.ToList();
            CalculateMerkle(proofList);
        }


        private void CalculateMerkle(List<Mproof> proofList)
        {

            if (proofList.Count == 1)
            {
                root = proofList[0].ProofHashValue;
                return ;
            }
            else
            {
                List<Mproof> newProofList = new List<Mproof>();
                for (int i = 0; i < proofList.Count; i+=2)
                {
                    Mproof newProof ;
                    if (i + 1 >= proofList.Count)
                    {
                        newProof = NewProofBySelf(proofList[i]);
                    }
                    else
                    {
                        newProof = CombinTowProof(proofList[i], proofList[i+1]);
                    }
                    newProofList.Add(newProof);
                    MProofDic.Add(newProof.Code, newProof);

                }
                CalculateMerkle(newProofList);
            }
        }


        /// <summary>
        /// 验证值可靠性
        /// </summary>
        /// <returns></returns>

        public bool CheckProof(Mproof checkedProof, string rootValue)
        {
            if (checkedProof.NodeList.Count != checkedProof.Distance)
                return false;
            string src = checkedProof.ProofHashValue;
            for (int i = 0; i < checkedProof.Distance;i++ )
            {
                src = checkedProof.NodeList[i].Side == 1 ? src + checkedProof.NodeList[i].HashValue : checkedProof.NodeList[i].HashValue + src;
                src = CalcMd5(src);
            }

            return src==rootValue;
        }


        public void AddChildValue(string newChildValue)
        {
            Mproof proof = new Mproof() { ProofHashValue = CalcMd5(newChildValue), Code = currentCode++,ChildCodeList=new List<int>() };
            MProofDic.Add(proof.Code, proof);
            AddMProof(lastChildProof,proof);
            lastChildProof = proof;
        }

        void AddMProof(Mproof previProof ,Mproof proof)
        {
            if (previProof.FriendCode > 0)
            {
                //最后一个子节点是已经组合的节点
                Mproof newProof = NewProofBySelf(proof);
                MProofDic.Add(newProof.Code, newProof);
                AddMProof(MProofDic[previProof.ParentProofCode], newProof);
            }
            else
            {
                //最后一个子节点是单数,可以和proof进行组合
                if (previProof.ParentProofCode == 0)
                {
                    //组合之后形成的新节点就是新的根节点
                    Mproof newProof = CombinTowProof(previProof, proof);
                    MProofDic.Add(newProof.Code, newProof);
                    root = newProof.ProofHashValue;
                    //退出
                    return;
                }
                else
                {
                    //修改previProof的父节点
                    previProof.AddFriendNode(new Node(){Side=1,HashValue=proof.ProofHashValue},MProofDic);
                    proof.AddFriendNode(new Node(){Side=2,HashValue=previProof.ProofHashValue},MProofDic);
                    proof.ParentProofCode = previProof.ParentProofCode;
                    MProofDic[previProof.ParentProofCode].ChildCodeList.Add(proof.Code);

                    string newStr = previProof.ProofHashValue + proof.ProofHashValue;
                    newStr = CalcMd5(newStr);
                    ModifyChild(MProofDic[previProof.ParentProofCode], newStr);
                }
            }
        }

        /// <summary>
        /// 两个节点组合成为一个新节点
        /// </summary>
        /// <param name="previProof"></param>
        /// <param name="proof"></param>
        Mproof CombinTowProof(Mproof previProof, Mproof proof)
        {
            Mproof newProof = new Mproof() { Code = currentCode++, ChildCodeList = new List<int>()};
            string newStr = previProof.ProofHashValue + proof.ProofHashValue;
            newProof.ProofHashValue = CalcMd5(newStr);
            newProof.ChildCodeList.Add(previProof.Code);
            newProof.ChildCodeList.Add(proof.Code);
            previProof.AddFriendNode(new Node() { Side = 1, HashValue = proof.ProofHashValue }, MProofDic, proof.Code);
            previProof.ParentProofCode = newProof.Code;
            proof.AddFriendNode(new Node() { Side = 2, HashValue = previProof.ProofHashValue }, MProofDic,previProof.Code);
            proof.ParentProofCode = newProof.Code;
            return newProof;
        }

        //没有可以组合的只能自己创建自己的父节点
        Mproof NewProofBySelf(Mproof proof)
        {
            Mproof newProof = new Mproof() { ProofHashValue = CalcMd5(proof.ProofHashValue), Code = currentCode++, ChildCodeList =new List<int>()};
            newProof.ChildCodeList.Add(proof.Code);
            proof.ParentProofCode = newProof.Code;
            proof.Distance++;
            return newProof;
        }

        //更新节点
        public void ModifyChild(int code ,string newValue)
        {
            if(MProofDic.ContainsKey(code))
            {
                ModifyChild(MProofDic[code], newValue);
            }
        }
        void ModifyChild(Mproof proof, string newValue)
        {
            proof.ProofHashValue = newValue;
            //跟新朋友节点中存的value值
            if(proof.FriendCode>0)
            {
                Mproof friendProof = MProofDic[proof.FriendCode];
                friendProof.ModifyFriendNode(newValue, 0, MProofDic);
            }
            if(proof.ParentProofCode>0)
            {
                //计算新的父节点值
                if(proof.FriendCode>0)
                {
                    Node friendNode = proof.NodeList[0];
                    newValue = friendNode.Side == 1 ? newValue + friendNode.HashValue : friendNode.HashValue + newValue;
                }
                newValue = CalcMd5(newValue);
                //跟新父节点
                ModifyChild(MProofDic[proof.ParentProofCode], newValue);
            }
            else
            {
                root = newValue;
            }
            
        }
        public String CalcMd5(String text)
        {
            // using System.Security.Cryptography; 使用加密库
            String md5 = "";
            MD5 md5_text = MD5.Create();
            byte[] temp = md5_text.ComputeHash(System.Text.Encoding.ASCII.GetBytes(text)); //计算MD5 Hash 值

            for (int i = 0; i < temp.Length; i++)
            {
                md5 += temp[i].ToString("x2"); //转码 每两位转一次16进制
            }
            return md5;
        }

    }
} 