using LibGit2Sharp;
using SemDiff.Core;
using System.IO;

namespace SemDiff.Test
{
    public class TestBase
    {
        public static Repo CurlyBroccoli { get; private set; }

        public static void CloneCurlyBrocoli()
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
    }
}