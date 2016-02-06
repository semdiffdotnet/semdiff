using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SemDiff.Core.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    /// <summary>
    /// Contains all the methods and classes nessasary to use the GitHub api to pull down data about pull requests. Additionally, contains the logic for authenticating
    /// </summary>
    public class GitHub
    {
        //Figure out how to ignore the IP, possibly parse by any number and ignore blank
        private static string APIRateLimitNonOAuthError = "API rate limit exceeded for xxx.xxx.xxx.xxx. (But here's the good news: Authenticated requests get a higher rate limit. Check out the documentation for more details.)";

        private static string APIDoesNotExistError = "Not Found";

        static GitHubConfiguration gitHubConfig =
            new GitHubConfiguration((AuthenticationSection)ConfigurationManager.GetSection("SemDiff.Core/authentication"));

        public GitHub(string repoOwner, string repoName)
        {
            this.RepoOwner = repoOwner;
            this.RepoName = repoName;

            this.RequestsRemaining = 1;
            Client = new HttpClient //TODO: Enable gzip!
            {
                BaseAddress = new Uri("https://api.github.com/")
            };
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(nameof(SemDiff));
            Client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

            string authToken = gitHubConfig.AuthenicationToken;
            string authUsername = gitHubConfig.Username;
            if (!string.IsNullOrEmpty(authToken) || !string.IsNullOrEmpty(authUsername))
            {
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{authUsername}:{authToken}")));
            }

            RepoFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SemDiff), RepoOwner, RepoName);
        }

        public string AuthToken { get; set; }
        public string AuthUsername { get; set; }
        public string RepoName { get; set; }
        public string RepoOwner { get; set; }
        public int RequestsRemaining { get; private set; }
        public HttpClient Client { get; private set; }
        public string RepoFolder { get; set; }

        private async void APIError(string content)
        {
            //TODO: implement Error handling

            //temp
            RequestsRemaining = 0;
        }

        /// <summary>
        /// Make a request to GitHub with nessasary checks, then parse the result into the specified type
        /// </summary>
        /// <param name="url">relative url to the requested resource</param>
        /// <typeparam name="T">Type the request is expected to contain</typeparam>
        private async Task<T> HttpGetAsync<T>(string url)
        {
            var content = await HttpGetAsync(url);
            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception e)
            {
                APIError(content);
                throw;
            }
        }

        private async Task<string> HttpGetAsync(string url)
        {
            //TODO: Handle Errors Here vv
            var response = await Client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                //TODO: Implement Check
            }
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<IList<PullRequest>> GetPullRequests()
        {
            //TODO: Investigate using the If-Modified-Since and If-None-Match headers https://developer.github.com/v3/#conditional-requests
            var url = $"/repos/{RepoOwner}/{RepoName}/pulls";
            var pullRequests = await HttpGetAsync<IList<PullRequest>>(url);

            return await Task.WhenAll(pullRequests.Select(async pr =>
            {
                var files = await HttpGetAsync<IList<Files>>($"/repos/{RepoOwner}/{RepoName}/pulls/{pr.Number}/files");
                pr.Files = files;
                return pr;
            }));
        }

        /// <summary>
        /// Download files for a given pull request.
        /// Store the files in the AppData folder with a subfolder for the pull request
        /// </summary>
        /// <param name="pr">the PullRequest for which the files need to be downloaded</param>
        public async Task DownloadFiles(PullRequest pr)
        {
            foreach (var current in pr.Files)
            {
                var csFileTokens = current.Filename.Split('.');
                if (csFileTokens.Last() == "cs")
                {
                    switch (current.Status)
                    {
                        case Files.StatusEnum.Added:
                        case Files.StatusEnum.Removed:
                            break;

                        case Files.StatusEnum.Modified:
                            var headTsk = DownloadFile(pr.Number, current.Filename, pr.Head.Sha);
                            var ancTsk = DownloadFile(pr.Number, current.Filename, pr.Base.Sha, isAncestor: true);
                            await Task.WhenAll(headTsk, ancTsk);
                            break;
                    }
                }
            }
        }

        private async Task DownloadFile(int prNum, string path, string sha, bool isAncestor = false)
        {
            var rawText = await HttpGetAsync($@"https://github.com/{RepoOwner}/{RepoName}/raw/{sha}/{path}");
            path = path.Replace('/', Path.DirectorySeparatorChar);
            var dir = Path.Combine(RepoFolder, $"{prNum}", path);

            if (isAncestor)
            {
                dir += ".orig";
            }
            new FileInfo(dir).Directory.Create();
            File.WriteAllText(dir, rawText);
        }

        public class PullRequest
        {
            public int Number { get; set; }
            public string State { get; set; }
            public bool Locked { get; set; }

            [JsonProperty("updated_at")]
            public DateTime Updated { get; set; }

            public User User { get; set; }
            public HeadBase Head { get; set; }
            public HeadBase Base { get; set; }
            public IList<Files> Files { get; set; }
        }

        public class Files
        {
            public string Filename { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public StatusEnum Status { get; set; }

            //[JsonProperty("raw_url")]
            //public string RawUrl { get; set; }
            public enum StatusEnum
            {
                Added,
                Modified,
                Removed,
            }
        }

        public class User
        {
            public string Login { get; set; }
        }

        private class GitHubError
        {
            public string Message { get; set; }
        }

        public class HeadBase
        {
            public string Label { get; set; }
            public string Ref { get; set; }
            public string Sha { get; set; }
        }

        struct GitHubConfiguration
        {
            readonly string authenticationToken;
            readonly string username;

            public GitHubConfiguration(AuthenticationSection section)
            {
                if (section == null)
                {
                    this.authenticationToken = null;
                    this.username = null;
                }
                else
                {
                    this.authenticationToken = section.Authentication.Token;
                    this.username = section.Authentication.Username;
                }
            }

            public string AuthenicationToken
            {
                get
                {
                    return this.authenticationToken;
                }
            }

            public string Username
            {
                get
                {
                    return this.username;
                }
            }
        }
    }
}
