// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SemDiff.Core.Exceptions;
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
        /// There are cases where GitHub's diff messes up; See #82
        /// </summary>
        public bool IsInvalid { get; set; }

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
            try
            {
                var baseTsk = ParentRepo.HttpGetAsync($@"https://github.com/{ParentRepo.Owner}/{ParentRepo.RepoName}/raw/{ParentPullRequst.Base.Sha}/{Filename}");
                var headTsk = ParentRepo.HttpGetAsync($@"https://github.com/{ParentRepo.Owner}/{ParentRepo.RepoName}/raw/{ParentPullRequst.Head.Sha}/{Filename}");

                var text = await Task.WhenAll(baseTsk, headTsk);
                var baseText = text[0];
                var headText = text[1];

                if (ParentRepo.LineEndings == LineEndingType.crlf)
                {
                    //Area for speed improvement
                    baseText = baseText.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                    headText = headText.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                }
                else
                {
                    //Area for speed improvement
                    baseText = baseText.Replace("\r\n", "\n").Replace("\r", "\n");
                    headText = headText.Replace("\r\n", "\n").Replace("\r", "\n");
                }

                BaseTree = CSharpSyntaxTree.ParseText(baseText);
                HeadTree = CSharpSyntaxTree.ParseText(headText);

                Directory.CreateDirectory(Path.GetDirectoryName(CachePathBase));
                File.WriteAllText(CachePathBase, baseText);
                File.WriteAllText(CachePathHead, headText);
            }
            catch (GitHubUnknownErrorException ex) when (ex.Message == "Not Found")
            {
                //See #82
                IsInvalid = true;
            }
        }

        internal void LoadFromCache()
        {
            BaseTree = CSharpSyntaxTree.ParseText(File.ReadAllText(CachePathBase));
            HeadTree = CSharpSyntaxTree.ParseText(File.ReadAllText(CachePathHead));
        }
    }
}