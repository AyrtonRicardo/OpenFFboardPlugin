using Newtonsoft.Json;
using NUnit.Framework;
using OpenFFBoardPlugin.Utils;
using System.Collections.Generic;

namespace OpenFFBoardPlugin.Tests
{
    /// <summary>
    /// Covers FlexibleListConverter&lt;T&gt; — the converter that tolerates {} in
    /// place of [] so hand-edited profiles.json files don't crash the plugin.
    /// </summary>
    [TestFixture]
    public class FlexibleListConverterTests
    {
        // Deserialize JSON into a wrapper that applies FlexibleListConverter.
        private static Wrapper Deserialize(string json)
            => JsonConvert.DeserializeObject<Wrapper>(json);

        private static string Serialize(Wrapper w)
            => JsonConvert.SerializeObject(w);

        // ── Reading: array forms ──────────────────────────────────────────────

        [Test]
        public void ReadJson_PopulatedArray_DeserializesAllItems()
        {
            var w = Deserialize(@"{ ""items"": [""a"", ""b"", ""c""] }");
            Assert.AreEqual(3, w.Items.Count);
            Assert.AreEqual("a", w.Items[0]);
            Assert.AreEqual("c", w.Items[2]);
        }

        [Test]
        public void ReadJson_EmptyArray_ReturnsEmptyList()
        {
            var w = Deserialize(@"{ ""items"": [] }");
            Assert.IsNotNull(w.Items);
            Assert.AreEqual(0, w.Items.Count);
        }

        // ── Reading: object / null forms (the bug scenarios) ─────────────────

        [Test]
        public void ReadJson_EmptyObject_ReturnsEmptyList()
        {
            // This was the bug: {} instead of [] caused a JsonSerializationException.
            var w = Deserialize(@"{ ""items"": {} }");
            Assert.IsNotNull(w.Items);
            Assert.AreEqual(0, w.Items.Count);
        }

        [Test]
        public void ReadJson_NullValue_ReturnsEmptyList()
        {
            var w = Deserialize(@"{ ""items"": null }");
            Assert.IsNotNull(w.Items);
            Assert.AreEqual(0, w.Items.Count);
        }

        [Test]
        public void ReadJson_MissingField_ReturnsNullProperty()
        {
            // Field absent entirely — converter is never called; property stays null.
            var w = Deserialize(@"{}");
            Assert.IsNull(w.Items);
        }

        [Test]
        public void ReadJson_NonEmptyObject_ReturnsEmptyList()
        {
            // A non-array object with content still yields an empty list (not an error).
            var w = Deserialize(@"{ ""items"": { ""foo"": 1 } }");
            Assert.IsNotNull(w.Items);
            Assert.AreEqual(0, w.Items.Count);
        }

        // ── Writing ───────────────────────────────────────────────────────────

        [Test]
        public void WriteJson_EmptyList_SerializesAsArray()
        {
            var w = new Wrapper { Items = new List<string>() };
            var json = Serialize(w);
            StringAssert.Contains("[]", json);
        }

        [Test]
        public void WriteJson_PopulatedList_SerializesAsArray()
        {
            var w = new Wrapper { Items = new List<string> { "x", "y" } };
            var json = Serialize(w);
            StringAssert.Contains("[", json);
            StringAssert.Contains("\"x\"", json);
            StringAssert.Contains("\"y\"", json);
        }

        // ── Round-trip ────────────────────────────────────────────────────────

        [Test]
        public void RoundTrip_EmptyObject_CanBeSavedBackAsArray()
        {
            // Simulate: load {} → save → reload → still an empty list.
            var loaded = Deserialize(@"{ ""items"": {} }");
            var resaved = Serialize(loaded);
            var reloaded = Deserialize(resaved);
            Assert.IsNotNull(reloaded.Items);
            Assert.AreEqual(0, reloaded.Items.Count);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private class Wrapper
        {
            [JsonConverter(typeof(FlexibleListConverter<string>))]
            public List<string> Items { get; set; }
        }
    }
}
