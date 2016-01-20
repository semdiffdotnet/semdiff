using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System.Collections.Generic;
namespace SemDiff.Test
{
    [TestClass]
    public class PullRequestListTest
    {
        [TestMethod]
        public void PullRequestFromTestRepo()
        {
            var gh = new GitHub("semdiffdotnet", "curly-broccoli");
            var req = gh.GetPullRequests();
        }
    }
}
