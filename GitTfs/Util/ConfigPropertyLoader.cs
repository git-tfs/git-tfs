using System;
using System.Collections.Generic;

namespace Sep.Git.Tfs.Util
{
    // Manages configurable values.
    [StructureMapSingleton]
    public class ConfigPropertyLoader
    {
        Globals _globals;
        Dictionary<string, object> _overrides = new Dictionary<string, object>();

        public ConfigPropertyLoader(Globals globals)
        {
            _globals = globals;
        }

        // Sets a value for the duration of this run of git-tfs.
        public void Override<T>(string key, T value)
        {
            _overrides[key] = value;
        }

        // Gets the value to use. Order of precedence:
        //
        // * temporary value, set with Override<T>().
        // * configured value, retrieved via git. This value is set external to any invocation of git-tfs.
        // * a default value, provided in the call to Get().
        public T Get<T>(string key, T defaultValue)
        {
            if (_overrides.ContainsKey(key))
                return (T) _overrides[key];

            var configEntry = globals.Repository.Get<T>(key);
            if (configEntry != null)
                return configEntry.Value;

            return defaultValue;
        }
    }
}
