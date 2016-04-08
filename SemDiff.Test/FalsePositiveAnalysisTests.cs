using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.IO;
using System.Linq;

namespace SemDiff.Test
{
    [TestClass]
    public class FalsePositiveAnalysisTests : TestBase
    {
        private static string relativePath = @"Curly-Broccoli/Broccoli/AnalyseUser.cs";
        private static SyntaxTree fpA;
        private static SyntaxTree fpB;
        private static SyntaxTree fpC;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            CloneCurlyBrocoli();

            var path = Path.GetFullPath(Path.Combine("curly", relativePath));
            //False Positive A (Function12 moved, but not changed)
            fpA = Repo.GetPathInCache(CurlyBroccoli.RepoFolder, 1, relativePath).ParseFile(setPath: path);
            //False Positive B (Function12 changed)
            fpB = Repo.GetPathInCache(CurlyBroccoli.RepoFolder, 2, relativePath).ParseFile(setPath: path);
            //Ancestor (shared by both)
            fpC = Repo.GetPathInCache(CurlyBroccoli.RepoFolder, 2, relativePath, isAncestor: true).ParseFile(setPath: path);
        }

        [TestMethod]
        public void FpDoCurlyBrocoliTest()
        {
            OneSide(CurlyBroccoli, fpA, fpB, fpC, relativePath);
            OneSide(CurlyBroccoli, fpB, fpA, fpC, relativePath);
        }

        private static void OneSide(Repo repo, SyntaxTree local, SyntaxTree remote, SyntaxTree ancestor, string path)
        {
            //Fake a call te to GetRemoteChange() by placeing the remote and ancestor trees into the RemoteChangesData list
            repo.RemoteChangesData = repo.RemoteChangesData.Clear();
            repo.RemoteChangesData = repo.RemoteChangesData.Add(1, new RemoteChanges
            {
                Date = DateTime.Now,
                Files = new[] { new RemoteFile {
                    Base = ancestor,
                    File = remote,
                    Filename = path
                } },
                Title = "Fake Pull Request",
                Url = "http://github.com/example/repo"
            });

            var res = Analysis.ForFalsePositive(repo, local);
            Assert.IsTrue(res.Any());
        }
    }
}