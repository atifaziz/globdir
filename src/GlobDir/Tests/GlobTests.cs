using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
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
            matches.First().Should().BeEquivalentTo(RootPath + Path.Combine("dirA", "OtherFile1.log"));
        }

        private static ICollection<string> GetMatches(string pattern)
        {
            var matches = Glob.GetMatches(Volume, RootPath + pattern);
            return matches.Select(m => Volume.GetPath(m)).ToList();
        }

        static XElement Dir(string name, params XElement[] items) { return new XElement("dir", new XAttribute("name", name), items); }
        static XElement File(string name) { return new XElement("file", new XAttribute("name", name)); }

        static readonly string RootPath = new string(Path.DirectorySeparatorChar, 1);
        static readonly PlatformAdaptationLayer<XElement> Volume = new TestPlatformAdaptationLayer(
            Dir(string.Empty,
                Dir("dirA", 
                    File("File1.log"),
                    File("File1.txt"),
                    File("File2.log"),
                    File("File2.txt"),
                    File("OtherFile1.log"),
                    File("OtherFile1.txt")
                ),
                Dir("dirB",
                    File("File1.log"),
                    File("File1.txt"),
                    File("File2.log"),
                    File("File2.txt")
                )
            ));

        sealed class TestPlatformAdaptationLayer : PlatformAdaptationLayer<XElement>
        {
            readonly XElement _root;

            public TestPlatformAdaptationLayer(XElement root) { _root = root; }
            
            public override string GetPath(XElement item)
            {
                var path = string.Empty;
                for (var dir = item; dir != null; dir = dir.Parent)
                    path = Path.Combine(dir.Attribute("name").Value, path);
                return Path.DirectorySeparatorChar + path;
            }

            public override IEnumerable<XElement> List(string path, string searchPattern)
            {
                if (searchPattern != "*") throw new NotSupportedException();
                var dir = FindDirectory(path);
                if (dir == null) throw new Exception("Directory not found.");
                return dir.Elements();
            }

            public override TResult FindDirectory<TResult>(string path, TResult missing, Func<XElement, TResult> resultor)
            {
                var segments = new Queue<string>(path.Replace("//", "/").TrimEnd('/').Split('/'));
                var dir = _root;
                var segment = segments.Dequeue();
                if (segment.Length > 0)
                    return missing;
                while (dir != null && segments.Count > 0)
                {
                    segment = segments.Dequeue();
                    dir = dir.Elements("dir").SingleOrDefault(e => IsNamed(e, segment));
                }
                return Result(dir, missing, resultor);
            }

            public override TResult FindFile<TResult>(string path, TResult missing, Func<XElement, TResult> resultor)
            {
                var segments = path.Replace("//", "/").Split('/');
                if (segments.Length < 2)
                    return missing;
                var dir = FindDirectory(string.Join("/", segments.Take(segments.Length - 1)));
                if (dir == null)
                    return missing;
                var filename = segments.Last();
                var file = dir.Elements("file").SingleOrDefault(e => IsNamed(e, filename));
                return Result(file, missing, resultor);
            }

            static bool IsNamed(XElement e, string name)
            {
                return e.Attribute("name").Value.Equals(name, StringComparison.Ordinal);
            }

            static TResult Result<TResult>(XElement dir, TResult nil, Func<XElement, TResult> resultor)
            {
                return dir != null ? resultor(dir) : nil;
            }
        }
    }
}
