﻿using System;
using Sep.Git.Tfs.Util;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sep.Git.Tfs.Core
{
    [StructureMapSingleton]
    public class Janitor : IDisposable
    {
        Queue<Action> _actions = new Queue<Action>();

        public void CleanThisUpWhenWeClose(Action action)
        {
            _actions.Enqueue(action);
        }

        public void Dispose()
        {
            while (_actions.Count > 0)
            {
                try
                {
                    _actions.Dequeue()();
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Janitor tried to clean something up, and it failed: " + e);
                }
            }
        }
    }
}
