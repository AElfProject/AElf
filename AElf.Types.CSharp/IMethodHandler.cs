namespace AElf.Types.CSharp
{
    public interface IMethodHandler
    {
        byte[] Execute(byte[] paramsBytes);
        string BytesToString(byte[] bytes);
        object BytesToReturnType(byte[] bytes);
    }
}