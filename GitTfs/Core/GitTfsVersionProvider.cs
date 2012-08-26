using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class GitTfsVersionProvider : IGitTfsVersionProvider
    {
        private ITfsHelper tfsHelper;

        public GitTfsVersionProvider(ITfsHelper tfsHelper)
        {
            this.tfsHelper = tfsHelper;
        }

        public string GetVersionString()
        {
            var versionString = "git-tfs version";
            versionString += " " + GetType().Assembly.GetName().Version;
            versionString += GetGitCommitForVersionString();
            versionString += " (TFS client library " + tfsHelper.TfsClientLibraryVersion + ")";
            versionString += " (" + (Environment.Is64BitProcess ? "64-bit" : "32-bit") + ")";
            return versionString;
        }

        private string GetGitCommitForVersionString()
        {
            try
            {
                return " (" + GetGitCommit().Substring(0, 8) + ")";
            }
            catch (Exception e)
            {
                Trace.WriteLine("Unable to get git version: " + e);
                return "";
            }
        }

        private string GetGitCommit()
        {
            var gitTfsAssembly = GetType().Assembly;
            using (var head = gitTfsAssembly.GetManifestResourceStream("Sep.Git.Tfs.GitVersionInfo"))
            {
                var commitRegex = new Regex(@"commit (?<sha>[a-f0-9]{8})", RegexOptions.IgnoreCase);
                return commitRegex.Match(ReadAllText(head)).Groups["sha"].Value;
            }
        }

        private string ReadAllText(Stream stream)
        {
            return new StreamReader(stream).ReadToEnd().Trim();
        }
    }
}