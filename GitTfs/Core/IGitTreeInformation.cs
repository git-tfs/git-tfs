using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sep.Git.Tfs.Core
{
    public interface IGitTreeInformation
    {
        // mode for existing path, null if path doesn't exist
        string GetMode(string path);
    }
}
