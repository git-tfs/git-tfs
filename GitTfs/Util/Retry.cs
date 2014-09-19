using System;
using System.Collections.Generic;
using System.Threading;

namespace Sep.Git.Tfs.Util
{
    class Retry
    {
        public static void Do(Action action)
        {
            Do(action, TimeSpan.FromSeconds(1));
        }

        public static void Do(Action action, TimeSpan retryInterval, int retryCount = 10)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, retryCount);
        }

        public static T Do<T>(Func<T> action)
        {
            return Do(action, TimeSpan.FromSeconds(1));
        }

        public static T Do<T>(Func<T> action, TimeSpan retryInterval, int retryCount = 10)
        {
            var exceptions = new List<Exception>();

            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {

                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
