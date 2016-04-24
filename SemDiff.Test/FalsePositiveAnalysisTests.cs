// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
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
            fpA = Repo.GetPathInCache(CurlyBroccoli.CacheDirectory, 1, relativePath).ParseFile(setPath: path);
            //False Positive B (Function12 changed)
            fpB = Repo.GetPathInCache(CurlyBroccoli.CacheDirectory, 2, relativePath).ParseFile(setPath: path);
            //Ancestor (shared by both)
            fpC = Repo.GetPathInCache(CurlyBroccoli.CacheDirectory, 2, relativePath, isAncestor: true).ParseFile(setPath: path);
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
            repo.PullRequests.Clear();
            repo.PullRequests.Add(new PullRequest
            {
                Updated = DateTime.Now,
                Files = new[] { new Core.RepoFile {
                    BaseTree = ancestor,
                    HeadTree = remote,
                    Filename = path,
                    Status = RepoFile.StatusEnum.Modified,
                } },
                Title = "Fake Pull Request",
                Url = "http://github.com/example/repo",
                Number = 1,
                State = "open",
                LastWrite = DateTime.MinValue,
                Base = new PullRequest.HeadBase { Sha = "" },
                Head = new PullRequest.HeadBase { Sha = "" },
            });
            var pr = repo.PullRequests.First();
            pr.ParentRepo = repo;
            pr.ValidFiles.First().ParentPullRequst = pr;

            var res = Analysis.ForFalsePositive(repo, local);
            Assert.IsTrue(res.Any());
        }
    }
}