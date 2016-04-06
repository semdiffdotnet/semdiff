using SemDiff.Core.Configuration;
using SemDiff.Core.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    /// <summary>
    /// Local representation of a GitHub repo
    /// </summary>
    public class Repo
    {
        internal static readonly GitHubConfiguration gitHubConfig =
            new GitHubConfiguration((AuthenticationSection)ConfigurationManager.GetSection("SemDiff.Core/authentication"));

        private static readonly Regex _gitHubUrl = new Regex(@"(git@|https:\/\/)github\.com(:|\/)(.*)\/(.*)");
        private static readonly ConcurrentDictionary<string, Repo> _repoLookup = new ConcurrentDictionary<string, Repo>();

        /// <summary>
        /// Repo Constructor, also see the static Authentication flag
        /// </summary>
        /// <param name="directory">Path to the repo on disk (the one open in visual studio)</param>
        /// <param name="owner">if repo is github.com/haroldhues/awesomeapp/ then haroldhues is the owner</param>
        /// <param name="name">if repo is github.com/haroldhues/awesomeapp/ then awesomeapp is the name</param>
        internal Repo(string directory, string owner, string name)
        {
            if (Authentication)
            {
#if DEBUG
                const string authUsername = "haroldhues";
                const string authToken = "9db4f2de497905dc5a5b2c597869a55a9ae05d9b";
#else
                const string authUsername = gitHubConfig.Username;
                const string authToken = gitHubConfig.AuthenicationToken;
#endif
                GitHubApi = new GitHub(owner, name, authUsername, authToken);
            }
            else
            {
                GitHubApi = new GitHub(owner, name);
            }
            Owner = owner;
            Name = name;
            LocalDirectory = directory;
        }

        public static bool Authentication { get; set; } = true;

        public static TimeSpan MaxUpdateInterval { get; set; } = TimeSpan.FromMinutes(5);

        public GitHub GitHubApi { get; private set; }

        public DateTime LastUpdate { get; internal set; } = DateTime.MinValue;

        public string LocalDirectory { get; }

        public string Name { get; }

        public string Owner { get; }

        //Old date insures update first time
        internal ImmutableDictionary<int, RemoteChanges> RemoteChangesData { get; set; } = ImmutableDictionary<int, RemoteChanges>.Empty;

        /// <summary>
        /// Looks for the git repo above the current file in the directory hierarchy. Null will be returned if no repo was found.
        /// </summary>
        /// <param name="filePath">Path to file in repo</param>
        /// <returns>Representation of repo or null (to indicate not found)</returns>
        public static Repo GetRepoFor(string filePath)
        {
            return _repoLookup.GetOrAdd(Path.GetDirectoryName(filePath), AddRepo);
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

            var pulls = await GitHubApi.GetPullRequestsAsync();
            if (pulls == null)
            {
                return;
            }

            await Task.WhenAll(pulls.Select(GitHubApi.DownloadFilesAsync));

            //Many Changes will be made to the Immutable Dictionary so we will use the builder interface
            var remChanges = RemoteChangesData.ToBuilder();

            foreach (var p in pulls)
            {
                remChanges[p.Number] = p.ToRemoteChanges(GitHubApi.RepoFolder);
            }

            //Update our RemoteChangesData reference to new data
            RemoteChangesData = remChanges.ToImmutable();
        }

        internal static Repo AddRepo(string directoryPath)
        {
            Logger.Debug($"Dir: {directoryPath}");
            var gitconfig = Path.Combine(directoryPath, ".git", "config");
            if (File.Exists(gitconfig))
            {
                Logger.Info($".gitconfig File Found: {gitconfig}");
                return RepoFromConfig(directoryPath, gitconfig);
            }
            else
            {
                //Go up a directory and check it out
                var parentDirectory = Path.GetDirectoryName(directoryPath);
                if (parentDirectory == null)
                {
                    //This file is not in a git repo! (GetDirectoryName returns null when given the root directory)
                    return null; //This is much more common than you might think, because often random files are compiled, this will allow us to exclude them
                }
                return _repoLookup.GetOrAdd(parentDirectory, AddRepo);
            }
        }

        /// <summary>
        /// Flushes the internal mappings of directories to repos
        /// </summary>
        internal static void ClearLookup()
        {
            _repoLookup.Clear();
        }

        internal static Repo RepoFromConfig(string repoDir, string gitconfigPath)
        {
            var config = File.ReadAllText(gitconfigPath);
            var match = _gitHubUrl.Match(config);
            if (!match.Success)
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
            return new Repo(repoDir, owner, name);
        }
    }
}