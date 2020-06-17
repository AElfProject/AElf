using AElf.Sdk.CSharp;

namespace AElf.CSharp.CodeOps.Patchers.Module
{
    public static class StateRestrictionProxy
    {
        public static object LimitStateSize(object obj)
        {
            if (SerializationHelper.Serialize(obj).Length > 128 * 1024)
                throw new AssertionException("State size limited.");
            return obj;
        }
    }
}