namespace AElf.Dtos
{
    public class SendRawTransactionInput
    {
        /// <summary>
        /// raw transaction
        /// </summary>
        public string Transaction { get; set; }
        
        /// <summary>
        /// signature
        /// </summary>
        public string Signature { get; set; }
        
        /// <summary>
        /// return transaction detail or not
        /// </summary>
        public bool ReturnTransaction { get; set; }
    }
}