using System;

namespace Sep.Git.Tfs.Util
{
    /// <summary>
    /// A generic caretaker that can be used inside a using statement to 
    /// execute code on dispose when the using block exits.
    /// </summary>
    /// <remarks>
    /// <para>The action can be used to restore state that was preserved
    /// when the caretaker was created.</para>
    /// <para>This concept is adapted from the <i>caretaker</i> as described 
    /// in the Memento pattern: http://en.wikipedia.org/wiki/Memento_pattern
    /// </para>
    /// </remarks>
    public class GenericCaretaker : IDisposable
    {
        private Action _restore;

        public GenericCaretaker(Action restore)
        {
            _restore = restore;
        }

        public void Dispose()
        {
            _restore();
        }
    }
}