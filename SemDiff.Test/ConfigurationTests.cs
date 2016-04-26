namespace SemDiff.Test
{
    using Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    [TestClass]
    public class GitHubAuthTest : TestBase
    {
        private string owner = "semdiffdotnet";
        private string repository = "curly-broccoli";
        private const string authUsername = "haroldhues";
        private const string authToken = "9db4f2de497905dc5a5b2c597869a55a9ae05d9b";
        private Repo repo;

        public GitHubAuthTest()
        {
            var repoLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff),"semdiffdotnet", repository);
            Directory.CreateDirectory(repoLoc);
            repo = new Repo(repoLoc, owner, repository);
        }

        [TestInitialize]
        public void TestInit()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff), repo.ConfigFile);
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff)));
            File.Delete(path);
            var auth = new Core.Configuration();
            auth.AuthToken = authToken;
            auth.Username = authUsername;
            var json = JsonConvert.SerializeObject(auth, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        [TestMethod]
        public async Task AuthorizedPullRequests()
        {
<<<<<<< HEAD:SemDiff.Test/GitHubAuthTests.cs
            repo.GetAuthentication();
            await repo.UpdateLimitAsync();
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff), repo.ConfigFile);
=======
            github.GetConfiguration();
            await github.UpdateLimitAsync();
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff), github.ConfigFile);
>>>>>>> master:SemDiff.Test/ConfigurationTests.cs
            File.Delete(path);
            Assert.IsTrue(repo.RequestsLimit > 60);
        }
        private void LineEndingTest(string fileData, LineEndingType ending)
        {
            var path = Path.Combine(repo.CacheDirectory, "test.json");
            Directory.CreateDirectory(repo.CacheDirectory);
            File.WriteAllText(path, fileData);
            var json = File.ReadAllText(path);
            var auth = JsonConvert.DeserializeObject<Core.Configuration>(json);
            File.Delete(path);
            Assert.IsTrue(auth.LineEnding == ending);
        }
        [TestMethod]
        public void TestInvalidLineEnding()
        {
            LineEndingTest("{\n   \"username\": null,\n   \"authtoken\": null,\n   \"line_ending\": \"lfsadf\"\n}", LineEndingType.crlf);
        }
        [TestMethod]
        public void TestlfLineEnding()
        {
            //upper case
            LineEndingTest("{\n   \"username\": null,\n   \"authtoken\": null,\n   \"line_ending\": \"LF\"\n}", LineEndingType.lf);
            //lower case
            LineEndingTest("{\n   \"username\": null,\n   \"authtoken\": null,\n   \"line_ending\": \"lf\"\n}", LineEndingType.lf);
            //mixed case
            LineEndingTest("{\n   \"username\": null,\n   \"authtoken\": null,\n   \"line_ending\": \"Lf\"\n}", LineEndingType.lf);
        }
        [TestMethod]
        public void TestcrlfLineEnding()
        {
            //upper case
            LineEndingTest("{\n   \"username\": null,\n   \"authtoken\": null,\n   \"line_ending\": \"CRLF\"\n}", LineEndingType.crlf);
            //lower case
            LineEndingTest("{\n   \"username\": null,\n   \"authtoken\": null,\n   \"line_ending\": \"crlf\"\n}", LineEndingType.crlf);
            //mixed case
            LineEndingTest("{\n   \"username\": null,\n   \"authtoken\": null,\n   \"line_ending\": \"cRlF\"\n}", LineEndingType.crlf);
        }
    }
}