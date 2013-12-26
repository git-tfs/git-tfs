using System;
using System.Collections.Generic;
using System.Threading;

namespace Sep.Git.Tfs.VsCommon
{
    public static class Retry
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
                catch (Microsoft.TeamFoundation.TeamFoundationServiceUnavailableException ex)
                {
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
                catch (Microsoft.TeamFoundation.Framework.Client.DatabaseOperationTimeoutException ex)
                {
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
                catch (System.Net.WebException ex)
                {
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
