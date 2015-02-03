using System;
using System.Collections.Generic;
using System.Threading;
using Sep.Git.Tfs.Core;

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
                catch (Microsoft.TeamFoundation.TeamFoundationServerException ex)
                {
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
                catch (System.Net.WebException ex)
                {
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
                catch (GitTfsException ex) // allows continue of catch (MappingConflictException e) throw as innerexception
                {
                    exceptions.Add(ex);
                    Thread.Sleep(retryInterval);
                }
            }

            throw new AggregateException(exceptions);
        }

        public static void DoWhile(Func<bool> action, int retryCount = 10)
        {
            DoWhile(action, TimeSpan.FromSeconds(0), retryCount);
        }

        public static void DoWhile(Func<bool> action, TimeSpan retryInterval, int retryCount = 10)
        {
            int count = 0;
            while (action())
            {
                count++;
                if (count > retryCount)
                    throw new GitTfsException("error: Action failed after " + retryCount + " retries!");
                Thread.Sleep(retryInterval);
            }
        }
    }
}
