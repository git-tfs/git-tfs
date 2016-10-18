
namespace Sep.Git.Tfs.Test
{
    public class BaseTest
    {
        public BaseTest()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Trace.Listeners.Clear();
            }
        }
    }
}