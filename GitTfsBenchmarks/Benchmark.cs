// from http://www.yoda.arachsys.com/csharp/benchmark.html

// The MIT License
// 
// Copyright (c) 2008 Jon Skeet
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

/// <summary>
/// The attribute to use to mark methods as being
/// the targets of benchmarking.
/// </summary>

[AttributeUsage(AttributeTargets.Method)]
public class BenchmarkAttribute : Attribute
{
}

/// <summary>
/// Very simple benchmarking framework. Looks for all types
/// in the current assembly which have public static parameterless
/// methods marked with the Benchmark attribute. In addition, if 
/// there are public static Init, Reset and Check methods with
/// appropriate parameters (a string array for Init, nothing for
/// Reset or Check) these are called at appropriate times.
/// </summary>
public class Benchmark
{
    /// <summary>
    /// Number of times to run the methods in each type.
    /// </summary>
    static int runIterations = 1;

    public static void Main(string[] args)
    {
        args = ParseCommandLine(args);

        // Save all the benchmark classes from doing a nullity test
        if (args == null)
        {
            args = new string[0];
        }

        // We're only ever interested in public static methods. This variable
        // just makes it easier to read the code...
        BindingFlags publicStatic = BindingFlags.Public | BindingFlags.Static;

        var exceptions = new List<Exception>();

        foreach (Type type in typeof(Benchmark).Assembly.GetTypes())
        {
            // Find an Init method taking string[], if any
            MethodInfo initMethod = type.GetMethod("Init", publicStatic, null,
                                                  new Type[] { typeof(string[]) },
                                                  null);

            // Find a parameterless Reset method, if any
            MethodInfo resetMethod = type.GetMethod("Reset", publicStatic,
                                                   null, new Type[0],
                                                   null);

            // Find a parameterless Cleanup method, if any
            MethodInfo cleanupMethod = type.GetMethod("Cleanup", publicStatic,
                                                     null, new Type[0],
                                                     null);

            // Find a parameterless Check method, if any
            MethodInfo checkMethod = type.GetMethod("Check", publicStatic,
                                                  null, new Type[0],
                                                  null);

            // Find all parameterless methods with the [Benchmark] attribute
            ArrayList benchmarkMethods = new ArrayList();
            foreach (MethodInfo method in type.GetMethods(publicStatic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters != null && parameters.Length != 0)
                {
                    continue;
                }

                if (method.GetCustomAttributes
                    (typeof(BenchmarkAttribute), false).Length != 0)
                {
                    benchmarkMethods.Add(method);
                }
            }

            // Ignore types with no appropriate methods to benchmark
            if (benchmarkMethods.Count == 0)
            {
                continue;
            }

            Console.WriteLine("Benchmarking type {0}", type.Name);

            // If we've got an Init method, call it once
            try
            {
                if (initMethod != null)
                {
                    initMethod.Invoke(null, new object[] { args });
                }
            }
            catch (TargetInvocationException e)
            {
                Exception inner = e.InnerException;
                string message = (inner == null ? null : inner.Message);
                if (message == null)
                {
                    message = "(No message)";
                }
                Console.WriteLine("Init failed ({0})", message);
                continue; // Next type
            }

            for (int i = 0; i < runIterations; i++)
            {
                if (runIterations != 1)
                {
                    Console.WriteLine("Run #{0}", i + 1);
                }

                foreach (MethodInfo method in benchmarkMethods)
                {
                    var shouldCleanUpOnFailure = true;
                    try
                    {
                        // Reset (if appropriate)
                        if (resetMethod != null)
                        {
                            resetMethod.Invoke(null, null);
                        }

                        // Give the test as good a chance as possible
                        // of avoiding garbage collection
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        // Now run the test itself
                        DateTime start = DateTime.Now;
                        method.Invoke(null, null);
                        DateTime end = DateTime.Now;

                        // Check the results (if appropriate)
                        // Note that this doesn't affect the timing
                        if (checkMethod != null)
                        {
                            checkMethod.Invoke(null, null);
                        }

                        // Clean up (if appropriate)
                        shouldCleanUpOnFailure = false;
                        if (cleanupMethod != null)
                        {
                            cleanupMethod.Invoke(null, null);
                        }

                        // If everything's worked, report the time taken, 
                        // nicely lined up (assuming no very long method names!)
                        Console.WriteLine("  {0,-20} {1}", method.Name, end - start);
                    }
                    catch (TargetInvocationException e)
                    {
                        Exception inner = e.InnerException;
                        exceptions.Add(inner ?? e);
                        string message = (inner == null ? null : inner.Message);
                        if (message == null)
                        {
                            message = "(No message)";
                        }
                        Console.WriteLine("  {0}: Failed ({1}) [{2}]", method.Name, message, exceptions.Count);

                        // Clean up (if appropriate)
                        if (shouldCleanUpOnFailure && cleanupMethod != null)
                        {
                            cleanupMethod.Invoke(null, null);
                        }
                    }
                }
            }
        }
        for (var i = 0; i < exceptions.Count; i++)
        {
            Console.WriteLine();
            Console.WriteLine("[" + (i + 1) + "] " + exceptions[i]);
        }
    }

    /// <summary>
    /// Parses the command line, returning an array of strings
    /// which are the arguments the tasks should receive. This
    /// array will definitely be non-null, even if null is
    /// passed in originally.
    /// </summary>
    static string[] ParseCommandLine(string[] args)
    {
        if (args == null)
        {
            return new string[0];
        }

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-runtwice":
                    runIterations = 2;
                    break;

                case "-version":
                    PrintEnvironment();
                    break;

                case "-endoptions":
                    // All following options are for the benchmarked
                    // types.
                    {
                        string[] ret = new string[args.Length - i - 1];
                        Array.Copy(args, i + 1, ret, 0, ret.Length);
                        return ret;
                    }

                default:
                    // Don't understand option; copy this and
                    // all remaining options and return them.
                    {
                        string[] ret = new string[args.Length - i];
                        Array.Copy(args, i, ret, 0, ret.Length);
                        return ret;
                    }
            }
        }
        // Understood all arguments
        return new string[0];
    }

    /// <summary>
    /// Prints out information about the operating environment.
    /// </summary>
    static void PrintEnvironment()
    {
        Console.WriteLine("Operating System: {0}", Environment.OSVersion);
        Console.WriteLine("Runtime version: {0}", Environment.Version);
    }
}
