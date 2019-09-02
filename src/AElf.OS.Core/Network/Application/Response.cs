namespace AElf.OS.Network.Application
{
    public class Response<T>
    {
        public bool Success { get; }
        public T Payload { get; }

        public Response(T payload)
        {
            Success = true;
            Payload = payload;
        }
    }
}