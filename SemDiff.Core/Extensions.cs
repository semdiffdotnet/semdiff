using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    /// <summary>
    /// Contains Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Merges two IEnumerable into a queue in an order based on an integer value
        /// </summary>
        /// <typeparam name="T">type parameter should be inferred by the compiler</typeparam>
        /// <param name="left">source enumerable</param>
        /// <param name="right">source enumerable</param>
        /// <param name="selector">
        /// a function that gets the integer value that the queue will be ordered by
        /// </param>
        /// <returns>
        /// A queue that contains the sources in an order based on the values provided by selector
        /// </returns>
        public static Queue<T> GetMergedQueue<T>(this IEnumerable<T> left, IEnumerable<T> right, Func<T, int> selector)
        {
            var leftQueue = left.ToQueue();
            var rightQueue = right.ToQueue();
            var result = new Queue<T>();
            while (true)
            {
                var leftEmpty = leftQueue.Count == 0;
                var rightEmpty = rightQueue.Count == 0;
                if (leftEmpty && rightEmpty)
                {
                    break;
                }
                else if (leftEmpty)
                {
                    result.Enqueue(rightQueue.Dequeue());
                }
                else if (rightEmpty)
                {
                    result.Enqueue(leftQueue.Dequeue());
                }
                else
                {
                    var l = selector(leftQueue.Peek());
                    var r = selector(rightQueue.Peek());
                    if (l > r)
                    {
                        result.Enqueue(rightQueue.Dequeue());
                    }
                    else
                    {
                        result.Enqueue(leftQueue.Dequeue());
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Executes a logging action for each item in an IEnumerable. Logging action will be
        /// executed as each item is used Note that the output of this function must be consumed in
        /// order to execute the logging action
        /// </summary>
        /// <typeparam name="T">type parameter should be inferred by the compiler</typeparam>
        /// <param name="source">list of <typeparamref name="T"/></param>
        /// <param name="loggingAction">action that will be called for every item in the list</param>
        /// <returns>the source items unchanged, but in a new IEnumerable</returns>
        public static IEnumerable<T> Log<T>(this IEnumerable<T> source, Action<T> loggingAction)
            => source.Select(o => { loggingAction?.Invoke(o); return o; });

        /// <summary>
        /// Executes a task, if it throws an exception then execute it again after a delay. If it
        /// fails again the error will not be caught
        /// </summary>
        /// <typeparam name="T">inferred by the compiler</typeparam>
        /// <param name="tsk">A function that produces the task to execute</param>
        /// <param name="wait">The amount of time to wait between failure and second try</param>
        /// <returns></returns>
        public async static Task<T> RetryOnceAsync<T>(this Func<Task<T>> tsk, TimeSpan wait)
        {
            try
            {
                return await tsk?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                await Task.Delay(wait);
                return await tsk?.Invoke();
            }
        }

        /// <summary>
        /// Creates a new queue from a list of items, a shortcut to prevent typing the <typeparamref
        /// name="T"/> parameter
        /// </summary>
        /// <typeparam name="T">type parameter should be inferred by the compiler</typeparam>
        /// <param name="source">list of <typeparamref name="T"/></param>
        /// <returns>A queue that contains the source</returns>
        public static Queue<T> ToQueue<T>(this IEnumerable<T> source) => new Queue<T>(source);

        public static SyntaxTree ToSyntaxTree(this SyntaxNode node) => SyntaxFactory.SyntaxTree(node);

        public static string ToLocalPath(this string path) => path.Replace('/', Path.DirectorySeparatorChar);

        public static string ToStandardPath(this string path) => path.Replace('\\', '/');
    }
}