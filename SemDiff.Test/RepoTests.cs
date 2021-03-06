// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SemDiff.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SemDiff.Test
{
    [TestClass]
    public class RepoTests : TestBase
    {
        private const string owner = "semdiffdotnet";
        private const string repository = "curly-broccoli";
        public static Repo repo;

        [TestInitialize]
        public void TestInit()
        {
            repo = GetDummyRepo(nameof(RepoTests), owner, repository);
        }

        [TestMethod]
        public void RepoConstructorTest()
        {
            Assert.AreEqual(repo.RepoName, repository);
            Assert.AreEqual(repo.Owner, owner);
        }

        [TestMethod]
        public void PullRequestFromTestRepo()
        {
            repo.AssertRateLimit();
            var requests = repo.GetPullRequestsAsync().Result;
            Assert.AreEqual(5, requests.Count);
            var r = requests.ElementAt(requests.Count - 4);
            if (r.Number == 4)
            {
                Assert.AreEqual(r.State, "open");
                Assert.AreEqual(r.ValidFiles.Count(), 1);
                foreach (var f in r.ValidFiles)
                {
                    Assert.AreEqual("Curly-Broccoli/Curly/Logger.cs", f.Filename);
                }
            }
            else
            {
                Assert.Fail();
            }
            Assert.AreEqual("895d2ca038344aacfbcf3902e978de73a7a763fe", r.Head.Sha);
        }

        [TestMethod]
        public void EtagNotModified()
        {
            repo.AssertRateLimit();
            var requests = repo.GetPullRequestsAsync().Result;
            requests = repo.GetPullRequestsAsync().Result;
            Assert.AreEqual(null, requests);
        }

        [TestMethod]
        public void PaginationOnRoslynPRs()
        {
            repo.AssertRateLimit();
            repo.Owner = "dotnet";
            repo.RepoName = "roslyn";
            var roslynPRs = repo.GetPullRequestsAsync().Result;
            Assert.IsTrue(roslynPRs.Count > 30);
        }

        [TestMethod]
        public void FilesPaginationOn50States()
        {
            repo.AssertRateLimit();
            repo.Owner = "semdiffdotnet";
            repo.RepoName = "50states";
            var PRs = repo.GetPullRequestsAsync().Result;
            Assert.AreEqual(1, PRs.Count);
            var pr = PRs.First();
            Assert.AreEqual(83, pr.ValidFiles.Count());
        }

        [TestMethod]
        public void DownloadFilesFromGitHub()
        {
            repo.AssertRateLimit();
            var requests = repo.GetPullRequestsAsync().Result;
            var fourWasFound = false;
            foreach (var r in requests)
            {
                r.GetFilesAsync().Wait();
                if (r.Number == 4)
                {
                    fourWasFound = true;
                    var counter = 0;
                    string[] directoryTokens;
                    var dir = "";
                    var f = r.ValidFiles.Last();
                    directoryTokens = f.Filename.Split('/');
                    dir = repo.CacheDirectory;

                    dir = Path.Combine(dir, r.Number.ToString(), f.Filename.ToLocalPath());
                    var text = File.ReadAllText(dir);
                    foreach (var line in File.ReadAllLines(dir))
                    {
                        if (counter == 6)
                        {
                            var expected = "namespace Curly";
                            Assert.AreEqual(line, expected);
                        }
                        if (counter == 9)
                        {
                            var expected = "    /// Utility for logging to an internal list of Log entities";
                            Assert.AreEqual(line, expected);
                        }
                        counter++;
                    }
                    Assert.AreEqual(34, counter);
                }
            }
            Assert.IsTrue(fourWasFound);
        }

        [TestMethod]
        public void UpdateLocalSavedJsonFile()
        {
            repo.AssertRateLimit();
            repo.GetPullRequestsAsync().Wait();
            var path = repo.CacheDirectory.Replace('/', Path.DirectorySeparatorChar);
            path = Path.Combine(path, repo.CachedLocalPullRequestListPath);
            new FileInfo(path).Directory.Create();
            if (File.Exists(path))
                File.Delete(path);
            repo.UpdateLocalSavedList();
            Assert.IsTrue(File.Exists(path));
            var json = File.ReadAllText(path);
            var currentSaved = JsonConvert.DeserializeObject<IList<PullRequest>>(json);
            Assert.AreEqual(repo.PullRequests.Count, currentSaved.Count);
            var local = currentSaved.First();
            var gPR = repo.PullRequests.First();
            Assert.AreEqual(local.Number, gPR.Number);
            Assert.AreEqual(local.State, gPR.State);
            Assert.AreEqual(local.Title, gPR.Title);
            Assert.AreEqual(local.Updated, gPR.Updated);
            Assert.AreEqual(local.LastWrite, gPR.LastWrite);
            Assert.AreEqual(local.Url, gPR.Url);
            Assert.IsNotNull(local.Head);
            Assert.IsNotNull(local.Base);
            Assert.IsNotNull(local.Files);
            Assert.IsNotNull(local.ValidFiles);
        }

        [TestMethod]
        public void RemovedClosedAndDeletedPRCachedFiles()
        {
            repo.AssertRateLimit();
            var requests = repo.GetPullRequestsAsync().Result;
            var zeroDir = Path.Combine(repo.CacheDirectory.Replace('/', Path.DirectorySeparatorChar), "0");
            var prZero = requests.OrderBy(p => p.Number).First().Clone();
            prZero.Number = 0;
            prZero.ParentRepo = repo;
            foreach (var f in prZero.ValidFiles) //Add files to fool it!
            {
                f.ParentPullRequst = prZero;
                new FileInfo(f.CachePathBase).Directory.Create();
                File.WriteAllText(f.CachePathBase, "<BAD>");
                File.WriteAllText(f.CachePathHead, "<BAD>");
            }
            repo.PullRequests.Add(prZero);
            var json = repo.CachedLocalPullRequestListPath;
            File.WriteAllText(json, JsonConvert.SerializeObject(repo.PullRequests));
            repo.GetCurrentSaved();
            repo.EtagNoChanges = null;
            requests = repo.GetPullRequestsAsync().Result;
            Assert.IsFalse(Directory.Exists(zeroDir));
        }

        [TestMethod]
        public void LastSessionLocalFiles()
        {
            repo.AssertRateLimit();
            var requests = repo.GetPullRequestsAsync().Result;
            repo.UpdateLocalSavedList();
            var newRepo = new Repo(repo.LocalGitDirectory, repo.Owner, repo.RepoName);
            newRepo.GetCurrentSaved();
            Assert.IsNotNull(newRepo.PullRequests);
        }

        [TestMethod]
        public void NoUnnecessaryDownloading()
        {
            repo.AssertRateLimit();
            var requests = repo.GetPullRequestsAsync().Result;
            var path = "";
            foreach (var r in requests)
            {
                r.GetFilesAsync().Wait();
                if (r.Number == 1)
                {
                    foreach (var files in r.ValidFiles)
                    {
                        path = files.Filename.Replace('/', Path.DirectorySeparatorChar);
                    }
                }
            }
            var fileLastUpdated = File.GetLastWriteTimeUtc(path);
            repo.EtagNoChanges = null;
            requests = repo.GetPullRequestsAsync().Result;
            foreach (var r in requests)
            {
                r.GetFilesAsync().Wait();
            }
            Assert.IsTrue(fileLastUpdated == File.GetLastWriteTimeUtc(path));
        }

        [TestMethod]
        public void GetRepoForThisFileTest()
        {
            var thisFile = GetFileName();
            var repo = Repo.GetRepoFor(thisFile);
            Assert.AreEqual("semdiffdotnet", repo.Owner);
            Assert.AreEqual("semdiff", repo.RepoName);
        }

        [TestMethod]
        public void FilterFilesTest()
        {
            var files = new[]
            {
                new RepoFile
                {
                    Status = RepoFile.StatusEnum.Modified,
                    Filename = "folder/ClassName.cs",
                },
                new RepoFile
                {
                    Status = RepoFile.StatusEnum.Modified,
                    Filename = "folder/ProjName.csproj",
                },
                new RepoFile
                {
                    Status = RepoFile.StatusEnum.Removed,
                    Filename = "folder/NameClass.cs",
                }

            };
            var files2 = PullRequest.FilterFiles(files);
            var f = files2.Single();
            Assert.AreEqual("folder/ClassName.cs", f.Filename);
            Assert.AreEqual(RepoFile.StatusEnum.Modified, f.Status);
            
        }

        private string GetFileName([CallerFilePath] string file = "") => file;
    }
}