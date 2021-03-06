// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemDiff.Test
{
    [TestClass]
    public class FalseNegativeAnalysisTests : TestBase
    {
        //Base Class File
        private static string baseClass = "Curly-Broccoli/Curly/Logger.cs";

        //Inherited Class File
        private static string inheritedClass = "Curly-Broccoli/Curly/ConsoleLogger.cs";

        //The extra trees
        private static SyntaxTree remoteBase;

        private static SyntaxTree ancestorBase;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            CloneCurlyBrocoli(checkoutBranch: "false_negative_a");

            //'Local' False Negative A (Inherited Logger and Extended Functionality) #3
            //These files are in the cloned directory, but a change needs to be created to trigger analysis!
            var file = Path.Combine("curly", inheritedClass);
            File.WriteAllText(file, "//Hello World!\r\n" + File.ReadAllText(file));

            //'Remote' False Negative B (Optimize Logger by not calling Log) #4
            remoteBase = Repo.GetPathInCache(CurlyBroccoli.CacheDirectory, 4, baseClass).ParseFile();

            //Ancestor
            ancestorBase = Repo.GetPathInCache(CurlyBroccoli.CacheDirectory, 3, baseClass, isAncestor: true).ParseFile();
        }

        [TestMethod]
        public void FnDoCurlyBrocoliTest()
        {
            //Need a semantic model that contains the child and parent classes

            //Compilation needed to get the syntax tree out
            var solution = MSBuildWorkspace.Create().OpenSolutionAsync(Path.GetFullPath(Path.Combine("curly", "Curly-Broccoli", "Curly-Broccoli.sln"))).Result;
            var compilations = Task.WhenAll(solution.Projects.Select(p => p.GetCompilationAsync())).Result;

            //Create a pair between of the tree and there compilation
            var trees_compilation = compilations.SelectMany(c => c.SyntaxTrees.Select(t => new { tree = t, comp = c }));
            //Find the tree for our file, and then get a semantic model
            var model = trees_compilation.Where(tc => tc.tree.FilePath.EndsWith(inheritedClass.Replace('/', '\\'))).Select(tc => tc.comp.GetSemanticModel(tc.tree)).Single();

            //Need a remote file (ancestor and remote versions) for the base classes
            var repo = CurlyBroccoli;
            repo.PullRequests.Clear();
            repo.PullRequests.Add(new PullRequest
            {
                Updated = DateTime.Now,
                Files = new[] { new RepoFile {
                    BaseTree = ancestorBase,
                    HeadTree = remoteBase,
                    Filename = baseClass
                } },
                Title = "Fake Pull Request",
                Url = "http://github.com/example/repo"
            });

            var res = Analysis.ForFalseNegative(CurlyBroccoli, model);
            Assert.IsTrue(res.ToList().Any());
        }
    }
}