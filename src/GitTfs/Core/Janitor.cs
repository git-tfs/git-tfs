using GitTfs.Util;

using System.Diagnostics;

namespace GitTfs.Core
{
    [StructureMapSingleton]
    public class Janitor : IDisposable
    {
        private readonly Queue<Action> _actions = new Queue<Action>();

        public void CleanThisUpWhenWeClose(Action action) => _actions.Enqueue(action);

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
