using Newtonsoft.Json;
using NUnit.Framework;
using OpenFFBoardPlugin.Utils;
using System.Collections.Generic;
using System.IO;

namespace OpenFFBoardPlugin.Tests
{
    [TestFixture]
    public class JsonHandlerTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        private string TempFile(string name = "test.json") => Path.Combine(_tempDir, name);

        // ── LoadFromJsonFile ──────────────────────────────────────────────────

        [Test]
        public void LoadFromJsonFile_FileDoesNotExist_ReturnsNull()
        {
            var result = JsonHandler.LoadFromJsonFile<TestModel>(TempFile("missing.json"));
            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromJsonFile_ValidCamelCaseJson_Deserializes()
        {
            File.WriteAllText(TempFile(), @"{ ""name"": ""hello"", ""count"": 42 }");
            var result = JsonHandler.LoadFromJsonFile<TestModel>(TempFile());
            Assert.IsNotNull(result);
            Assert.AreEqual("hello", result.Name);
            Assert.AreEqual(42, result.Count);
        }

        [Test]
        public void LoadFromJsonFile_InvalidJson_ReturnsNull()
        {
            File.WriteAllText(TempFile(), "this is not json {{{{");
            var result = JsonHandler.LoadFromJsonFile<TestModel>(TempFile());
            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromJsonFile_EmptyFile_ReturnsNull()
        {
            File.WriteAllText(TempFile(), "");
            var result = JsonHandler.LoadFromJsonFile<TestModel>(TempFile());
            Assert.IsNull(result);
        }

        // ── SaveToJsonFile ────────────────────────────────────────────────────

        [Test]
        public void SaveToJsonFile_WritesCamelCaseJson()
        {
            var model = new TestModel { Name = "board", Count = 7 };
            JsonHandler.SaveToJsonFile(TempFile(), model);
            var raw = File.ReadAllText(TempFile());
            // camelCase — not PascalCase
            StringAssert.Contains("\"name\"", raw);
            StringAssert.Contains("\"count\"", raw);
            StringAssert.DoesNotContain("\"Name\"", raw);
        }

        [Test]
        public void SaveToJsonFile_WritesIndentedJson()
        {
            var model = new TestModel { Name = "x", Count = 1 };
            JsonHandler.SaveToJsonFile(TempFile(), model);
            var raw = File.ReadAllText(TempFile());
            // Indented output has newlines
            StringAssert.Contains("\n", raw);
        }

        // ── Clone ─────────────────────────────────────────────────────────────

        [Test]
        public void Clone_ProducesEqualButDistinctObject()
        {
            var original = new TestModel { Name = "orig", Count = 1, Tags = new List<string> { "a", "b" } };
            var clone = JsonHandler.Clone(original);
            Assert.AreEqual(original.Name, clone.Name);
            Assert.AreEqual(original.Count, clone.Count);
            Assert.AreEqual(original.Tags, clone.Tags);
            Assert.AreNotSame(original, clone);
        }

        [Test]
        public void Clone_IsDeepCopy_MutatingCloneDoesNotAffectOriginal()
        {
            var original = new TestModel { Tags = new List<string> { "a" } };
            var clone = JsonHandler.Clone(original);
            clone.Tags.Add("b");
            clone.Name = "modified";
            Assert.AreEqual(1, original.Tags.Count);
            Assert.IsNull(original.Name);
        }

        // ── Round-trip ────────────────────────────────────────────────────────

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesData()
        {
            var original = new TestModel { Name = "roundtrip", Count = 99, Tags = new List<string> { "x", "y" } };
            JsonHandler.SaveToJsonFile(TempFile(), original);
            var loaded = JsonHandler.LoadFromJsonFile<TestModel>(TempFile());
            Assert.AreEqual(original.Name, loaded.Name);
            Assert.AreEqual(original.Count, loaded.Count);
            CollectionAssert.AreEqual(original.Tags, loaded.Tags);
        }

        // ── Helper model ──────────────────────────────────────────────────────

        private class TestModel
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public List<string> Tags { get; set; }
        }
    }
}
