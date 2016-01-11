using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;

namespace SemDiff.Test
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        public void LoggerMethodsTest()
        {
            Logger.Debug("Compliation Started For File 'blah'");
            Logger.Info("'40' of '60' requests have been used");
            Logger.Error("Unknown/unexpected error occured");

            //There is no way to look into the Trace from here, but if they haven't crashed that is a good sign

            //After running the test it should look something like this:
            //DEBUG: 2016-01-05T22:31:06 | "Compliation Started For File 'blah'" @ LoggerTests.cs:13 LoggerMethodsTest
            //INFO : 2016-01-05T22:31:06 | "'40' of '60' requests have been used" @ LoggerTests.cs:14 LoggerMethodsTest
            //ERROR: 2016-01-05T22:31:06 | "Unknown/unexpected error occured" @ LoggerTests.cs:15 LoggerMethodsTest
        }
    }
}