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
        public static Repo github;

        [TestInitialize]
        public void TestInit()
        {
            github = GetDummyRepo(nameof(GitIgnoreTests), owner, repository);
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