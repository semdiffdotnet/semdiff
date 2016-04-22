// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SemDiff.Test
{
    [TestClass]
    public class RepoTests : TestBase
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            CloneCurlyBrocoli();
        }

        [TestMethod]
        public void CheckRateLimit()
        {
            CurlyBroccoli.UpdateLimitAsync().Wait();
            var limit = CurlyBroccoli.RequestsLimit;
            var remaining = CurlyBroccoli.RequestsRemaining;
            if ((float)remaining / limit < 0.1)
            {
                Assert.Inconclusive("There are less than 10% of requests remaining before the rate limit is hit");
            }
        }

        [TestMethod]
        public void RepoGetChangedFiles()
        {
            CurlyBroccoli.UpdateLimitAsync().Wait();
            var limit = CurlyBroccoli.RequestsLimit;
            var remaining = CurlyBroccoli.RequestsRemaining;
            if ((float)remaining / limit < 0.1)
            {
                Assert.Inconclusive("There are less than 10% of requests remaining before the rate limit is hit");
            }
            CurlyBroccoli.UpdateRemoteChangesAsync().Wait();
            var pulls = CurlyBroccoli.PullRequests.ToList();
            Assert.AreEqual(5, pulls.Count);
            foreach (var p in pulls)
            {
                Assert.IsNotNull(p.Files);
                Assert.IsNotNull(p.Title);
                Assert.AreNotEqual(default(DateTime), p.Updated);
                foreach (var f in p.Files)
                {
                    Assert.IsNotNull(f.BaseTree);
                    Assert.IsNotNull(f.HeadTree);
                    Assert.IsNotNull(f.Filename);
                }
            }
        }

        [TestMethod]
        public void GetRepoForThisFileTest()
        {
            var thisFile = GetFileName();
            var repo = Repo.GetRepoFor(thisFile);
            Assert.AreEqual("semdiffdotnet", repo.Owner);
            Assert.AreEqual("semdiff", repo.RepoName);
        }

        private string GetFileName([CallerFilePath] string file = "") => file;
    }
}