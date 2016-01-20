using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemDiff.Test
{
    [TestClass]
    public class Diff3Tests
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
            var changes = Diff3.Diff(original, changed);
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
            var changes = Diff3.Diff(original, changed);
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
            var changes = Diff3.Diff(original, changed).ToList();
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
        public void GetChangesStringTest()
        {
            var original = @"
                var str = ""<<<<<<<<"";
            ".WrapWithMethod().Parse();
            var changed = @"
                var str = "">>>>||||<<<<"";
            ".WrapWithMethod().Parse();
            var changes = Diff3.Diff(original, changed).ToList();
        }
    }
}