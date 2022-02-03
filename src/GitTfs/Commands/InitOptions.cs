using System;
using NDesk.Options;
using GitTfs.Util;

namespace GitTfs.Commands
{
    [StructureMapSingleton]
    public class InitOptions
    {
        private const string DefaultAutocrlf = "false";

        public InitOptions() { GitInitAutoCrlf = DefaultAutocrlf; }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "template=", "Passed to git-init",
                        v => GitInitTemplate = v },
                    { "shared:", "Passed to git-init",
                        v => GitInitShared = v == null ? (object)true : (object)v },
                    { "initial-branch=", "Passed to git-init (requires Git >= 2.28.0)",
                        v => GitInitDefaultBranch = v },
                    { "autocrlf=", "Normalize line endings (default: " + DefaultAutocrlf + ")",
                        v => GitInitAutoCrlf = ValidateCrlfValue(v) },
                    { "ignorecase=", "Ignore case in file paths (default: system default)",
                        v => GitInitIgnoreCase = ValidateIgnoreCaseValue(v) },
                    {"bare", "Clone the TFS repository in a bare git repository", v => IsBare = v != null},
                    {"workspace=", "Set tfs workspace to a specific folder (a shorter path is better!)", v => WorkspacePath = v},
                };
            }
        }

        private string ValidateCrlfValue(string v)
        {
            string[] valid = { "false", "true", "auto" };
            if (!Array.Exists(valid, s => v == s))
                throw new OptionException("error: autocrlf value must be one of true, false or auto", "autocrlf");
            return v;
        }

        private string ValidateIgnoreCaseValue(string v)
        {
            string[] valid = { "false", "true" };
            if (!Array.Exists(valid, s => v == s))
                throw new OptionException("error: ignorecase value must be true or false", "ignorecase");
            return v;
        }


        public bool IsBare { get; set; }
        public string WorkspacePath { get; set; }
        public string GitInitTemplate { get; set; }
        public object GitInitShared { get; set; }
        public string GitInitDefaultBranch { get; set; }
        public string GitInitAutoCrlf { get; set; }
        public string GitInitIgnoreCase { get; set; }
    }
}
