namespace AElf.Dtos
{
    public class SendRawTransactionOutput
    {
        public string TransactionId { get; set; }
        
        public TransactionDto Transaction { get; set; }
    }
}