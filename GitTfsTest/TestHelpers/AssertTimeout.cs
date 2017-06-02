using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Sep.Git.Tfs.Test.TestHelpers
{
    public static class AssertTimeout
    {
        /// <summary>
        /// Fails tests which would otherwise run indefinitely.
        /// </summary>
        public static void For(TimeSpan limit, Action actionToLimit, string message = null)
        {
            if (actionToLimit == null) throw new ArgumentNullException("actionToLimit");

            var exceptionDispatchInfo = (ExceptionDispatchInfo)null;
            var thread = new Thread(() =>
            {
                try
                {
                    actionToLimit.Invoke();
                }
                catch (Exception ex)
                {
                    // Preserve the call stack
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
            })
            {
                Name = "AssertTimeout " + actionToLimit.Method.Name
            };

            thread.Start();

            if (Debugger.IsAttached)
            {
                thread.Join();
            }
            else
            {
                if (!thread.Join(limit))
                {
                    thread.Abort();
                    throw new TimeoutException(message ?? (actionToLimit.Method.Name + " took longer than " + limit + "."));
                }
            }

            if (exceptionDispatchInfo != null)
                exceptionDispatchInfo.Throw();
        }
    }
}
