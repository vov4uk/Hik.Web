using System;

namespace Job.Extensions
{
    public static class TypeExtentions
    {
        public static bool IsInheritedFrom(this Type type, Type Lookup)
        {
            var baseType = type.BaseType;
            if (baseType == null)
                return false;

            if (baseType.IsGenericType
                    && baseType.GetGenericTypeDefinition() == Lookup)
                return true;

            return baseType.IsInheritedFrom(Lookup);
        }
    }
}
