namespace AElf.CSharp.CodeOps.Patchers
{
    public interface IPatcher<T>
    {
        void Patch(T item);
    }
}
