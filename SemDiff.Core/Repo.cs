// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using LibGit2Sharp;
using Newtonsoft.Json;
using SemDiff.Core.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class Repo
    {
        private static readonly Regex _gitHubUrl = new Regex(@"(git@|https:\/\/)github\.com(:|\/)(.*)\/(.*)");
        private static readonly ConcurrentDictionary<string, Repo> _repoLookup = new ConcurrentDictionary<string, Repo>();
        private static readonly Regex nextLinkPattern = new Regex("<(http[^ ]*)>; *rel *= *\"next\"");

        public Repo(string gitDir, string repoOwner, string repoName, string authUsername = null, string authToken = null)
        {
            Logger.Info($"{nameof(Repo)}: {authUsername}:{authToken} for {repoOwner}\\{repoName}");
            Owner = repoOwner;
            RepoName = repoName;
            LocalGitDirectory = gitDir;
            EtagNoChanges = null;
            Client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })
            {
                BaseAddress = new Uri("https://api.github.com/")
            };
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(nameof(SemDiff));
            Client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
            CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff), Owner, RepoName);

            if (!string.IsNullOrWhiteSpace(authUsername) && !string.IsNullOrWhiteSpace(authToken))
            {
                AuthUsername = authUsername;
                AuthToken = authToken;
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{AuthUsername}:{AuthToken}")));
            }
            else
            {
                GetConfiguration();
            }
        }

        #region Move to config object

        public static TimeSpan MaxUpdateInterval { get; set; } = TimeSpan.FromMinutes(5);
        public string AuthToken { get; set; }
        public string AuthUsername { get; set; }

        #endregion Move to config object

        public string CacheDirectory { get; set; }
        public string CachedLocalPullRequestListPath => Path.Combine(CacheDirectory, "LocalList.json");
        public string ConfigFile { get; } = "User_Config.json";
        public HttpClient Client { get; private set; }
        public string EtagNoChanges { get; set; }
        public DateTime LastUpdate { get; internal set; } = DateTime.MinValue;
        public string LocalGitDirectory { get; }
        public string LocalRepoDirectory => Path.GetDirectoryName(Path.GetDirectoryName(LocalGitDirectory));
        public string Owner { get; set; }
        public LineEndingType LineEndings { get; set; }
        public List<PullRequest> PullRequests { get; } = new List<PullRequest>();
        public string RepoName { get; set; }

        public int RequestsLimit { get; private set; }

        public int RequestsRemaining { get; private set; }

        /// <summary>
        /// If the authentication file exists, it reads in the data.
        /// If the authentication file doesn't exist, it creates a blank copy.
        /// </summary>
        public void GetConfiguration()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff));
            Directory.CreateDirectory(path);
            path = Path.Combine(path, ConfigFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                try
                {
                    var auth = JsonConvert.DeserializeObject<Configuration>(json);
                    AuthUsername = auth.Username;
                    AuthToken = auth.AuthToken;
                    LineEndings = auth.LineEnding;
                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{AuthUsername}:{AuthToken}")));
                }
                catch (Exception ex)
                {
                    //Directory.Delete(CacheDirectory); This may be a good idea with a few more checks
                    Logger.Error($"{ex.GetType().Name}: Couldn't deserialize {path} because {ex.Message}");
                }
            }
            else
            {
                var newAuth = new Configuration();
                File.WriteAllText(path, JsonConvert.SerializeObject(newAuth, Formatting.Indented));
            }
        }

        /// <summary>
        /// Looks for the git repo above the current file in the directory hierarchy. Null will be returned if no repo was found.
        /// </summary>
        /// <param name="filePath">Path to file in repo</param>
        /// <returns>Representation of repo or null (to indicate not found)</returns>
        public static Repo GetRepoFor(string filePath)
        {
            var repoDir = Repository.Discover(filePath);
            if (repoDir == null)
            {
                return null;
            }
            return _repoLookup.GetOrAdd(repoDir, AddRepo);
        }

        /// <summary>
        /// Reads the pull requests from the persistent file
        /// </summary>
        public void GetCurrentSaved()
        {
            Debug.Assert(this != null);
            try
            {
                if (!Directory.Exists(CacheDirectory))
                    return;
                if (!File.Exists(CachedLocalPullRequestListPath))
                    return;
                var json = File.ReadAllText(CachedLocalPullRequestListPath);
                var list = JsonConvert.DeserializeObject<IEnumerable<PullRequest>>(json);
                PullRequests.Clear();
                foreach (var p in list)
                {
                    //Restore Self-Referential Loops
                    p.ParentRepo = this;
                    foreach (var r in p.Files)
                    {
                        r.ParentPullRequst = p;
                    }

                    if (VerifyPullRequestCache(p))
                    {
                        PullRequests.Add(p);
                        foreach (var r in p.Files)
                        {
                            r.LoadFromCache();
                        }
                    }
                    else
                    {
                        if (Directory.Exists(p.CacheDirectory))
                            Directory.Delete(p.CacheDirectory, true);
                    }
                }

                UpdateLocalSavedList(); //In case some were deleted, write the file back
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType().Name}: Couldn't load {CachedLocalPullRequestListPath} because {ex.Message}");
            }
        }

        private static bool VerifyPullRequestCache(PullRequest p)
        {
            if (!Directory.Exists(p.CacheDirectory))
                return false;

            foreach (var f in p.Files)
            {
                if (!File.Exists(f.CachePathBase))
                    return false;
                if (!File.Exists(f.CachePathHead))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets each page of the pull request list from GitHub. Once the list is complete, get all
        /// the pull request files for each pull request.
        /// </summary>
        /// <returns>List of pull request information.</returns>
        public async Task<IList<PullRequest>> GetPullRequestsAsync()
        {
            var url = $"/repos/{Owner}/{RepoName}/pulls";
            var etag = Ref.Create(EtagNoChanges);
            var updated = await GetPaginatedListAsync<PullRequest>(url, etag);
            var outdated = PullRequests;
            EtagNoChanges = etag.Value;
            if (updated == null)
            {
                return null;
            }
            if (outdated != null)
            {
                var prToRemove = outdated.Where(old => updated.All(newpr => newpr.Number != old.Number));
                foreach (var pr in updated)
                {
                    pr.ParentRepo = this;
                    var old = outdated.FirstOrDefault(o => o.Number == pr.Number);
                    if (old != null)
                    {
                        pr.LastWrite = old.LastWrite;
                    }
                }
                DeletePRsFromDisk(prToRemove);
            }
            await Task.WhenAll(updated.Select(async pr =>
            {
                var files = await GetPaginatedListAsync<RepoFile>($"/repos/{Owner}/{RepoName}/pulls/{pr.Number}/files");
                foreach (var f in files)
                {
                    f.ParentPullRequst = pr;
                }
                pr.Files = PullRequest.FilterFiles(files).ToList();
                return pr;
            }));
            PullRequests.Clear();
            PullRequests.AddRange(updated);
            UpdateLocalSavedList();
            return updated;
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
            Directory.CreateDirectory(CacheDirectory);
            File.WriteAllText(CachedLocalPullRequestListPath, JsonConvert.SerializeObject(PullRequests, Formatting.Indented));
        }

        /// <summary>
        /// Gets Pull Requests and the master branch if it has been modified, this method also insures that we don't update more than MaxUpdateInterval
        /// </summary>
        public async Task UpdateRemoteChangesAsync()
        {
            lock (this)
            {
                var elapsedSinceUpdate = (DateTime.Now - LastUpdate);
                if (elapsedSinceUpdate <= MaxUpdateInterval)
                {
                    return;
                }
                LastUpdate = DateTime.Now;
            }

            var pulls = await GetPullRequestsAsync();
            if (pulls == null)
            {
                return;
            }

            await Task.WhenAll(pulls.Select(p => p.GetFilesAsync()));
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
            var dir = Path.Combine(repofolder, $"{prNum}", path.ToLocalPath());

            if (isAncestor)
            {
                dir += ".orig";
            }

            return dir;
        }

        internal bool FileChangedLocally(string filePath)
        {
            using (var repo = new Repository(LocalGitDirectory))
            {
                var fileStatus = repo.RetrieveStatus(filePath);
                switch (fileStatus)
                {
                    case FileStatus.NewInIndex:
                    case FileStatus.ModifiedInIndex:
                    case FileStatus.RenamedInIndex:
                    case FileStatus.TypeChangeInIndex:
                    case FileStatus.NewInWorkdir:
                    case FileStatus.ModifiedInWorkdir:
                    case FileStatus.TypeChangeInWorkdir:
                    case FileStatus.RenamedInWorkdir:
                    case FileStatus.Conflicted:
                        return true;

                    //These are odd in that they shouldn't actually happen
                    case FileStatus.Nonexistent:
                    case FileStatus.DeletedFromIndex:
                    case FileStatus.DeletedFromWorkdir:
                        return true;

                    case FileStatus.Unreadable:
                    case FileStatus.Ignored:
                    case FileStatus.Unaltered:
                        return false;

                    default:
                        throw new NotImplementedException($"{fileStatus}");
                }
            }
        }

        internal async Task<IList<T>> GetPaginatedListAsync<T>(string url, Ref<string> etag = null)
        {
            var pagination = Ref.Create<string>(null);
            var first = await HttpGetAsync<IList<T>>(url, etag, pagination);
            if (first == null) //Etag
            {
                return null;
            }

            var list = new List<T>(first);
            while (pagination.Value != null)
            {
                var next = await HttpGetAsync<IList<T>>(pagination.Value, pages: pagination);
                list.AddRange(next);
            }
            return list;
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
        internal async Task<T> HttpGetAsync<T>(string url, Ref<string> etag = null, Ref<string> pages = null) where T : class
        {
            var content = await HttpGetAsync(url, etag, pages);
            if (content == null)
                return null;
            return DeserializeWithErrorHandling<T>(content);
        }

        internal async Task<string> HttpGetAsync(string url, Ref<string> etag = null, Ref<string> pages = null)
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

        private static Repo AddRepo(string repoDir)
        {
            using (var r = new Repository(repoDir))
            {
                var matchingUrls = r.Network.Remotes
                    .Select(remote => _gitHubUrl.Match(remote.Url))
                    .Where(m => m.Success);
                var match = matchingUrls.FirstOrDefault();
                if (match == null)
                {
                    Logger.Error(nameof(GitHubUrlNotFoundException));
                    throw new GitHubUrlNotFoundException(path: repoDir);
                }

                var url = match.Value.Trim();
                var owner = match.Groups[3].Value.Trim();
                var name = match.Groups[4].Value.Trim();
                if (name.EndsWith(".git"))
                {
                    name = name.Substring(0, name.Length - 4);
                }
                Logger.Debug($"Repo: Owner='{owner}' Name='{name}' Url='{url}'");
                var repo = new Repo(repoDir, owner, name);
                repo.GetCurrentSaved();
                return repo;
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
                var dir = Path.Combine(CacheDirectory, $"{pr.Number}");
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