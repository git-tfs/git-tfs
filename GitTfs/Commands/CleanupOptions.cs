﻿using NDesk.Options;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [StructureMapSingleton]
    public class CleanupOptions
    {
        private readonly Globals _globals;

        public CleanupOptions(Globals globals)
        {
            _globals = globals;
        }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "v|verbose", v => IsVerbose = v != null },
                };
            }
        }

        private bool IsVerbose { get; set; }

        public void Init()
        {
            if (IsVerbose)
                _globals.DebugOutput = true;
        }
    }
}
