using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        public GitHub(string repoOwner, string repoName)
        {
            this.RepoOwner = repoOwner;
            this.RepoName = repoName;

            Client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })
            {
                BaseAddress = new Uri("https://api.github.com/")
            };
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(nameof(SemDiff));
            Client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

            RepoFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SemDiff), RepoOwner, RepoName);
        }

        public GitHub(string repoOwner, string repoName, string authUsername, string authToken) : this(repoOwner, repoName)
        {
            AuthUsername = authUsername;
            AuthToken = authToken;
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{AuthUsername}:{AuthToken}")));
        }

        public string AuthToken { get; set; }
        public string AuthUsername { get; set; }
        public string RepoName { get; set; }
        public string RepoOwner { get; set; }
        public int RequestsRemaining { get; private set; }
        public int RequestsLimit { get; private set; }
        public HttpClient Client { get; private set; }
        public string RepoFolder { get; set; }

        /// <summary>
        /// Makes a request to github to update RequestsRemaining and RequestsLimit
        /// </summary>
        public Task UpdateLimit()
        {
            return HttpGetAsync("/rate_limit");
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
                throw;
            }
        }

        private async Task<string> HttpGetAsync(string url)
        {
            //Request, but retry once waiting 5 minutes
            var response = await Extensions.RetryOnce(() => Client.GetAsync(url), TimeSpan.FromMinutes(5));
            IEnumerable<string> headerVal;
            if (response.Headers.TryGetValues("X-RateLimit-Limit", out headerVal))
            {
                RequestsLimit = int.Parse(headerVal.Single());
            }
            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out headerVal))
            {
                RequestsRemaining = int.Parse(headerVal.Single());
            }
            if (!response.IsSuccessStatusCode)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        throw new UnauthorizedAccessException("Authentication Failure");
                    case HttpStatusCode.Forbidden:
                        throw new UnauthorizedAccessException("Rate Limit Exceeded");
                    default:
                        throw new NotImplementedException();
                }
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
            string dir = GetPathInCache(RepoFolder, prNum, path, isAncestor);
            new FileInfo(dir).Directory.Create();
            File.WriteAllText(dir, rawText);
        }

        private static string GetPathInCache(string repofolder, int prNum, string path, bool isAncestor = false)
        {
            var dir = Path.Combine(repofolder, $"{prNum}", path);

            if (isAncestor)
            {
                dir += ".orig";
            }

            return dir;
        }

        public class PullRequest
        {
            public int Number { get; set; }
            public string State { get; set; }
            public string Title { get; set; }
            public bool Locked { get; set; }

            [JsonProperty("updated_at")]
            public DateTime Updated { get; set; }

            public User User { get; set; }
            public HeadBase Head { get; set; }
            public HeadBase Base { get; set; }
            public IList<Files> Files { get; set; }

            internal RemoteChanges ToRemoteChanges(string repofolder)
            {
                return new RemoteChanges
                {
                    Date = Updated,
                    Title = Title,
                    Files = Files.Where(f => f.Status == GitHub.Files.StatusEnum.Modified).Where(f => f.Filename.Split('.').Last() == "cs").Select(f => f.ToRemoteFile(repofolder, Number)).ToList(),
                };
            }
        }

        public class Files
        {
            public string Filename { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public StatusEnum Status { get; set; }

            internal RemoteFile ToRemoteFile(string repofolder, int num)
            {
                var baseP = GetPathInCache(repofolder, num, Filename, isAncestor: true);
                var fileP = GetPathInCache(repofolder, num, Filename, isAncestor: false);
                return new RemoteFile
                {
                    Filename = Filename,
                    Base = CSharpSyntaxTree.ParseText(File.ReadAllText(baseP), path: baseP),
                    File = CSharpSyntaxTree.ParseText(File.ReadAllText(fileP), path: fileP)
                };
            }

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
    }
}