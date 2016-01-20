using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System.Linq;

namespace SemDiff.Test
{
    [TestClass]
    public class Diff3Tests
    {
        [TestMethod]
        public void CompareNoConflictTest()
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
            var conflicts = Diff3.Compare(original, changed1, changed2);
            Assert.IsFalse(conflicts.Any());
        }

        [TestMethod]
        public void CompareOverlapingTest()
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
            ".WrapWithMethod().Parse(); //Minus 153 chars
            var changed2 = @"
                var total = 0;
                var n = 100;
                for (var i = 0; i < n; i++)
                {
                    total += i;
                }
            ".WrapWithMethod().Parse();
            var conflicts = Diff3.Compare(original, changed1, changed2);
            Assert.IsTrue(conflicts.Any());
        }

        [TestMethod]
        public void CompareInsertsTest()
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
            var conflicts = Diff3.Compare(original, changed1, changed2);
            Assert.IsTrue(conflicts.Any());
        }
    }
}