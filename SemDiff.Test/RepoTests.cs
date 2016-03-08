using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.IO;
using System.Linq;

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
        public void RepoGetChangedFiles()
        {
            CurlyBroccoli.UpdateRemoteChangesAsync().Wait();
            var pulls = CurlyBroccoli.RemoteChangesData.Values.ToList();
            Assert.AreEqual(5, pulls.Count);
            foreach (var p in pulls)
            {
                Assert.IsNotNull(p.Files);
                Assert.IsNotNull(p.Title);
                Assert.AreNotEqual(default(DateTime), p.Date);
                foreach (var f in p.Files)
                {
                    Assert.IsNotNull(f.Base);
                    Assert.IsNotNull(f.File);
                    Assert.IsNotNull(f.Filename);
                }
            }
        }
    }
}