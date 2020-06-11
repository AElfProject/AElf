namespace AElf.CSharp.CodeOps.Patchers
{
    public interface IPatcher
    {
    }

    public interface IPatcher<T> : IPatcher
    {
        void Patch(T item);
    }
}