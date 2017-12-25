using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/**
MerkleTest   understanding

1X1  2X2   roottreeloding
**/
namespace MerkleTest
{
    class Program
    {
        static List<String> stringList = new List<string>();
        static List<String> stringList2 = new List<string>();
        static void Main(string[] args)
        {
            Init();
        }

        private static void Init()
        {
            stringList.Clear();
            stringList2.Clear();
            Console.WriteLine("请输入第一个原始节点列表，各个元素用“,”分割");
            string inputString=Console.ReadLine();
            if (!string.IsNullOrEmpty(inputString))
            {
                string[] s = inputString.Split(',');
                foreach(string x in s)
                    stringList.Add(x);
            }
            Console.WriteLine("请输入第二个原始节点列表，各个元素用“,”分割");
            inputString = Console.ReadLine();
            if (!string.IsNullOrEmpty(inputString))
            {
                string[] s = inputString.Split(',');
                foreach (string x in s)
                    stringList2.Add(x);
            }
            
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("开始计算，请稍等");
            string rootHash1 = MerkleTrees(stringList);
            string rootHash2 = MerkleTrees(stringList2);
            Console.WriteLine("第一个根节点Hash是：" + rootHash1 + ",第二个根节点Hash是:" + rootHash2);
            stringList.Clear();
            stringList.Add(rootHash1);
            stringList.Add(rootHash2);
            rootHash1 = MerkleTrees(stringList);
            Console.WriteLine("更上层根节点Hash是：" + rootHash1 );
            Console.WriteLine(); Console.WriteLine();
            Init();


        }

        private static string MerkleTrees(List<string> xList )
        {
            string rootHash = CalculateMerkle(xList);
            return rootHash;
        }

        private static string CalculateMerkle(List<string> sList)
        {
            if(sList.Count==1)
            {
                return sList[0];
            }
            else
            {
                List<string> list = new List<string>();
                for(int i=0;i<sList.Count;)
                {

                    if(i+1>=sList.Count)
                    {
                        list.Add(CalcMd5(sList[i]));
                    }
                    else
                    {
                        list.Add(CalcMd5(sList[i]+sList[i+1]));
                    }

                    
                        i += 2;

                }
                return CalculateMerkle(list);
            }
        }

        private static String CalcMd5(String text)
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
