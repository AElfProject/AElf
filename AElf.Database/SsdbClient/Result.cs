namespace AElf.Database.SsdbClient
{
    
    public class Result<T>
    {
        public bool Success { get; set; }

        public string ErrMsg { get; set; }

        public T Data { get; set; }
    }

    public static class ResultUtility<T>
    {
        public static Result<T> Success(T data)
        {
            return new Result<T> {Data = data};
        }

        public static Result<T> Failed(string message)
        {
            return new Result<T> {Success = false, ErrMsg = message};
        }
    }
}