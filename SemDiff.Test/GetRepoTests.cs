using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SemDiff.Test
{
    [TestClass]
    public class GetRepoTests
    {
        [TestMethod]
        public void RepoFromConfigTest()
        {
            var repo = Repo.RepoFromConfig(".", "testgitconfig.txt");
            Assert.AreEqual("dotnet", repo.Owner);
            Assert.AreEqual("roslyn", repo.Name);
        }

        [TestMethod]
        public void RepoFromConfig2Test()
        {
            var repo = Repo.RepoFromConfig(".", "testgitconfig2.txt");
            Assert.AreEqual("haroldhues", repo.Owner);
            Assert.AreEqual("HaroldHues-Public", repo.Name);
        }

        [TestMethod]
        public void GetRepoForThisFileTest()
        {
            var thisFile = GetFileName();
            var repo = Repo.GetRepoFor(thisFile);
            Assert.AreEqual("semdiffdotnet", repo.Owner);
            Assert.AreEqual("semdiff", repo.Name);
        }

        private string GetFileName([CallerFilePath] string file = "") => file;

        [TestMethod]
        public void GetRepoThatDoesntExistTest()
        {
            var inAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Local", "Microsoft");
            var repo = Repo.GetRepoFor(inAppData);
            Assert.AreEqual(null, repo);
        }
    }
}