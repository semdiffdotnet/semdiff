using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.Collections.Generic;

namespace SemDiff.Test
{
    [TestClass]
    public class PullRequestListTest
    {
        string owner = "semdiffdotnet";
        string repository = "curly-broccoli";
        GitHub github;

        public void setGitHub()
        {
            github = new GitHub(owner, repository);
        }
        [TestMethod]
        public void NewGitHub()
        {
            setGitHub();
            Assert.AreEqual(github.RepoName, repository);
            Assert.AreEqual(github.RepoOwner, owner);

        }
        [TestMethod]
        public void PullRequestFromTestRepo()
        {
            setGitHub();
            var requests = github.GetPullRequests();
            var fourWasFound = false;
            if (github.RequestsRemaining != 0)
            {
                foreach(var r in requests)
                {
                   if(r.number == 4)
                    {
                        fourWasFound = true;
                        Assert.AreEqual(r.locked,false);
                        Assert.AreEqual(r.state, "open");
                        Assert.AreEqual(r.user.login, "haroldhues");
                        Assert.AreEqual(r.files.Count, 1);
                        foreach(var f in r.files)
                        {
                            Assert.AreEqual(f.filename, "Curly-Broccoli/Broccoli/AnalyseUser.cs");
                            Assert.AreEqual(f.raw_url, "https://github.com/haroldhues/curly-broccoli/raw/79336452724314e1fb29671a7319db9d9a5c7e29/Curly-Broccoli/Broccoli/AnalyseUser.cs");
                        }
                    }
                }
                Assert.AreEqual(fourWasFound, true);
            }
        }
        [TestMethod]
        public void GetFilesFromGitHub()
        {
            setGitHub();
            var requests = github.GetPullRequests();
            var fourWasFound = false;
            if (github.RequestsRemaining != 0)
            {
                Assert.AreNotEqual(github.RequestsRemaining, 0);
                foreach (var r in requests)
                {
                    github.DownloadFiles(r);
                    if (r.number == 4)
                    {
                        fourWasFound = true;
                        string line;
                        int counter = 0;
                        string[] directoryTokens;
                        string dir = "";
                        foreach (var f in r.files)
                        {
                            directoryTokens = f.filename.Split('/');
                            dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SemDiff/";
                            foreach (var token in directoryTokens)
                            {
                                if (token != directoryTokens[0])
                                {
                                    dir = dir + "/" + token;
                                }
                                else
                                {
                                    dir = dir + token + "/" + r.number;
                                }
                            }
                        }
                        using (var file = new System.IO.StreamReader(dir))
                        {
                            while ((line = file.ReadLine()) != null)
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

                            file.Close();
                        }
                        Assert.AreEqual(counter, 35);
                    }
                }
                Assert.AreEqual(fourWasFound, true);
            }
        }
    }
}
