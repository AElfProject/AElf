namespace AElf.Kernel
{
    /// <summary>
    /// meikletree中的每一个节点基类
    /// </summary>
    public class Node
    {
        public int Side { get; set; }//1表示在右，2表示在左
        public string HashValue { get; set; }//这个节点的Hash值
    }
}
