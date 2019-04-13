namespace AElf.WebApp.Application.Chain.Dto
{
    public class TransactionDto
    {
        public string From { get; set; }
        
        public string To { get; set; }
        
        public long RefBlockNumber { get; set; }
        
        public string RefBlockPrefix { get; set; }
        
        public string MethodName { get; set; }
        
        public string Params { get; set; }
        
        public string[] Sigs { get; set; }
    }
}