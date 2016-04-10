using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
    }
}