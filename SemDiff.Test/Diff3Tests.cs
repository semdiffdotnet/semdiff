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
            Assert.IsFalse(conflicts.Conflicts.Any());
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
            Assert.IsTrue(conflicts.Conflicts.Any());
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
        }

        //[TestMethod]
        //public void CompareMethodsTest()
        //{
        //    var original = ("for (var i = n - 1; i >= 0; i--) { total = i + total; } return 17;".Method("TheAnswer", "int") + "return 42;".Method("MostRandom", "int")).WrapWithClass().Parse();
        //    var changed1 = ("for (var i = n - 1; i >= 0; i--) { total = i + total; } return 42;".Method("TheAnswer", "int") + "return 17;".Method("MostRandom", "int")).WrapWithClass().Parse();
        //    var changed2 = ("return 24;".Method("MostRandom", "int") + "for (var i = n - 1; i >= 0; i--) { total = i + total; } return 17;".Method("TheAnswer", "int")).WrapWithClass().Parse();
        //    var conflicts = Diff3.Compare(original, changed1, changed2).ToList();
        //    Assert.IsTrue(conflicts.Any());
        //}

        [TestMethod]
        public void CompareMethodsTest()
        {
            var original = (
                "return;".Method("Member1") +
                "return;".Method("Member3") +
                "return;".Method("Member4") +
                "return;".Method("Member5") +
                "return;".Method("Member6") +
                "return;".Method("Member7") +
                "return;".Method("Member2") +
                "return;".Method("Member8")
                ).WrapWithClass().Parse();
            var changed1 = (
                "return;".Method("Member1") +
                "return;".Method("Member2") +
                "return;".Method("Member3") +
                "return;".Method("Member4") +
                "return;".Method("Member5") +
                "return;".Method("Member6") +
                "return;".Method("Member7") +
                "return;".Method("Member8")
                ).WrapWithClass().Parse();
            var changed2 = (
                "return;".Method("Member1") +
                "return;".Method("Member3") +
                "return;".Method("Member4") +
                "return;".Method("Member5") +
                "return;".Method("Member6") +
                "return;".Method("Member7") +
                "var x = 10; return;".Method("Member2") +
                "return;".Method("Member8")
                ).WrapWithClass().Parse();
            var conflicts = Diff3.Compare(original, changed1, changed2).Conflicts.ToList();
            Assert.IsTrue(conflicts.Any());
        }
    }
}