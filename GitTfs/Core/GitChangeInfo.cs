using System.Diagnostics;
using System.Text.RegularExpressions;
using GitSharp.Core;
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
            "(?<srcpath>[^\\t]+)" +
            "(\\t" +
            "(?<dstpath>[^\\t]+)" +
            ")?" +
            "$",
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

        private readonly Match _match;

        private GitChangeInfo(Match match)
        {
            _match = match;
        }

        public FileMode NewMode { get { return _match.Groups["dstmode"].Value.ToFileMode(); } }
        public string Status { get { return _match.Groups["status"].Value; } }

        public ExplicitArgsExpression Merge(ExplicitArgsExpression builder)
        {
            return builder
                .With("oldMode").EqualTo(_match.Groups["srcmode"].Value)
                .With("newMode").EqualTo(_match.Groups["dstmode"].Value)
                .With("oldSha").EqualTo(_match.Groups["srcsha1"].Value)
                .With("newSha").EqualTo(_match.Groups["dstsha1"].Value)
                .With("path").EqualTo(_match.Groups["srcpath"].Value)
                .With("pathTo").EqualTo(_match.Groups["dstpath"].Value)
                .With("score").EqualTo(_match.Groups["score"].Value);
        }
    }
}