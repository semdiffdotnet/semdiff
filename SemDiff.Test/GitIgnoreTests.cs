// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SemDiff.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SemDiff.Test
{
    [TestClass]
    public class GitIgnoreTests : TestBase
    {
        private const string owner = "semdiffdotnet";
        private const string repository = "curly-broccoli";
        private const string authUsername = "haroldhues";
        private const string authToken = "9db4f2de497905dc5a5b2c597869a55a9ae05d9b";
        public static Repo github;

        [TestInitialize]
        public void TestInit()
        {
            var repoLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff), "semdiffdotnet", repository);
            github = new Repo(repoLoc, owner, repository, authUsername, authToken);
            github.UpdateLimitAsync().Wait();
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff));
            if (new FileInfo(appDataFolder).Exists)
                Directory.Delete(appDataFolder, recursive: true);
        }

        [TestMethod]
        public void CreateGitIgnoreIfItDoesntExist()
        {
            var gitIgnoreLoc = Path.Combine(github.LocalRepoDirectory, ".gitignore");
            if (File.Exists(gitIgnoreLoc))
            {
                File.Delete(gitIgnoreLoc);
            }
            github.UpdateGitIgnore();
            Assert.IsTrue(File.Exists(gitIgnoreLoc));
        }
        [TestMethod]
        public void GitIgnoreAddsWhenNeeded()
        {
            var gitIgnoreLoc = Path.Combine(github.LocalRepoDirectory, ".gitignore");
            if (File.Exists(gitIgnoreLoc))
            {
                File.Delete(gitIgnoreLoc);
            }
            var gitIgnoreText = ".semdiff/stuff" + Environment.NewLine + ".sem" + Environment.NewLine + " .semdiff";
            File.WriteAllText(gitIgnoreLoc, gitIgnoreText);
            github.UpdateGitIgnore();
            var input = File.ReadAllText(gitIgnoreLoc);
            gitIgnoreText = gitIgnoreText +Environment.NewLine +
                "#The Semdiff cache folder" + Environment.NewLine + 
                ".semdiff/" + Environment.NewLine;
            Assert.IsTrue(input == gitIgnoreText);
        }
    }
}