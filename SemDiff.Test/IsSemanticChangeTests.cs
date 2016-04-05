using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.Linq;

namespace SemDiff.Test
{
    [TestClass]
    public class IsSemanticChangeTests
    {
        [TestMethod]
        public void IsSemanticChange0Test()
        {
            var original = @"
                var total = 0; //This is good formating!
                var n = 100;

                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed = @"
                var total = 0; //This is good formating!
                var n = 100;

                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var str = Diff.VisualDiff(original, changed);
            var change = TriviaCompare.IsSemanticChange(original.GetRoot(), changed.GetRoot());
            Assert.IsFalse(change);
        }

        [TestMethod]
        public void IsSemanticChange1Test()
        {
            var original = @"
                var total = 0; //This is good formating!
                var n = 100;

                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed = @"var total=0;var n=100;for(var i=n-1;i>=0;i--){total=i+total; /*Random Comment*/}".WrapWithMethod().Parse();
            var str = Diff.VisualDiff(original, changed);
            var change = TriviaCompare.IsSemanticChange(original.GetRoot(), changed.GetRoot());
            Assert.IsFalse(change);
        }

        [TestMethod]
        public void IsSemanticChange2Test()
        {
            var original = @"
                var total = 0; //This is good formating!
                var n = 100;

                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed = @"

                            var total=0;

                                     var n
                                           = 100;for
                    (var           i=n-1;
                                  i >= 0;
                                     i--)

                    {
                            total=    i   +     total

            ;
                  } //Formating is more of an art than a science? Don't you think?

                        ".WrapWithMethod().Parse();
            var str = Diff.VisualDiff(original, changed);
            var change = TriviaCompare.IsSemanticChange(original.GetRoot(), changed.GetRoot());
            Assert.IsFalse(change);
        }

        [TestMethod]
        public void IsSemanticChange3Test()
        {
            var original = @"
                var total = 0; //This is good formating!
                var n = 100;

                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed = @"
                var total = 0; //This is good formating!
                var n = 100;

                for (var i = n - 1; i >= 0; i--)
                {
                    total += i;
                }
            ".WrapWithMethod().Parse(); //(total ... changed)
            var str = Diff.VisualDiff(original, changed);
            var change = TriviaCompare.IsSemanticChange(original.GetRoot(), changed.GetRoot());
            Assert.IsTrue(change);
        }

        [TestMethod]
        public void IsSemanticChange4Test()
        {
            var original = @"
                var total = 0; //This is good formating!
                var n = 100;

                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total;
                }
            ".WrapWithMethod().Parse();
            var changed = @"
                var total = 0; //This is good formating!
                var n = 100;

                for (var i = n - 1; i >= 0; i--)
                {
                    total = i + total
                }
            ".WrapWithMethod().Parse(); //(missing semicolon)
            var str = Diff.VisualDiff(original, changed);
            var change = TriviaCompare.IsSemanticChange(original.GetRoot(), changed.GetRoot());
            Assert.IsTrue(change);
        }
    }
}