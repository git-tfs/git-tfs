using System;
using System.Dynamic;
using System.Reflection;

namespace Sep.Git.Tfs.VsCommon
{
    public class ReflectionProxy : DynamicObject
    {
        private readonly Type _reflectedType;
        private readonly object _reflectedObject;

        public ReflectionProxy(Type reflectedType, params object[] constructorArgs)
        {
            if (reflectedType == null) throw new ArgumentNullException("reflectedType");

            _reflectedType = reflectedType;
            _reflectedObject = Activator.CreateInstance(reflectedType, constructorArgs);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            try
            {
                result = _reflectedType.InvokeMember(binder.Name, BindingFlags.InvokeMethod, null, _reflectedObject,
                                                     args);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }

        }
    }
}