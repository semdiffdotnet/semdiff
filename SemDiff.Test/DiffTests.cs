using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System.Linq;

namespace SemDiff.Test
{
    [TestClass]
    public class DiffTests
    {
        [TestMethod]
        public void GetChangesNotChangedTest()
        {
            var original = @"
                var total = 0;
                var n = 100;
                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed = @"
                var total = 0;
                var n = 100;
                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changes = Diff.Compare(original, changed);
            Assert.AreEqual(0, changes.Count());
        }

        [TestMethod]
        public void GetChangesChangedOnceTest()
        {
            var original = @"
                var total = 0;
                var n = 100;
                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed = @"
                var total = 0;
                var n = 100;
                for (var i = n - 1; i >= 0; i--)
                {
                    total += i;
                }
            ".WrapWithMethod().Parse();
            var changes = Diff.Compare(original, changed);
            Assert.AreEqual("= i + total", changes.Single().AncestorText);
            Assert.AreEqual("+= i", changes.Single().ChangedText);
        }

        [TestMethod]
        public void GetChangesChangedTwiceTest()
        {
            var original = @"
                var total = 0;
                var n = 100;
                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed = @"
                var total = 0;
                var n = 100;
                for (var i = 0; i < n; i++)
                {
                    total += i;
                }
            ".WrapWithMethod().Parse();
            var changes = Diff.Compare(original, changed).ToList();
            Assert.AreEqual(4, changes.Count()); //Even though two changes are made it is interpreted as 4!

            Assert.AreEqual("n - 1", changes[0].AncestorText);
            Assert.AreEqual("0", changes[0].ChangedText);

            Assert.AreEqual(">= 0", changes[1].AncestorText);
            Assert.AreEqual("< n", changes[1].ChangedText);

            Assert.AreEqual("--", changes[2].AncestorText);
            Assert.AreEqual("++", changes[2].ChangedText);

            Assert.AreEqual("= i + total", changes[3].AncestorText);
            Assert.AreEqual("+= i", changes[3].ChangedText);
        }

        [TestMethod]
        public void IntersectFalseTest()
        {
            var original = @"
                var total = 0;
                var n = 100;
                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed1 = @"
                var total = 0;
                var n = 100;
                for (var i = 0; i < n; i++)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed2 = @"
                var total = 0;
                var n = 100;
                for (var i = n - 1; i >= 0; i--)
                {
                    total += i;
                }
            ".WrapWithMethod().Parse();
            var c1s = Diff.Compare(original, changed1).ToList();
            var c2 = Diff.Compare(original, changed2).Single();

            foreach (var x in Enumerable.Range(0, 3))
            {
                Assert.IsFalse(Diff.Intersects(c1s[x], c2));
                Assert.IsFalse(Diff.Intersects(c2, c1s[x]));
            }
        }

        [TestMethod]
        public void IntersectTrueInsertsTest()
        {
            var original = @"
                var total = 0;
                var n = 100;
                for (var i = 0; i < n; i++)
                {
                }
            ".WrapWithMethod().Parse();
            var changed1 = @"
                var total = 0;
                var n = 100;
                for (var i = 0; i < n; i++)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed2 = @"
                var total = 0;
                var n = 100;
                for (var i = 0; i < n; i++)
                {
                    total += i;
                }
            ".WrapWithMethod().Parse();
            var c1 = Diff.Compare(original, changed1).Single();
            var c2 = Diff.Compare(original, changed2).Single();

            Assert.IsTrue(Diff.Intersects(c1, c2));
            Assert.IsTrue(Diff.Intersects(c2, c1));
        }

        //TODO: Test to see if this subtle conflict could result in a merge conflict
        //[TestMethod]
        //public void IntersectTrueReplacePartTest()
        //{
        //    var original = @"
        //        var total = 0;
        //        var n = 100;
        //        for (var i = 0; i < n; i++)
        //        {
        //            total = i;
        //        }
        //    ".WrapWithMethod().Parse();
        //    var changed1 = @"
        //        var total = 0;
        //        var n = 100;
        //        for (var i = 0; i < n; i++)
        //        {
        //            total = i + total;
        //        }
        //    ".WrapWithMethod().Parse();
        //    var changed2 = @"
        //        var total = 0;
        //        var n = 100;
        //        for (var i = 0; i < n; i++)
        //        {
        //            total += i;
        //        }
        //    ".WrapWithMethod().Parse();
        //    var c1 = Diff.Compare(original, changed1).Single();
        //    var c2 = Diff.Compare(original, changed2).Single();

        //    Assert.IsTrue(Diff.Intersects(c1, c2));
        //}

        [TestMethod]
        public void IntersectTrueOverlapingTest()
        {
            var original = @"
                var total = 0;
                var n = 100;
                for (var i = 0; i < n; i++)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed1 = @"
                var total = 0;
            ".WrapWithMethod().Parse();
            var changed2 = @"
                var total = 0;
                var n = 100;
                for (var i = 0; i < n; i++)
                {
                    total += i;
                }
            ".WrapWithMethod().Parse();
            var c1 = Diff.Compare(original, changed1).Single();
            var c2 = Diff.Compare(original, changed2).Single();

            Assert.IsTrue(Diff.Intersects(c1, c2));
            Assert.IsTrue(Diff.Intersects(c2, c1));
        }
    }
}