namespace AElf.Node.RPC.DTO
{
    public class TransactionDto
    {
        public byte[] From { get; set; }
        public byte[] To { get; set; }
        public string Method { get; set; }
        public ulong IncrementId { get; set; }
        public object[] Params { get; set; }
    }
}