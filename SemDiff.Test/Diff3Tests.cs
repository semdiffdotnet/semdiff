using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
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

        //[TestMethod]
        //public void CompareMethodsTest()
        //{
        //    //This is our base (the common ancestor)
        //    var original = (
        //        "return;".Method("Member1") + 3.BlankLines() +
        //        "return;".Method("Member3") + 3.BlankLines() +
        //        "return;".Method("Member4") + 3.BlankLines() +
        //        "return;".Method("Member5") + 3.BlankLines() +
        //        "return;".Method("Member6") + 3.BlankLines() +
        //        "return;".Method("Member7") + 3.BlankLines() +
        //        "return;".Method("Member2") + 3.BlankLines() +
        //        "return;".Method("Member8") + 3.BlankLines()
        //        //Utilities for producing valid syntaxtree
        //        ).WrapWithClass().Parse();
        //    //This is like a local developer that moves a method (method 2)
        //    var local = (
        //        "return;".Method("Member1") +
        //        "return;".Method("Member2") +
        //        "return;".Method("Member3") + 3.BlankLines() +
        //        "return;".Method("Member4") + 3.BlankLines() +
        //        "return;".Method("Member5") + 3.BlankLines() +
        //        "return;".Method("Member6") + 3.BlankLines() +
        //        "return;".Method("Member7") + 3.BlankLines() +
        //        "return;".Method("Member8") + 3.BlankLines()
        //        ).WrapWithClass().Parse();
        //    //This is like a pull request that modifies a method (method 2)
        //    var remote = (
        //        "return;".Method("Member1") + 3.BlankLines() +
        //        "return;".Method("Member3") + 3.BlankLines() +
        //        "return;".Method("Member4") + 3.BlankLines() +
        //        "return;".Method("Member5") + 3.BlankLines() +
        //        "return;".Method("Member6") + 3.BlankLines() +
        //        "return;".Method("Member7") +
        //        "var x = 10; return;".Method("Member2") +
        //        "return;".Method("Member8")
        //        ).WrapWithClass().Parse();

        //    //Run through the Diff3 logic to get the changes and the conflicts
        //    var diff3Result = Diff3.Compare(original, local, remote);

        //    //Conflict captures method that was both removed and edited
        //    //Assume that there is only one for this test!
        //    var conflict = diff3Result.Conflicts.Single();
        //    //Assume that we have captured a whole method in our conflict
        //    var orig = conflict.Ancestor.Node as MethodDeclarationSyntax;
        //    Assert.IsNotNull(orig);
        //    //Check that it is removed locally
        //    Assert.AreEqual(0, conflict.Local.Span.Length);
        //    //Get changed method!
        //    var rem = conflict.Remote.Node as MethodDeclarationSyntax;
        //    Assert.IsNotNull(rem);

        //    //Look in the non-conflicting change that represents the method
        //    //    being added somewhere else (move destination)
        //    var loc = diff3Result.Local
        //            //Make sure there is nothing there before
        //            .Where(diff => string.IsNullOrWhiteSpace(diff.Ancestor.Text))
        //            //Make sure there is a method there afterwards
        //            .Select(diff => diff.Changed.Node as MethodDeclarationSyntax)
        //            .Where(method => method != null)
        //            //Make sure that it matches our method's name
        //            .First(method => method.Identifier.Text == rem.Identifier.Text);
        //    Assert.IsNotNull(loc);

        //    //Now we diff the insides of the methods
        //    var diff3ResultInner = Diff3.Compare(orig, loc, rem);
        //    //Since there is no conflicts inside the method, this is a false-positive!
        //    Assert.IsFalse(diff3ResultInner.Conflicts.Any());
        //}

        //[TestMethod]
        //public void CompareMethods2Test()
        //{
        //    var original = (
        //        "return;".Method("Member1") +
        //        "return;".Method("Member3") +
        //        "return;".Method("Member4") +
        //        "return;".Method("Member5") +
        //        "return;".Method("Member6") +
        //        "return;".Method("Member7") +
        //        "return;".Method("Member2") +
        //        "return;".Method("Member8")
        //        ).WrapWithClass().Parse();
        //    var changed1 = (
        //        "return;".Method("Member1") +
        //        "return;".Method("Member2") +
        //        "return;".Method("Member3") +
        //        "return;".Method("Member4") +
        //        "return;".Method("Member5") +
        //        "return;".Method("Member6") +
        //        "return;".Method("Member7") +
        //        "return;".Method("Member8")
        //        ).WrapWithClass().Parse();
        //    var changed2 = (
        //        "return;".Method("Member1") +
        //        "return;".Method("Member3") +
        //        "return;".Method("Member4") +
        //        "return;".Method("Member5") +
        //        "return;".Method("Member6") +
        //        "return;".Method("Member7") +
        //        "var x = 10; return;".Method("Member2") +
        //        "return;".Method("Member8")
        //        ).WrapWithClass().Parse();
        //    var diff3Result = Diff3.Compare(original, changed1, changed2);

        //    //Conflict captures method that was both removed and edited
        //    var conflict = diff3Result.Conflicts.Single();
        //    var orig = conflict.Ancestor.Node as MethodDeclarationSyntax;
        //    Assert.IsNotNull(orig);
        //    Assert.AreEqual(0, conflict.Local.Span.Length);
        //    var rem = conflict.Remote.Node as MethodDeclarationSyntax;

        //    //Look in the non-conflicting change that represents the
        //    var loc = diff3Result.Local.First(diff =>
        //            string.IsNullOrWhiteSpace(diff.Ancestor.Text) && (diff.Changed.Node as MethodDeclarationSyntax)?.Identifier.Text == rem.Identifier.Text
        //            ).Changed.Node as MethodDeclarationSyntax;

        //    var diff3ResultInner = Diff3.Compare(orig, loc, rem);
        //    Assert.IsFalse(diff3ResultInner.Conflicts.Any());
        //}
    }
}