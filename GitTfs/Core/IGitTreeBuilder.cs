using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sep.Git.Tfs.Core
{
    public interface IGitTreeBuilder
    {
        void Add(string path, string file, string mode);
        void Remove(string path);
        string GetTree();
    }
}
