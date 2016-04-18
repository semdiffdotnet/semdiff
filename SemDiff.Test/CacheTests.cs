// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System.Collections.Generic;
using System.Linq;

namespace SemDiff.Test
{
    [TestClass]
    public class CacheTests
    {
        [TestMethod]
        public void CacheEnumerateTwiceTest()
        {
            //Base line
            var one = GetTestEnumerable();
            foreach (var i in one) { } //Enumerate once
            Assert.AreNotEqual(1, one.First());

            //With Cache
            var two = GetTestEnumerable().CacheEnumerable();
            foreach (var i in two) { } //Enumerate once
            Assert.AreEqual(1, two.First());
        }

        [TestMethod]
        public void CacheEnumeratePartialTwiceTest()
        {
            //Base line
            var one = GetTestEnumerable();
            one.Take(5).ToList();
            one.Skip(5).Take(5).ToList();
            Assert.AreNotEqual(11, one.Skip(10).First());

            //With Cache
            var two = GetTestEnumerable().CacheEnumerable();
            two.Take(5).ToList();
            two.Skip(5).Take(5).ToList();
            Assert.AreEqual(11, two.Skip(10).First());
        }

        [TestMethod]
        public void CacheTaleOfTwoEnumerationsTest()
        {
            //This is a verbose test that test for correct behavior when moving through the list twice at the same time.

            var source = GetTestEnumerable().CacheEnumerable();
            var one = source.GetEnumerator();
            //Manually Enumerate 10
            ManuallyEnumerate10(one, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            var two = source.GetEnumerator();
            //Manually Enumerate 10
            ManuallyEnumerate10(two, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            //Manually Enumerate 10 more
            ManuallyEnumerate10(two, new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });

            Assert.IsFalse(two.MoveNext());

            //The cache should just be in use at this point
            Assert.IsInstanceOfType(source.GetEnumerator(), typeof(List<int>.Enumerator));

            //Manually Enumerate 10 more from one
            ManuallyEnumerate10(one, new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });

            Assert.IsFalse(one.MoveNext());

            //One last double check
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }, source.ToArray());
        }

        private void ManuallyEnumerate10(IEnumerator<int> one, int[] v)
        {
            ManuallyEnumerate2(one, v[0], v[1]);
            ManuallyEnumerate2(one, v[2], v[3]);
            ManuallyEnumerate2(one, v[4], v[5]);
            ManuallyEnumerate2(one, v[6], v[7]);
            ManuallyEnumerate2(one, v[8], v[9]);
        }

        private void ManuallyEnumerate2(IEnumerator<int> one, int v1, int v2)
        {
            Assert.IsTrue(one.MoveNext());
            Assert.AreEqual(v1, one.Current);
            Assert.IsTrue(one.MoveNext());
            Assert.AreEqual(v2, one.Current);
        }

        public static IEnumerable<int> GetTestEnumerable()
        {
            int i = 0;
            var enumerate = Enumerable.Range(0, 20).Select(ignore => ++i);
            return enumerate;
        }
    }
}