namespace AElf.CSharp.CodeOps.Patchers
{
    public interface IPatcher
    {
        bool SystemContactIgnored { get; }
    }

    public interface IPatcher<T> : IPatcher
    {
        void Patch(T item);
    }
}