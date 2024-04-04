
namespace GitTfs.Test
{
    public class BaseTest
    {
        /// <summary>
        /// Set this variable to `true` to display trace logs
        /// This value is set by default to false because xunit runner >v2.0 do not hide these logs anymore
        /// and it too much logs are displayed in build output (AppVeyor page is too heavy to load and read!)
        /// </summary>
        public const bool DebugTests = false;

        public static bool DisplayTrace => System.Diagnostics.Debugger.IsAttached || DebugTests;
        public BaseTest()
        {
            Globals.DisableGarbageCollect = true;
            if (!DisplayTrace)
            {
                System.Diagnostics.Trace.Listeners.Clear();
            }
        }
    }
}