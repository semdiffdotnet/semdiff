using Newtonsoft.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    /// <summary>
    /// Contains Extension methods
    /// </summary>
    public static class Extensions
    {
        public static T Clone<T>(this T source)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
        }

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
            if (selector == null)
                throw new InvalidOperationException();
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

        /// <summary>
        /// By Default when Linq is used the source in enumerated every time the result is used. The
        /// traditional fix is to use ToList, but this must enumerate the whole enumerable. This function
        /// is the middle ground. It will enumerate the source once, but it will only enumerate the items
        /// that are needed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<T> Cache<T>(this IEnumerable<T> source)
        {
            return new CacheIterator<T>(source);
        }

        private class CacheIterator<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> source;
            private readonly List<T> cached = new List<T>();
            private readonly IEnumerator<T> enumerator;
            private bool isCached;

            public CacheIterator(IEnumerable<T> source)
            {
                this.source = source;
                enumerator = source.GetEnumerator();
            }

            private void MoveNext()
            {
                if (isCached)
                    return;
                lock (this)
                {
                    if (isCached)
                        return;
                    if (enumerator.MoveNext())
                    {
                        cached.Add(enumerator.Current);
                    }
                    else
                    {
                        isCached = true;
                    }
                }
            }

#pragma warning disable CC0022 // Should dispose object (Incorrect Code Cracker Warning)

            public IEnumerator<T> GetEnumerator() => isCached ? (IEnumerator<T>)cached.GetEnumerator() : new CacheEnumerator<T>(this);

#pragma warning restore CC0022 // Should dispose object

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private class CacheEnumerator<R> : IEnumerator<R>
            {
                private readonly CacheIterator<R> cacheIterator;
                private int currentIndex = -1;

                public CacheEnumerator(CacheIterator<R> cacheIterator)
                {
                    this.cacheIterator = cacheIterator;
                }

                public R Current
                {
                    get
                    {
                        try
                        {
                            return cacheIterator.cached[currentIndex];
                        }
                        catch (IndexOutOfRangeException)
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    currentIndex++;

                    if (currentIndex < cacheIterator.cached.Count)
                    {
                        return true;
                    }
                    else if (cacheIterator.isCached)
                    {
                        return false;
                    }
                    else
                    {
                        cacheIterator.MoveNext();
                        return (currentIndex < cacheIterator.cached.Count);
                    }
                }

                public void Reset()
                {
                    currentIndex = -1;
                }
            }
        
	/// <summary>
        /// Generates a tuple for every matching element in the list. If the size of the lists are
        /// not the same, then the shorter list is padded with the default value of the type.
        /// </summary>
        /// <typeparam name="L">Element type of left list</typeparam>
        /// <typeparam name="R">Element type of right list</typeparam>
        /// <param name="sourcel">Left list</param>
        /// <param name="sourcer">Right list</param>
        /// <returns>
        /// List of tuples, item 1 will contain items from the left list and item 2 will contain
        /// items from the right list
        /// </returns>
        public static IEnumerable<Tuple<L, R>> Map<L, R>(this IEnumerable<L> sourcel, IEnumerable<R> sourcer)
        {
            var enuml = sourcel.GetEnumerator();
            var enumr = sourcer.GetEnumerator();

            var availablel = true;
            var availabler = true;

            do
            {
                availablel = availablel ? enuml.MoveNext() : false; //This way MoveNext is not called after it has returned false once
                availabler = availabler ? enumr.MoveNext() : false;

                if (availablel || availabler)
                {
                    yield return Tuple.Create(availablel ? enuml.Current : default(L),
                                              availabler ? enumr.Current : default(R));
                }
            }
            while (availablel || availabler);
        }

        /// <summary>
        /// Makes a struct type nullable
        /// </summary>
        /// <typeparam name="T">Type that will become nullable</typeparam>
        /// <param name="source">object to be surrounded by nullable</param>
        /// <returns>A nullable type</returns>
        public static T? MakeNullable<T>(this T source) where T : struct
        {
            return source;
        }

        /// <summary>
        /// Filters out all comment and whitespace changes, leaving only conflicts that are
        /// semantically meaningful
        /// </summary>
        /// <param name="source">list of conflicts</param>
        /// <returns>list of semantically meaningful conflicts</returns>
        public static IEnumerable<Conflict> SemanticChanges(this IEnumerable<Conflict> source)
        {
            return source.Where(c => TriviaCompare.IsSemanticChange(c.Local.Node, c.Remote.Node));
        }
    }
}
