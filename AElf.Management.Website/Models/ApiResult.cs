namespace AElf.Management.Website.Models
{
    public class ApiResult
    {
        public int Code { get; set; }

        public string Msg { get; set; }

        public ApiResult()
        {
            Code = 0;
            Msg = "ok";
        }
    }
}