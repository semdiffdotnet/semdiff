using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.IO;
using System.Linq;

namespace SemDiff.Test
{
    [TestClass]
    public class RepoTests
    {
        public static Repo CurlyBroccoli { get; set; }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var curlyPath = Path.GetFullPath("curly");
            if (Directory.Exists(curlyPath))
            {
                SetNormalAttr(new DirectoryInfo(curlyPath)); //http://stackoverflow.com/a/1702920
                Directory.Delete(curlyPath, true);
            }
            Repository.Clone("https://github.com/semdiffdotnet/curly-broccoli.git", curlyPath);
            Repo.Authentication = true;
            CurlyBroccoli = Repo.RepoFromConfig(curlyPath, Path.Combine(curlyPath, ".git", "config"));
        }

        private static void SetNormalAttr(DirectoryInfo directory)
        {
            foreach (var subdir in directory.GetDirectories())
            {
                SetNormalAttr(subdir);
                subdir.Attributes = FileAttributes.Normal;
            }
            foreach (var file in directory.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }
        }

        [TestMethod]
        public void RepoGetChangedFiles()
        {
            var pulls = CurlyBroccoli.GetRemoteChanges().ToList();
            Assert.AreEqual(4, pulls.Count);
            foreach (var p in pulls)
            {
                Assert.IsNotNull(p.Files);
                Assert.IsTrue(p.Files.Count() > 0);
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