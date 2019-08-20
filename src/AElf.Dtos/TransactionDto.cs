namespace AElf.Dtos
{
    public class TransactionDto
    {
        public string From { get; set; }
        
        public string To { get; set; }
        
        public long RefBlockNumber { get; set; }
        
        public string RefBlockPrefix { get; set; }
        
        public string MethodName { get; set; }
        
        public string Params { get; set; }
        
        public string Signature { get; set; }
    }
}