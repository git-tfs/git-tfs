﻿using System.IO;

namespace GitTfs.Core
{
    public interface ITreeEntry
    {
        string FullName { get; }
        Stream OpenRead();
    }
}