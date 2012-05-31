using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public class GitChangeInfo
    {
        public static readonly Regex DiffTreePattern = new Regex(
            // See http://www.kernel.org/pub/software/scm/git/docs/git-diff-tree.html#_raw_output_format
            "^:" +
            "(?<srcmode>[0-7]{6})" +
            " " +
            "(?<dstmode>[0-7]{6})" +
            " " +
            "(?<srcsha1>[0-9a-f]{40})" +
            " " +
            "(?<dstsha1>[0-9a-f]{40})" +
            " " +
            "(?<status>.)" + "(?<score>[0-9]*)" +
            "\\t" +
            "(?<srcquot>\"?)(?<srcpath>[^\\t]+?)\"?" +
            "(\\t" +
            "(?<dstquot>\"?)(?<dstpath>[^\\t]+?)\"?" +
            ")?" +
            "$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex unicodePattern = new Regex(@"\\[0-7]{3}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static GitChangeInfo Parse(string diffTreeLine)
        {
            var match = DiffTreePattern.Match(diffTreeLine);
            Debug(diffTreeLine, match, DiffTreePattern);
            return new GitChangeInfo(match);
        }

        private static void Debug(string input, Match match, Regex regex)
        {
            Trace.WriteLine("Regex: " + regex);
            Trace.WriteLine("Input: " + input);
            foreach(var groupName in regex.GetGroupNames())
            {
                Trace.WriteLine(" -> " + groupName + ": >>" + match.Groups[groupName].Value + "<<");
            }
        }

        private static string replaceUnicode(Match match)
        {
            return char.ConvertFromUtf32(Convert.ToInt32(match.Value.Substring(1), 8));
        }

        private static string getPathFromMatch(Match match, string quotName, string pathName)
        {
            if (String.IsNullOrEmpty(match.Groups[quotName].Value))
                return match.Groups[pathName].Value;
            else
                return unicodePattern.Replace(match.Groups[pathName].Value, replaceUnicode);
        }

        private readonly Match _match;
        private readonly string _srcpath;
        private readonly string _dstpath;

        private GitChangeInfo(Match match)
        {
            _match = match;
            _srcpath = getPathFromMatch(match, "srcquot", "srcpath");
            _dstpath = getPathFromMatch(match, "dstquot", "dstpath");
        }

        public LibGit2Sharp.Mode NewMode { get { return _match.Groups["dstmode"].Value.ToFileMode(); } }
        public string Status { get { return _match.Groups["status"].Value; } }

        public string oldMode { get { return _match.Groups["srcmode"].Value; } }
        public string newMode { get { return _match.Groups["dstmode"].Value; } }
        public string oldSha  { get { return _match.Groups["srcsha1"].Value; } }
        public string newSha  { get { return _match.Groups["dstsha1"].Value; } }
        public string path    { get { return _srcpath; } }
        public string pathTo  { get { return _dstpath; } }
        public string score   { get { return _match.Groups["score"].Value; } }

        public IGitChangedFile ToGitChangedFile(ExplicitArgsExpression builder)
        {
            return builder.With(this).GetInstance<IGitChangedFile>(Status);
        }
    }
}
