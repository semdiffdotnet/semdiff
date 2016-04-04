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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    /// <summary>
    /// Contains all the methods and classes necessary to use the GitHub api to pull down data about
    /// pull requests. Additionally, contains the logic for authenticating
    /// </summary>
    public class GitHub
    {
        private static readonly Regex nextLinkPattern = new Regex("<(http[^ ]*)>; *rel *= *\"next\"");

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
            GetCurrentSaved();
        }

        public string AuthToken { get; set; }
        public string AuthUsername { get; set; }
        public HttpClient Client { get; private set; }
        public IList<PullRequest> CurrentSaved { get; set; }
        public string EtagNoChanges { get; set; }
        public string JsonFileName { get; } = "LocalList.json";
        public string RepoFolder { get; set; }
        public string RepoName { get; set; }
        public string RepoOwner { get; set; }
        public int RequestsLimit { get; private set; }
        public int RequestsRemaining { get; private set; }

        /// <summary>
        /// Download files for a given pull request. Store the files in the AppData folder with a
        /// sub-folder for the pull request
        /// </summary>
        /// <param name="pr">the PullRequest for which the files need to be downloaded</param>
        public async Task DownloadFilesAsync(PullRequest pr)
        {
            if (pr.LastWrite >= pr.Updated)
                return;
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

                        case Files.StatusEnum.Changed:
                            Logger.Info($"The mythical 'changed' status has occured! {pr.Number}:{current.Filename}");
                            goto case Files.StatusEnum.Modified; //Effectivly falls through to the following

                        case Files.StatusEnum.Modified:
                            var headTsk = DownloadFileAsync(pr.Number, current.Filename, pr.Head.Sha);
                            var ancTsk = DownloadFileAsync(pr.Number, current.Filename, pr.Base.Sha, isAncestor: true);
                            await Task.WhenAll(headTsk, ancTsk);
                            break;
                    }
                }
            }
            pr.LastWrite = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets each page of the pull request list from GitHub. Once the list is complete, get all
        /// the pull request files for each pull request.
        /// </summary>
        /// <returns>List of pull request information.</returns>
        public async Task<IList<PullRequest>> GetPullRequestsAsync()
        {
            var url = $"/repos/{RepoOwner}/{RepoName}/pulls";
            var etag = Ref.Create(EtagNoChanges);
            var pagination = Ref.Create<string>(null);
            var paginationPRs = await HttpGetAsync<IList<PullRequest>>(url, etag, pagination);
            EtagNoChanges = etag.Value;
            var pullRequests = paginationPRs;
            while (paginationPRs != null)
            {
                paginationPRs = null;
                if (pagination.Value != null)
                {
                    paginationPRs = await HttpGetAsync<IList<PullRequest>>(pagination.Value, pages: pagination);
                    foreach (var cur in paginationPRs)
                    {
                        pullRequests.Add(cur);
                    }
                }
            }
            if (pullRequests == null)
            {
                return null;
            }
            if (CurrentSaved != null)
            {
                var removePRs = CurrentSaved;
                foreach (var pr in pullRequests)
                {
                    foreach (var prRemove in removePRs)
                    {
                        if (pr.Number == prRemove.Number)
                        {
                            pr.LastWrite = prRemove.LastWrite;
                            removePRs.Remove(prRemove);
                            break;
                        }
                    }
                }
                DeletePRsFromDisk(removePRs);
            }
            CurrentSaved = pullRequests;
            return await Task.WhenAll(pullRequests.Select(async pr =>
            {
                var filePagination = Ref.Create<string>(null);
                var files = await HttpGetAsync<IList<Files>>($"/repos/{RepoOwner}/{RepoName}/pulls/{pr.Number}/files", pages: filePagination);
                pr.Files = files;
                files = null;
                while (filePagination.Value != null)
                {
                    files = await HttpGetAsync<IList<Files>>(filePagination.Value, pages: filePagination);
                    foreach (var cur in files)
                    {
                        pr.Files.Add(cur);
                    }
                }
                return pr;
            }));
        }

        /// <summary>
        /// Makes a request to GitHub to update RequestsRemaining and RequestsLimit
        /// </summary>
        public Task UpdateLimitAsync()
        {
            return HttpGetAsync("/rate_limit");
        }

        /// <summary>
        /// Write the current internal list of pull requests to a file
        /// </summary>
        public void UpdateLocalSavedList()
        {
            var path = RepoFolder.Replace('/', Path.DirectorySeparatorChar);
            path = Path.Combine(path, JsonFileName);
            new FileInfo(path).Directory.Create();
            File.WriteAllText(path, JsonConvert.SerializeObject(CurrentSaved));
        }

        /// <summary>
        /// Construct the absolute path of a file in a pull request
        /// </summary>
        /// <param name="repofolder">the path to the repo in the AppData folder</param>
        /// <param name="prNum">number property of pull request</param>
        /// <param name="path">the relative path of the file in the repo</param>
        /// <param name="isAncestor">if true appends the additional orig extension</param>
        /// <returns></returns>
        internal static string GetPathInCache(string repofolder, int prNum, string path, bool isAncestor = false)
        {
            var dir = Path.Combine(repofolder, $"{prNum}", path.Replace('/', '\\'));

            if (isAncestor)
            {
                dir += ".orig";
            }

            return dir;
        }

        /// <summary>
        /// Reads the pull requests from the persistent file
        /// </summary>
        public void GetCurrentSaved()
        {
            try
            {
                var path = RepoFolder.Replace('/', Path.DirectorySeparatorChar);
                path = Path.Combine(path, JsonFileName);
                var json = File.ReadAllText(path);
                CurrentSaved = JsonConvert.DeserializeObject<IList<PullRequest>>(json);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType().Name}: Couldn't load {JsonFileName} because {ex.Message}");
            }
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

        private static string ParseNextLink(IEnumerable<string> links)
        {
            foreach (var l in links)
            {
                var match = nextLinkPattern.Match(l);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            return null;
        }

        private void DeletePRsFromDisk(IEnumerable<PullRequest> prs)
        {
            foreach (var pr in prs)
            {
                var dir = Path.Combine(RepoFolder, $"{pr.Number}");
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception ex)
                {
                    Logger.Error($"{ex.GetType().Name}: Couldn't delete {dir} because {ex.Message}");
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

        /// <summary>
        /// Make a request to GitHub with necessary checks, then parse the result into the specified type
        /// </summary>
        /// <param name="url">relative url to the requested resource</param>
        /// <param name="etag">
        /// An optional special object that contains a string that can be changed, if the parameter
        /// is present and the string inside is not null it will be placed in the IfNotModified
        /// HttpHeader otherwise if it is present but the string inside is null then any Etag found
        /// on the response header will be set in the object
        /// </param>
        /// <param name="pages">
        /// if not null and the response had a link to a next page the value inside will be set to
        /// the url of the next page
        /// </param>
        /// <typeparam name="T">Type the request is expected to contain</typeparam>
        /// <returns>
        /// A Task that once awaited will result in the pages contents being deserialized into the
        /// return object
        /// </returns>
        private async Task<T> HttpGetAsync<T>(string url, Ref<string> etag = null, Ref<string> pages = null) where T : class
        {
            var content = await HttpGetAsync(url, etag, pages);
            if (content == null)
                return null;
            return DeserializeWithErrorHandling<T>(content);
        }

        private async Task<string> HttpGetAsync(string url, Ref<string> etag = null, Ref<string> pages = null)
        {
            //Request, but retry once waiting 5 minutes
            Client.DefaultRequestHeaders.IfNoneMatch.Clear();
            if (etag?.Value != null)
            {
                Client.DefaultRequestHeaders.IfNoneMatch.Add(EntityTagHeaderValue.Parse(etag.Value));
            }
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
            if (etag != null && response.Headers.TryGetValues("ETag", out headerVal))
            {
                etag.Value = headerVal.Single();
            }
            if (pages != null)
            {
                pages.Value = response.Headers.TryGetValues("Link", out headerVal) ? ParseNextLink(headerVal) : null;
            }
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Object used for parsing that reflects the json of the object that GitHub uses for files
        /// </summary>
        public class Files
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

        /// <summary>
        /// Object used for parsing that reflects the json of the object that GitHub uses for Pull Requests
        /// </summary>
        public class PullRequest
        {
            public HeadBase Base { get; set; }
            public IList<Files> Files { get; set; }
            public HeadBase Head { get; set; }
            public DateTime LastWrite { get; set; } = DateTime.MinValue;
            public bool Locked { get; set; }
            public int Number { get; set; }
            public string State { get; set; }
            public string Title { get; set; }

            [JsonProperty("updated_at")]
            public DateTime Updated { get; set; }

            [JsonProperty("html_url")]
            public string Url { get; set; }

            public User User { get; set; }

            internal RemoteChanges ToRemoteChanges(string repofolder)
            {
                return new RemoteChanges
                {
                    Date = Updated,
                    Title = Title,
                    Url = Url,
                    Files = Files
                        .Where(f => f.Status == GitHub.Files.StatusEnum.Modified || f.Status == GitHub.Files.StatusEnum.Changed)
                        .Where(f => f.Filename.Split('.').Last() == "cs")
                        .Select(f => f.ToRemoteFile(repofolder, Number))
                        .ToList(),
                };
            }
        }

        /// <summary>
        /// Object used for parsing that reflects the json object that GitHub uses to represent users.
        /// </summary>
        public class User
        {
            public string Login { get; set; }
        }

        /// <summary>
        /// Object used for parsing that reflects the json of the response from GitHub in an error
        /// </summary>
        internal class GitHubError
        {
            [JsonProperty("documentation_url")]
            public string DocumentationUrl { get; set; }

            public string Message { get; set; }

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