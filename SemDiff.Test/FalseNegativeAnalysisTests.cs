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
            //These files are in the cloned directory!

            //'Remote' False Negative B (Optimize Logger by not calling Log) #4
            remoteBase = GitHub.GetPathInCache(CurlyBroccoli.GitHubApi.RepoFolder, 4, baseClass).ParseFile();

            //Ancestor
            ancestorBase = GitHub.GetPathInCache(CurlyBroccoli.GitHubApi.RepoFolder, 3, baseClass, isAncestor: true).ParseFile();
        }

        [TestMethod]
        public void FnDoCurlyBrocoliTest()
        {
            //Need a semantic model that contains the child and parent classes

            //Compilation needed to get the syntax tree out
            var solution = MSBuildWorkspace.Create().OpenSolutionAsync(Path.GetFullPath(Path.Combine("curly", "Curly-Broccoli", "Curly-Broccoli.sln"))).Result;
            var compilations = Task.WhenAll(solution.Projects.Select(p => p.GetCompilationAsync())).Result;

            //Creat a pair between of the tree and there compilation
            var trees_compilation = compilations.SelectMany(c => c.SyntaxTrees.Select(t => new { tree = t, comp = c }));
            //Find the tree for our file, and then get a semantic model
            var model = trees_compilation.Where(tc => tc.tree.FilePath.EndsWith(inheritedClass.Replace('/', '\\'))).Select(tc => tc.comp.GetSemanticModel(tc.tree)).Single();

            //Need a remote file (ancestor and remote versions) for the base classes
            var repo = CurlyBroccoli;
            repo.RemoteChangesData = repo.RemoteChangesData.Clear();
            repo.RemoteChangesData = repo.RemoteChangesData.Add(1, new RemoteChanges
            {
                Date = DateTime.Now,
                Files = new[] { new RemoteFile {
                    Base = ancestorBase,
                    File = remoteBase,
                    Filename = baseClass
                } },
                Title = "Fake Pull Request",
                Url = "http://github.com/example/repo"
            });

            var res = Analysis.ForFalseNegative(CurlyBroccoli, model);
            Assert.IsTrue(res.Any());
        }
    }
}