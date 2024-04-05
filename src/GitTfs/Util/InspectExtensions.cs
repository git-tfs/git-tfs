using System.Collections;
using System.Text.RegularExpressions;

namespace GitTfs.Extensions
{
    public static class InspectExtensions
    {
        public static string Inspect(this object obj) => Inspect(obj, d => d.Inspect(), e => e.Inspect(), o => InspectWithProperties(o));

        public static string Inspect(this char o) => "'" + o + "'";

        public static string Inspect(this string o)
        {
            if (o == null) return Inspect((object)o);
            return "\"" + o + "\"";
        }

        public static string Inspect(this IDictionary o)
        {
            if (o == null) return Inspect((object)o);
            var entries = o.Keys.Cast<object>().Select(key => InspectSimple(key) + " => " + InspectSimple(o[key]));
            return "{" + string.Join(", ", entries.ToArray()) + "}";
        }

        public static string Inspect(this IEnumerable o)
        {
            if (o == null) return Inspect((object)o);
            return "[" + string.Join(", ", o.Cast<object>().Select(obj => InspectSimple(obj)).ToArray()) + "]";
        }

        private static string InspectWithProperties(object o)
        {
            var inspected = "#<";
            inspected += InspectType(o.GetType());
            inspected += string.Join(",",
                                     o.GetType().GetProperties().Select(p => " " + p.Name + "=" + InspectSimple(p.GetValue(o, null)))
                                         .ToArray());
            inspected += ">";
            return inspected;
        }

        private static string InspectSimple(object obj) => Inspect(obj, d => "{...}", e => "[...]",
                           o => "#<" + InspectType(o.GetType()) + ":0x" + o.GetHashCode().ToString("x") + ">");

        private static string Inspect(object o,
            Func<IDictionary, string> inspectDictionary,
            Func<IEnumerable, string> inspectEnumerable,
            Func<object, string> inspectObject)
        {
            if (o == null) return "null";
            if (ShouldUseToString(o)) return o.ToString();
            if (o is Enum) return o.ToString().Replace(", ", "|");
            if (o is String) return ((String)o).Inspect();
            if (o is char) return ((char)o).Inspect();
            if (CanInspect(o)) return CallInspect(o);
            if (o is IDictionary) return inspectDictionary((IDictionary)o);
            if (o is IEnumerable) return inspectEnumerable((IEnumerable)o);
            return inspectObject(o);
        }

        private static bool ShouldUseToString(object o) => o is int ||
                   o is long ||
                   o is float ||
                   o is double ||
                   o is decimal ||
                   o is byte ||
                   o is DateTime;

        private static bool CanInspect(object o)
        {
            var inspectMethod = o.GetType().GetMethod("Inspect", new Type[0]);
            return inspectMethod != null && inspectMethod.ReturnType == typeof(String);
        }

        private static string CallInspect(object o)
        {
            var inspectMethod = o.GetType().GetMethod("Inspect", new Type[0]);
            return (string)inspectMethod.Invoke(o, null);
        }

        private static string InspectType(Type type) => IsAnonymousType(type) ? InspectAnonymousType(type) : type.IsGenericType ? InspectGenericType(type) : type.FullName;

        private static bool IsAnonymousType(Type type) => new Regex("^<>f__AnonymousType").IsMatch(type.FullName);

        private static string InspectAnonymousType(Type type) => "anon";

        private static string InspectGenericType(Type type)
        {
            var name = Undecorate(type.FullName);
            name += "<";
            name += string.Join(",", type.GetGenericArguments().Select(t => InspectType(t)).ToArray());
            name += ">";
            return name;
        }

        private static string Undecorate(string genericTypeName)
        {
            var decorationMatcher = new Regex(@"`\d+\[\[[^\]]+\](,\[[^\]]+\])*\]");
            return decorationMatcher.Replace(genericTypeName, "");
        }
    }
}
