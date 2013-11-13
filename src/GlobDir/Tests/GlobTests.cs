using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using GlobDir;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class GlobTests
    {
        [Test]
        public void CanGetAllTextFilesInAllSubdirs()
        {
            var pattern = Path.Combine("**", "*.txt");
            var matches = GetMatches(pattern);
            matches.Count.Should().Be(5);
        }

        [Test]
        public void CanGetAllTextFilesThatStartWithFileInAllSubdirs()
        {
            var pattern = Path.Combine("**", "File*.txt");
            var matches = GetMatches(pattern);
            matches.Count.Should().Be(4);
        }

        [Test]
        public void CanGetAllTextFilesThatStartWithFileInSubDirsThatStartWithDir()
        {
            var pattern = Path.Combine("**", "Dir?", "File*.txt");
            var matches = GetMatches(pattern);
            matches.Count.Should().Be(4);
        }
        [Test]
        public void CanGetZeroTextFilesThatStartWithFileInSubDirsThatStartWithNonExistentName()
        {
            var pattern = Path.Combine("**/WrongDir*", "File*.txt");
            var matches = GetMatches(pattern);
            matches.Count.Should().Be(0);
        }

        [Test]
        public void CanGetAllTextFilesNamedOtherFileInSubdirDirA()
        {
            var pattern = Path.Combine("**", "dirA", "OtherFile?.*");
            var matches = GetMatches(pattern);
            matches.Count.Should().Be(2);
        }

        [Test]
        public void CanGetAllTextFilesNamedOtherFileLogInSubdirDirA()
        {
            var pattern = Path.Combine("**", "dirA", "OtherFile1.log");
            var matches = GetMatches(pattern);
            matches.Count.Should().Be(1);
            matches.First().Should().BeEquivalentTo(Path.Combine(GetGlobTestDir(), "dirA", "OtherFile1.log"));
        }

        private static List<string> GetMatches(string pattern)
        {
            var globTestDir = GetGlobTestDir() + Path.DirectorySeparatorChar;
            var matches = Glob.GetMatches(string.Format("{0}" + pattern, globTestDir)).ToList();
            return matches;
        }

        private static string GetGlobTestDir()
        {
            var globTestDir = Path.Combine(GetTestFilesLocation(), "GlobTestFiles");
            return globTestDir;
        }

        private static string GetTestFilesLocation()
        {
            return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }
    }
}
