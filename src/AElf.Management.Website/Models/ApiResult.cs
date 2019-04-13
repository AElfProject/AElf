namespace AElf.Management.Website.Models
{
    public interface IApiResult
    {
        int Code { get; set; }

        string Msg { get; set; }
    }

    public class ApiResult<T> : IApiResult
    {
        public int Code { get; set; }

        public string Msg { get; set; }

        public T Data { get; set; }

        public ApiResult()
        {
            Code = 0;
            Msg = "ok";
        }

        public ApiResult(T data)
        {
            Code = 0;
            Msg = "ok";
            Data = data;
        }

        public ApiResult(int code, string msg)
        {
            Code = code;
            Msg = msg;
        }
    }

    public class ApiEmptyResult : IApiResult
    {
        public int Code { get; set; }

        public string Msg { get; set; }

        public static ApiEmptyResult Default = new ApiEmptyResult();

        public ApiEmptyResult()
        {
            Code = 0;
            Msg = "ok";
        }

        public ApiEmptyResult(int code, string msg)
        {
            Code = code;
            Msg = msg;
        }
    }
}