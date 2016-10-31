using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using StructureMap;
using FileMode = LibGit2Sharp.Mode;

namespace Sep.Git.Tfs.Core
{
    public class GitChangeInfo
    {
        public struct ChangeType
        {
            public const string ADD = "A";
            public const string COPY = "C";
            public const string MODIFY = "M";
            public const string DELETE = "D";
            public const string RENAMEEDIT = "R";
            //public const string TYPECHANGE="T";
            //public const string UNMERGED="U";
            //public const string UNKNOWN="X";
        }

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
            "\\000" +
            "(?<srcpath>[^\\000]+)" +
            "(\\000" +
            "(?<dstpath>[^\\000]+)" +
            ")?" +
            "\\000$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static IEnumerable<GitChangeInfo> GetChangedFiles(TextReader reader)
        {
            string line;
            while (null != (line = GetDiffTreeLine(reader)))
            {
                var change = Parse(line);

                if (FileMode.GitLink == change.NewMode)
                    continue;

                yield return change;
            }
        }

        private static string GetDiffTreeLine(TextReader reader)
        {
            var sb = new StringBuilder();

            char[] block = new char[98];
            var bytesRead = reader.ReadBlock(block, 0, 98);
            if (bytesRead == 0)
            {
                return null;
            }
            else if (bytesRead != 98)
            {
                throw new Exception("Invalid input.");
            }

            var nullBytesLeft = 2;
            if (block[97] == 'C' || block[97] == 'R')
            {
                nullBytesLeft = 3;
            }

            sb.Append(block);

            while (nullBytesLeft > 0)
            {
                var currentByte = reader.Read();
                if (currentByte == -1)
                {
                    throw new Exception("Invalid input.");
                }
                else if (currentByte == 0)
                {
                    nullBytesLeft--;
                }

                sb.Append((char)currentByte);
            }

            return sb.ToString();
        }

        public static GitChangeInfo Parse(string diffTreeLine)
        {
            var match = DiffTreePattern.Match(diffTreeLine);
            if (!match.Success)
            {
                throw new Exception("Invalid input.");
            }

            Debug(diffTreeLine, match, DiffTreePattern);
            return new GitChangeInfo(match);
        }

        private static void Debug(string input, Match match, Regex regex)
        {
            Trace.WriteLine("Regex: " + regex);
            Trace.WriteLine("Input: " + input);
            foreach (var groupName in regex.GetGroupNames())
            {
                Trace.WriteLine(" -> " + groupName + ": >>" + match.Groups[groupName].Value + "<<");
            }
        }

        private readonly Match _match;

        private GitChangeInfo(Match match)
        {
            _match = match;
        }

        public LibGit2Sharp.Mode NewMode { get { return _match.Groups["dstmode"].Value.ToFileMode(); } }
        public string Status { get { return _match.Groups["status"].Value; } }

        public string oldMode { get { return _match.Groups["srcmode"].Value; } }
        public string newMode { get { return _match.Groups["dstmode"].Value; } }
        public string oldSha { get { return _match.Groups["srcsha1"].Value; } }
        public string newSha { get { return _match.Groups["dstsha1"].Value; } }
        public string path { get { return _match.Groups["srcpath"].Value; } }
        public string pathTo { get { return _match.Groups["dstpath"].Value; } }
        public string score { get { return _match.Groups["score"].Value; } }

        public IGitChangedFile ToGitChangedFile(ExplicitArgsExpression builder)
        {
            return builder.With(this).GetInstance<IGitChangedFile>(Status);
        }
    }
}
