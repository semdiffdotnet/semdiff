using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    public static class Extensions
    {
        public static Queue<T> ToQueue<T>(this IEnumerable<T> source) => new Queue<T>(source);

        public static Queue<T> GetMergedChangeQueue<T>(this IEnumerable<T> left, IEnumerable<T> right, Func<T, int> selector)
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

        public async static Task<T> RetryOnce<T>(this Func<Task<T>> tsk, TimeSpan wait)
        {
            try
            {
                return await tsk();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                await Task.Delay(wait);
                return await tsk();
            }
        }
    }
}