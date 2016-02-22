﻿using Microsoft.CodeAnalysis;
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
        private static string fpPath = @"Curly-Broccoli/Broccoli/AnalyseUser.cs";
        private static SyntaxTree fpA;
        private static SyntaxTree fpB;
        private static SyntaxTree fpC;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            CloneCurlyBrocoli();

            //False Positive A (Function12 moved, but not changed)
            fpA = GitHub.GetPathInCache(CurlyBroccoli.GitHubApi.RepoFolder, 1, fpPath).ParseFile();
            //False Positive B (Function12 changed)
            fpB = GitHub.GetPathInCache(CurlyBroccoli.GitHubApi.RepoFolder, 2, fpPath).ParseFile();
            //Ancestor (shared by both)
            fpC = GitHub.GetPathInCache(CurlyBroccoli.GitHubApi.RepoFolder, 2, fpPath, isAncestor: true).ParseFile();
        }

        [TestMethod]
        public void FpDoCurlyBrocoliTest()
        {
            var path = Path.GetFullPath(Path.Combine("curly", fpPath));
            OneSide(CurlyBroccoli, fpA, fpB, fpC, path);
            OneSide(CurlyBroccoli, fpB, fpA, fpC, path);
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
                } }
            });

            var res = Analysis.ForFalsePositive(repo, local, path);
            Assert.IsTrue(res.Any());
        }
    }
}