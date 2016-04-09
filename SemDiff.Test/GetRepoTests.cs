using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SemDiff.Test
{
    [TestClass]
    public class GetRepoTests : TestBase
    {
        [TestMethod]
        public void RepoFromConfigTest()
        {
            var repo = Repo.RepoFromConfig(".", "testgitconfig.txt");
            Assert.AreEqual("dotnet", repo.Owner);
            Assert.AreEqual("roslyn", repo.RepoName);
        }

        [TestMethod]
        public void RepoFromConfig2Test()
        {
            var repo = Repo.RepoFromConfig(".", "testgitconfig2.txt");
            Assert.AreEqual("haroldhues", repo.Owner);
            Assert.AreEqual("HaroldHues-Public", repo.RepoName);
        }

        [TestMethod]
        public void RepoFromConfig3Test()
        {
            var repo = Repo.RepoFromConfig(".", "testgitconfig3.txt");
            Assert.AreEqual("dotnet", repo.Owner);
            Assert.AreEqual("roslyn", repo.RepoName);
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

        [TestMethod]
        public void GetRepoThatDoesntExistTest()
        {
            var inAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Local", "Microsoft");
            var repo = Repo.GetRepoFor(inAppData);
            Assert.AreEqual(null, repo);
        }
    }
}