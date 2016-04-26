// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using LibGit2Sharp;
using SemDiff.Core;
using System.IO;

namespace SemDiff.Test
{
    public class TestBase
    {
        public static Repo CurlyBroccoli { get; private set; }

        public static void CloneCurlyBrocoli(string checkoutBranch = null)
        {
            var curlyPath = Path.GetFullPath("curly");
            if (Directory.Exists(curlyPath))
            {
                SetNormalAttr(new DirectoryInfo(curlyPath)); //http://stackoverflow.com/a/1702920
                Directory.Delete(curlyPath, true);
            }
            var repo = Repository.Clone("https://github.com/semdiffdotnet/curly-broccoli.git", curlyPath);
            Repo.ClearCache();
            CurlyBroccoli = Repo.GetRepoFor(repo);

            if (!string.IsNullOrWhiteSpace(checkoutBranch))
            {
                using (var r = new Repository(repo))
                {
                    r.Fetch("origin");

                    //Get the remote branch, or the branch we want to track
                    var tBranch = r.Branches[$"origin/{checkoutBranch}"];
                    //Create an empty branch with correct name and correct code
                    var branch = r.CreateBranch(checkoutBranch, tBranch.Tip);
                    //Update the brach to track the remote
                    //branch = r.Branches.Update(branch, b => b.TrackedBranch = tBranch.CanonicalName);

                    r.Checkout(branch);
                }
            }

            CurlyBroccoli.UpdateRemoteChangesAsync().Wait();
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
    }
}