namespace AElf.WebApp.Application.Chain.Dto
{
    public class SendRawTransactionInput
    {
        public string Transaction { get; set; }
        
        public string Signature { get; set; }
    }
}