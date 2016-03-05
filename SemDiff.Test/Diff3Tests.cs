using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System.IO;
using System.Linq;

namespace SemDiff.Test
{
    [TestClass]
    public class Diff3Tests : TestBase
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

            var localDiff = Diff.VisualDiff(conflicts.Local, conflicts.AncestorTree);
            var remoteDiff = Diff.VisualDiff(conflicts.Remote, conflicts.AncestorTree);

            conflicts.Conflicts = conflicts.Conflicts.ToList();

            Assert.IsTrue(!conflicts.Conflicts.Any());
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

            var localDiff = Diff.VisualDiff(conflicts.Local, conflicts.AncestorTree);
            var remoteDiff = Diff.VisualDiff(conflicts.Remote, conflicts.AncestorTree);

            conflicts.Conflicts = conflicts.Conflicts.ToList();

            Assert.IsTrue(conflicts.Conflicts.Any());
            var strrep = conflicts.Conflicts.First().ToString();
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
            Assert.IsTrue(conflicts.Conflicts.Any());
            var strrep = conflicts.Conflicts.First().ToString();
        }

        //This test exists because these files were causing problems where the span that we were creating
        //wasn't actually in the file, so when we went to get a string representation it would throw an
        //OutOfRangeException
        [TestMethod]
        public void CompareOutOfRangeRegressionTest()
        {
            var ancestor = File.ReadAllText("Newtonsoft.Ancestor.txt").WrapWithNamespace().Parse();
            var local = File.ReadAllText("Newtonsoft.Local.txt").WrapWithNamespace().Parse();
            var remote = File.ReadAllText("Newtonsoft.Remote.txt").WrapWithNamespace().Parse();

            var conflicts = Diff3.Compare(ancestor, local, remote);

            var localDiff = Diff.VisualDiff(conflicts.Local, conflicts.AncestorTree);
            var remoteDiff = Diff.VisualDiff(conflicts.Remote, conflicts.AncestorTree);

            conflicts.Conflicts = conflicts.Conflicts.ToList();
            Assert.IsTrue(conflicts.Conflicts.Any());
            var strrep = conflicts.Conflicts.First().ToString();
        }

        //More Minimal Failing case for the above
        [TestMethod]
        public void CompareFirstDiffLongSecondShortRegressionTest()
        {
            var original = @"
            int outval = 0;
            switch (0x5) {
                case 0x1:
                    outval = 0x7;
                    break;

                case 0x2:
                    outval = 0x6;
                    break;

                case 0x3:
                    outval = 0x5;
                    break;

                case 0x4:
                    outval = 0x4;
                    break;

                case 0x5:
                    outval = 0x3;
                    break;

                case 0x6:
                    outval = 0x2;
                    break;

                case 0x7:
                    outval = 0x1;
                    break;

                case 0x8:
                    outval = 0x0;
                    break;
            }
            ".WrapWithMethod().Parse();
            var changed1 = @"
            int outval = 0x3;
            ".WrapWithMethod().Parse();
            var changed2 = @"
            int outval = 0;
            switch (0x4) {
                case 0x4:
                    outval = 0x4;
                    break;
            }
            ".WrapWithMethod().Parse();
            var conflicts = Diff3.Compare(original, changed1, changed2);

            var localDiff = Diff.VisualDiff(conflicts.Local, conflicts.AncestorTree);
            var remoteDiff = Diff.VisualDiff(conflicts.Remote, conflicts.AncestorTree);

            conflicts.Conflicts = conflicts.Conflicts.ToList();
            Assert.IsTrue(conflicts.Conflicts.Any());
            var strrep = conflicts.Conflicts.First().ToString();
        }
    }
}