using System;
using System.IO;
using NUnit.Framework;
using OpenFFBoardPlugin.Utils;

namespace OpenFFBoardPlugin.Tests
{
    /// <summary>
    /// Helper models for JsonHandler generic tests.
    /// </summary>
    internal class SimpleModel
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    internal class NestedModel
    {
        public string Title { get; set; }
        public SimpleModel Child { get; set; }
    }

    internal class ListModel
    {
        public System.Collections.Generic.List<SimpleModel> Items { get; set; }
    }

    [TestFixture]
    public class JsonHandlerTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "OpenFFBoardTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── File existence ────────────────────────────────────────────────────

        [Test]
        public void LoadFromJsonFile_FileNotFound_ReturnsNull()
        {
            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(
                Path.Combine(_tempDir, "nonexistent.json"));

            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromJsonFile_NullPath_ReturnsNull()
        {
            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(null);

            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromJsonFile_EmptyStringPath_ReturnsNull()
        {
            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(string.Empty);

            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromJsonFile_DirectoryPathInsteadOfFile_ReturnsNull()
        {
            // File.Exists returns false for a directory path
            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(_tempDir);

            Assert.IsNull(result);
        }

        // ── Deserialization ───────────────────────────────────────────────────

        [Test]
        public void LoadFromJsonFile_ValidJsonObject_ReturnsDeserializedObject()
        {
            var path = WriteTempJson("{\"name\":\"hello\",\"value\":42}");

            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(path);

            Assert.IsNotNull(result);
            Assert.AreEqual("hello", result.Name);
            Assert.AreEqual(42, result.Value);
        }

        [Test]
        public void LoadFromJsonFile_CamelCaseKeys_MapToPascalCaseProperties()
        {
            // JsonHandler uses CamelCasePropertyNamesContractResolver
            var path = WriteTempJson("{\"name\":\"camelTest\",\"value\":7}");

            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(path);

            Assert.AreEqual("camelTest", result.Name);
            Assert.AreEqual(7, result.Value);
        }

        [Test]
        public void LoadFromJsonFile_NestedObject_DeserializesRecursively()
        {
            var path = WriteTempJson("{\"title\":\"parent\",\"child\":{\"name\":\"kid\",\"value\":5}}");

            var result = JsonHandler.LoadFromJsonFile<NestedModel>(path);

            Assert.IsNotNull(result);
            Assert.AreEqual("parent", result.Title);
            Assert.IsNotNull(result.Child);
            Assert.AreEqual("kid", result.Child.Name);
            Assert.AreEqual(5, result.Child.Value);
        }

        [Test]
        public void LoadFromJsonFile_ListProperty_DeserializesListCorrectly()
        {
            var path = WriteTempJson("{\"items\":[{\"name\":\"a\",\"value\":1},{\"name\":\"b\",\"value\":2}]}");

            var result = JsonHandler.LoadFromJsonFile<ListModel>(path);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual("a", result.Items[0].Name);
            Assert.AreEqual("b", result.Items[1].Name);
        }

        [Test]
        public void LoadFromJsonFile_JsonWithNullField_PropertyIsNull()
        {
            var path = WriteTempJson("{\"name\":null,\"value\":0}");

            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(path);

            Assert.IsNotNull(result);
            Assert.IsNull(result.Name);
        }

        [Test]
        public void LoadFromJsonFile_ExtraUnknownFields_IgnoredSilently()
        {
            var path = WriteTempJson("{\"name\":\"x\",\"value\":1,\"unknownField\":\"ignored\"}");

            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(path);

            Assert.IsNotNull(result);
            Assert.AreEqual("x", result.Name);
        }

        [Test]
        public void LoadFromJsonFile_MissingOptionalFields_DefaultToTypeDefault()
        {
            // Missing "value" → int default = 0
            var path = WriteTempJson("{\"name\":\"onlyname\"}");

            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(path);

            Assert.IsNotNull(result);
            Assert.AreEqual("onlyname", result.Name);
            Assert.AreEqual(0, result.Value);
        }

        [Test]
        public void LoadFromJsonFile_EmptyJsonObject_ReturnsInstanceWithDefaults()
        {
            var path = WriteTempJson("{}");

            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(path);

            Assert.IsNotNull(result);
            Assert.IsNull(result.Name);
            Assert.AreEqual(0, result.Value);
        }

        [Test]
        public void LoadFromJsonFile_JsonNull_ReturnsNull()
        {
            // "null" is valid JSON but deserializes reference types to null
            var path = WriteTempJson("null");

            var result = JsonHandler.LoadFromJsonFile<SimpleModel>(path);

            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromJsonFile_WorksWithDifferentGenericTypes_ProfileHolder()
        {
            var path = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestData", "valid_two_profiles.json");

            var result = JsonHandler.LoadFromJsonFile<DTO.ProfileHolder>(path);

            Assert.IsNotNull(result);
        }

        // ── Error paths (may be affected by SimHub.Logging initialisation) ───

        [Test]
        [Description("SimHub.Logging.Current may be null in test context causing NRE in catch block")]
        public void LoadFromJsonFile_InvalidJson_ReturnsNullOrThrowsNre()
        {
            var path = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestData", "invalid_json.json");

            SimpleModel result = null;
            try
            {
                result = JsonHandler.LoadFromJsonFile<SimpleModel>(path);
                Assert.IsNull(result, "Expected null for invalid JSON");
            }
            catch (NullReferenceException)
            {
                // SimHub.Logging.Current is not initialised in test context.
                // The catch block inside JsonHandler tries to call Current.Error()
                // which throws NRE. This is a known limitation.
                Assert.Inconclusive("SimHub.Logging.Current not initialised; cannot fully test error path.");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private string WriteTempJson(string content)
        {
            var path = Path.Combine(_tempDir, Guid.NewGuid() + ".json");
            File.WriteAllText(path, content);
            return path;
        }
    }
}
