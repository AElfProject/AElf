namespace AElf.WebApp.Application.Chain.Dto
{
    public class GetTransactionPoolStatusOutput
    {
        public int AllTransactionCount { get; set; }
        public int ValidatedTransactionCount { get; set; }
    }
}