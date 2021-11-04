namespace AElf.Types
{
    /// <summary>
    /// Initial version: add, sub, mul, div, mod
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IInteger<T> where T : IInteger<T>
    {
        void Add(in T a, out T res);
        void Sub(in T a, out T res);
        void Mul(in T a, out T res);
        void Div(in T a, out T res);
        void Mod(in T a, out T res);
    }
}