using MoreLinq;
using SemDiff.Core.Configuration;
using SemDiff.Core.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    /// <summary>
    /// Local representation of a github repo
    /// </summary>
    public class Repo
    {
        private static readonly ConcurrentDictionary<string, Repo> _repoLookup = new ConcurrentDictionary<string, Repo>();
        public static bool Authentication { get; set; } = true;
        public static TimeSpan MaxUpdateInterval { get; set; } = TimeSpan.FromMinutes(5);

        internal static GitHubConfiguration gitHubConfig =
            new GitHubConfiguration((AuthenticationSection)ConfigurationManager.GetSection("SemDiff.Core/authentication"));

        /// <summary>
        /// Looks for the git repo above the current file in the directory higherarchy. Null will be returned if no repo was found.
        /// </summary>
        /// <param name="filePath">Path to file in repo</param>
        /// <returns>Representation of repo or null (to indicate not found)</returns>
        public static Repo GetRepoFor(string filePath)
        {
            return _repoLookup.GetOrAdd(Path.GetDirectoryName(filePath), AddRepo);
        }

        internal static Repo AddRepo(string directoryPath)
        {
            Logger.Debug($"Dir: {directoryPath}");
            var gitconfig = Path.Combine(directoryPath, ".git", "config");
            if (File.Exists(gitconfig))
            {
                Logger.Info($"Git Config File Found: {gitconfig}");
                return RepoFromConfig(directoryPath, gitconfig);
            }
            else
            {
                //Go up a directory and check it out
                var parentDirectory = Path.GetDirectoryName(directoryPath);
                if (parentDirectory == null)
                {
                    //This file is not in a git repo! (GetDirectoryName returns null when given the root directory)
                    return null; //This is much more common than you might think, because offten random files are compiled, this will allow us to exclude them
                }
                return _repoLookup.GetOrAdd(parentDirectory, AddRepo);
            }
        }

        private static readonly Regex _gitHubUrl = new Regex(@"(git@|https:\/\/)github\.com(:|\/)(.*)\/(.*)");

        public string Owner { get; private set; }
        public string Name { get; private set; }
        public string LocalDirectory { get; private set; }
        public GitHub GitHubApi { get; private set; }
        public DateTime LastUpdate { get; internal set; } = DateTime.MinValue; //Old date insures update first time
        internal ImmutableDictionary<int, RemoteChanges> RemoteChangesData { get; set; } = ImmutableDictionary<int, RemoteChanges>.Empty;

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

        /// <summary>
        /// Flushes the internal mapings of directories to repos
        /// </summary>
        internal static void ClearLookup()
        {
            _repoLookup.Clear();
        }

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
                var authUsername = "haroldhues";
                var authToken = "9db4f2de497905dc5a5b2c597869a55a9ae05d9b";
#else
                var authUsername = gitHubConfig.Username;
                var authToken = gitHubConfig.AuthenicationToken;
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

        /// <summary>
        /// Gets Pull Requests and the master branch if it has been modified, this method also insures that we don't update more than MaxUpdateInterval
        /// </summary>
        public async Task UpdateRemoteChangesAsync()
        {
            var elapsedSinceUpdate = (DateTime.Now - LastUpdate);
            if (elapsedSinceUpdate > MaxUpdateInterval)
            {
                //TODO: Need a lock around this block so that if this method is called concurrently twice it will only make requests once.

                //Many Changes will be made to the Immutable Dictionary so we will use the builder interface
                var remChanges = RemoteChangesData.ToBuilder();

                var pulls = await GitHubApi.GetPullRequestsAsync();
                await Task.WhenAll(pulls.Select(GitHubApi.DownloadFilesAsync));
                foreach (var p in pulls)
                {
                    remChanges.Add(p.Number, p.ToRemoteChanges(GitHubApi.RepoFolder));
                }

                //Update our RemoteChangesData referace to new data
                RemoteChangesData = remChanges.ToImmutable();
                LastUpdate = DateTime.Now;
            }
        }
    }
}