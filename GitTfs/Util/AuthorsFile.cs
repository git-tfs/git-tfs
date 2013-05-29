﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core;
using System.Diagnostics;

namespace Sep.Git.Tfs.Util
{
    public class Author
    {
        public Author(string tfsUserId, string name, string email)
        {
            TfsUserId = tfsUserId;
            _gitAuthor = new Tuple<string, string>(name, email);
            _gitUserId = BuildGitUserId(_gitAuthor);
        }

        public string Name 
        {
            get
            {
                return _gitAuthor.Item1;
            } 
        }


        public string Email 
        {
            get
            {
                return _gitAuthor.Item2;
            }
        }

        public string TfsUserId { get; set; }

        public string GitUserId
        {
            get
            {
                return _gitUserId; 
            }
        }

        // we only use the trimmed email address as identity 
        // (dictionary key) to avoid mismatches because of 
        // active directory name formatting rules.
        public static string BuildGitUserId(string email)
        {
            return email.Trim(); 
        }

        public static string BuildGitUserId(Tuple<string,string> gitUser)
        {
            return BuildGitUserId(gitUser.Item2);
        }

        #region (private)
        private Tuple<string, string> _gitAuthor;
        private string _gitUserId;
        #endregion
    }

    [StructureMapSingleton]
    public class AuthorsFile
    {
        private readonly Dictionary<string, Author> _authorsByTfsUserId = new Dictionary<string, Author>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Author> _authorsByGitUserId = new Dictionary<string, Author>(StringComparer.OrdinalIgnoreCase);

        public AuthorsFile()
        {}


        public Dictionary<string, Author> Authors
        {
            get
            {
                return this._authorsByTfsUserId;
            }
        }


        public Dictionary<string, Author> AuthorsByGitUserId
        {
            get
            {
                return this._authorsByGitUserId;
            }
        }


        public Author FindAuthor(Tuple<string,string> gitUser)
        {
            string key = Author.BuildGitUserId(gitUser);
            Author a;
            return _authorsByGitUserId.TryGetValue(key, out a) ? a : null;
        }


        // The first time a tfs user id or a git id is encountered, it is used as lookup key.
        public bool Parse(TextReader authorsFileStream)
        {
            if (authorsFileStream == null)
                return false;
            int lineCount = 0;
            string line = authorsFileStream.ReadLine();
            while (line != null)
            {
                lineCount++;
                if (!line.StartsWith("#"))
                {
                    //regex pulled from git svn script here: https://github.com/git/git/blob/master/git-svn.perl
                    Regex ex = new Regex(@"^(.+?|\(no author\))\s*=\s*(.+?)\s*<(.+)>\s*$");
                    Match match = ex.Match(line);
                    if (match.Groups.Count != 4 || String.IsNullOrWhiteSpace(match.Groups[1].Value) || String.IsNullOrWhiteSpace(match.Groups[2].Value) ||
                        String.IsNullOrWhiteSpace(match.Groups[3].Value))
                    {
                        throw new GitTfsException("Invalid format of Authors file on line " + lineCount + ".");
                    }
                    else
                    {
                        //git svn doesn't trim, but maybe this should?
                        string tfsUserId = match.Groups[1].Value; //.Trim();
                        string name      = match.Groups[2].Value; //.Trim();
                        string email     = match.Groups[3].Value; //.Trim();

                        Author a = new Author(tfsUserId, name, email);

                        if (!_authorsByTfsUserId.ContainsKey(a.TfsUserId))
                            _authorsByTfsUserId.Add(a.TfsUserId, a);

                        if (!_authorsByGitUserId.ContainsKey(a.GitUserId))
                            _authorsByGitUserId.Add(a.GitUserId, a);
                    }
                }
                line = authorsFileStream.ReadLine();
            }
            return true;
        }

        public bool Parse(string authorsFilePath, string gitDir)
        {
            var savedAuthorFile = Path.Combine(gitDir, "git-tfs_authors");
            if (!String.IsNullOrWhiteSpace(authorsFilePath))
            {
                if (!File.Exists(authorsFilePath))
                {
                    throw new GitTfsException("Authors file cannot be found: '" + authorsFilePath + "'");
                }
                else
                {
                    Trace.WriteLine("Reading authors file : " + authorsFilePath);
                    bool parseSuccess;
                    using (StreamReader sr = new StreamReader(authorsFilePath))
                    {
                        parseSuccess = Parse(sr);
                    }
                    try
                    {
                        File.Copy(authorsFilePath, savedAuthorFile, true);
                        return parseSuccess;
                    }
                    catch (Exception) { }
                }
            }
            else if (File.Exists(savedAuthorFile))
            {
                Trace.WriteLine("Reading cached authors file (" + savedAuthorFile + ")...");
                using (StreamReader sr = new StreamReader(savedAuthorFile))
                {
                    return Parse(sr);
                }
            }
            else
                Trace.WriteLine("No authors file used.");
            return false;
        }
    }
}
