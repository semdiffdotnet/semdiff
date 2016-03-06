using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SemDiff.Core.Exceptions;
using System;
using System.Collections.Generic;
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
        public GitHub(string repoOwner, string repoName, string authUsername = null, string authToken = null)
        {
            Logger.Info($"{nameof(GitHub)}: {authUsername}:{authToken} for {repoOwner}\\{repoName}");
            RepoOwner = repoOwner;
            RepoName = repoName;
            EtagNoChanges = null;
            Client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })
            {
                BaseAddress = new Uri("https://api.github.com/")
            };
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(nameof(SemDiff));
            Client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

            RepoFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SemDiff), RepoOwner, RepoName);

            if (!string.IsNullOrWhiteSpace(authUsername) && !string.IsNullOrWhiteSpace(authToken))
            {
                AuthUsername = authUsername;
                AuthToken = authToken;
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{AuthUsername}:{AuthToken}")));
            }
        }

        public string AuthToken { get; set; }
        public string AuthUsername { get; set; }
        public string RepoName { get; set; }
        public string RepoOwner { get; set; }
        public int RequestsRemaining { get; private set; }
        public int RequestsLimit { get; private set; }
        public HttpClient Client { get; private set; }
        public string RepoFolder { get; set; }
        public string EtagNoChanges { get; set; }

        /// <summary>
        /// Makes a request to github to update RequestsRemaining and RequestsLimit
        /// </summary>
        public Task UpdateLimitAsync()
        {
            return HttpGetAsync("/rate_limit");
        }

        /// <summary>
        /// Make a request to GitHub with nessasary checks, then parse the result into the specified type
        /// </summary>
        /// <param name="url">relative url to the requested resource</param>
        /// <typeparam name="T">Type the request is expected to contain</typeparam>
        private async Task<T> HttpGetAsync<T>(string url) where T : class
        {
            var content = await HttpGetAsync(url);
            if (content == null)
                return null;
            return DeserializeWithErrorHandling<T>(content);
        }

        private async Task<string> HttpGetAsync(string url)
        {
            //Request, but retry once waiting 5 minutes
            if(EtagNoChanges != null)
                Client.DefaultRequestHeaders.Add("If-None-Match", EtagNoChanges);
            var response = await Extensions.RetryOnceAsync(() => Client.GetAsync(url), TimeSpan.FromMinutes(5));
            IEnumerable<string> headerVal;
            if (response.Headers.TryGetValues("X-RateLimit-Limit", out headerVal))
            {
                RequestsLimit = int.Parse(headerVal.Single());
            }
            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out headerVal))
            {
                RequestsRemaining = int.Parse(headerVal.Single());
            }
            if (response.Headers.TryGetValues("ETag", out headerVal))
            {
                EtagNoChanges = headerVal.Single();
            }
            if (!response.IsSuccessStatusCode)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        var unauth = await response.Content.ReadAsStringAsync();
                        var unauthorizedError = DeserializeWithErrorHandling<GitHubError>(unauth);
                        Logger.Error($"{nameof(GitHubAuthenticationFailureException)}: {unauthorizedError.Message}");
                        throw new GitHubAuthenticationFailureException();
                    case HttpStatusCode.Forbidden:
                        var forbid = await response.Content.ReadAsStringAsync();
                        var forbidError = DeserializeWithErrorHandling<GitHubError>(forbid);
                        Logger.Error($"{nameof(GitHubRateLimitExceededException)}: {forbidError.Message}");
                        throw new GitHubRateLimitExceededException();
                    case HttpStatusCode.NotModified:
                        //Returns null because we have nothing to update if nothing was modified
                        return null;
                    default:
                        var str = await response.Content.ReadAsStringAsync();
                        var error = DeserializeWithErrorHandling<GitHubError>(str);
                        throw error.ToException();
                }
            }
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<IList<PullRequest>> GetPullRequestsAsync()
        {
            //TODO: Investigate using the If-Modified-Since and If-None-Match headers https://developer.github.com/v3/#conditional-requests
            var url = $"/repos/{RepoOwner}/{RepoName}/pulls";
            var pullRequests = await HttpGetAsync<IList<PullRequest>>(url);
            if(pullRequests == null)
            {
                return null;
            }
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
        public async Task DownloadFilesAsync(PullRequest pr)
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
                        case Files.StatusEnum.Renamed: //Not sure how to handle this one...
                            break;

                        case Files.StatusEnum.Modified:
                            var headTsk = DownloadFileAsync(pr.Number, current.Filename, pr.Head.Sha);
                            var ancTsk = DownloadFileAsync(pr.Number, current.Filename, pr.Base.Sha, isAncestor: true);
                            await Task.WhenAll(headTsk, ancTsk);
                            break;
                    }
                }
            }
        }

        private async Task DownloadFileAsync(int prNum, string path, string sha, bool isAncestor = false)
        {
            var rawText = await HttpGetAsync($@"https://github.com/{RepoOwner}/{RepoName}/raw/{sha}/{path}");
            path = path.Replace('/', Path.DirectorySeparatorChar);
            var dir = GetPathInCache(RepoFolder, prNum, path, isAncestor);
            new FileInfo(dir).Directory.Create();
            File.WriteAllText(dir, rawText);
        }

        internal static string GetPathInCache(string repofolder, int prNum, string path, bool isAncestor = false)
        {
            var dir = Path.Combine(repofolder, $"{prNum}", path);

            if (isAncestor)
            {
                dir += ".orig";
            }

            return dir;
        }

        private static T DeserializeWithErrorHandling<T>(string content)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception ex)
            {
                Logger.Error($"{nameof(GitHubDeserializationException)}: {ex.Message}");
                throw new GitHubDeserializationException(ex);
            }
        }

        public class PullRequest
        {
            public int Number { get; set; }
            public string State { get; set; }
            public string Title { get; set; }
            public bool Locked { get; set; }

            [JsonProperty("updated_at")]
            public DateTime Updated { get; set; }

            [JsonProperty("html_url")]
            public string Url { get; set; }

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
                    Url = Url,
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
                Renamed
            }
        }

        public class User
        {
            public string Login { get; set; }
        }

        public class HeadBase
        {
            public string Label { get; set; }
            public string Ref { get; set; }
            public string Sha { get; set; }
        }

        internal class GitHubError
        {
            public string Message { get; set; }

            [JsonProperty("documentation_url")]
            public string DocumentationUrl { get; set; }

            internal Exception ToException()
            {
                Logger.Error($"{nameof(GitHubUnknownErrorException)}: {Message}");
                return new GitHubUnknownErrorException(
                        string.IsNullOrWhiteSpace(DocumentationUrl)
                        ? Message
                        : $"({Message})[{DocumentationUrl}]"
                        );
            }
        }
    }
}