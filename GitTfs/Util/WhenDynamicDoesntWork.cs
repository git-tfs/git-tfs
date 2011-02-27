using System.Reflection;

namespace Sep.Git.Tfs.Util
{
    public static class WhenDynamicDoesntWork
    {
        public static T Call<T>(this object o, string method, params object[] args)
        {
            return (T)o.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, o, args);
        }
    }
}