namespace AElf.WebApp.Application.Chain.Dto
{
    public class GetTransactionPoolStatusOutput
    {
        public int Queued { get; set; }
        public int Validated { get; set; }
    }
}