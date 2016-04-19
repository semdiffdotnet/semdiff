// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    /// <summary>
    /// Object used for parsing that reflects the json of the object that GitHub uses for Pull Requests
    /// </summary>
    public class PullRequest
    {
        public HeadBase Base { get; set; }

        [JsonIgnore]
        public string CacheDirectory => Path.Combine(ParentRepo.CacheDirectory, Number.ToString());

        public IList<RepoFile> Files { get; set; }
        public HeadBase Head { get; set; }
        public DateTime LastWrite { get; set; } = DateTime.MinValue;
        public int Number { get; set; }

        [JsonIgnore]
        public Repo ParentRepo { get; set; }

        public string State { get; set; }
        public string Title { get; set; }

        [JsonProperty("updated_at")]
        public DateTime Updated { get; set; }

        [JsonProperty("html_url")]
        public string Url { get; set; }

        /// <summary>
        /// Download files for a given pull request. Store the files in the AppData folder with a
        /// sub-folder for the pull request
        /// </summary>
        public async Task GetFilesAsync()
        {
            if (LastWrite >= Updated)
            {
                foreach (var f in Files)
                {
                    f.LoadFromCache();
                }
            }
            else
            {
                await Task.WhenAll(Files.Select(current => current.DownloadFileAsync()));
                LastWrite = DateTime.UtcNow;
            }
        }

        //Filters a list of files leaving only the files that semdiff can actually use
        internal static IEnumerable<RepoFile> FilterFiles(IEnumerable<RepoFile> files)
        {
            return files.Where(current =>
            {
                switch (current.Status)
                {
                    case RepoFile.StatusEnum.Added:
                    case RepoFile.StatusEnum.Removed:
                    case RepoFile.StatusEnum.Renamed: //Not sure how to handle this one...
                        break;

                    case RepoFile.StatusEnum.Changed:
                        Logger.Info($"The mythical 'changed' status has occurred! {current.ParentPullRequst.Number}:{current.Filename}");
                        return true;

                    case RepoFile.StatusEnum.Modified:
                        return true;
                }
                return false;
            });
        }

        /// <summary>
        /// Object used for parsing that reflects the json of the object that GitHub uses inside of
        /// the pull request object
        /// </summary>
        public class HeadBase
        {
            public string Label { get; set; }
            public string Ref { get; set; }
            public string Sha { get; set; }
        }
    }
}