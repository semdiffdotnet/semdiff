// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    /// <summary>
    /// Represents files that are part of a Pull Request
    /// </summary>
    public class RepoFile
    {
        public enum StatusEnum
        {
            Added,
            Modified,
            Removed,
            Renamed,

            //Mostly undocumented, occurs rarely
            //Seems to show up when only the file permissions were changed
            //or if there are too many files it the pull request (more than about 200)
            Changed
        }

        /// <summary>
        /// The ancestor that the pull request will be merged with
        /// </summary>
        [JsonIgnore]
        public SyntaxTree BaseTree { get; set; }

        [JsonIgnore]
        public string CachePathBase => Path.Combine(ParentPullRequst.CacheDirectory, Filename.ToStandardPath() + ".orig");

        [JsonIgnore]
        public string CachePathHead => Path.Combine(ParentPullRequst.CacheDirectory, Filename.ToStandardPath());

        public string Filename { get; set; }

        /// <summary>
        /// The current file from the open pull request
        /// </summary>
        [JsonIgnore]
        public SyntaxTree HeadTree { get; set; }

        [JsonIgnore]
        public PullRequest ParentPullRequst { get; set; }

        [JsonIgnore]
        public Repo ParentRepo => ParentPullRequst.ParentRepo;

        [JsonConverter(typeof(StringEnumConverter))]
        public StatusEnum Status { get; set; }

        internal async Task DownloadFileAsync()
        {
            var baseTsk = ParentRepo.HttpGetAsync($@"https://github.com/{ParentRepo.Owner}/{ParentRepo.RepoName}/raw/{ParentPullRequst.Base.Sha}/{Filename}");
            var headTsk = ParentRepo.HttpGetAsync($@"https://github.com/{ParentRepo.Owner}/{ParentRepo.RepoName}/raw/{ParentPullRequst.Head.Sha}/{Filename}");

            var text = await Task.WhenAll(baseTsk, headTsk);
            var baseText = text[0];
            var headText = text[1];

            BaseTree = CSharpSyntaxTree.ParseText(baseText);
            HeadTree = CSharpSyntaxTree.ParseText(headText);

            Directory.CreateDirectory(Path.GetDirectoryName(CachePathBase));
            File.WriteAllText(CachePathBase, baseText);
            File.WriteAllText(CachePathHead, headText);
        }

        internal void LoadFromCache()
        {
            BaseTree = CSharpSyntaxTree.ParseText(File.ReadAllText(CachePathBase));
            HeadTree = CSharpSyntaxTree.ParseText(File.ReadAllText(CachePathHead));
        }
    }
}