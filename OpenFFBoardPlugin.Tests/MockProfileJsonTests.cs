using NUnit.Framework;
using OpenFFBoardPlugin.DTO;
using OpenFFBoardPlugin.Utils;
using System.Collections.Generic;
using System.IO;

namespace OpenFFBoardPlugin.Tests
{
    /// <summary>
    /// Tests that use the real profiles.json fixture from TestSupport/mock_json/.
    /// Covers valid loading, the "None" profile, and {} vs [] tolerance.
    /// </summary>
    [TestFixture]
    public class MockProfileJsonTests
    {
        private string _realProfilePath;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _realProfilePath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestSupport", "mock_json", "profiles.json");

            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── Loading the real fixture ──────────────────────────────────────────

        [Test]
        public void LoadFromJson_RealFile_DoesNotReturnNull()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            Assert.IsNotNull(holder);
        }

        [Test]
        public void LoadFromJson_RealFile_HasCorrectRelease()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            Assert.AreEqual(2, holder.Release);
        }

        [Test]
        public void LoadFromJson_RealFile_LoadsThreeProfiles()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            Assert.AreEqual(3, holder.Profiles.Count);
        }

        [Test]
        public void LoadFromJson_RealFile_ProfileNamesAreCorrect()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var names = holder.Profiles.ConvertAll(p => p.Name);
            CollectionAssert.Contains(names, "None");
            CollectionAssert.Contains(names, "Flash profile");
            CollectionAssert.Contains(names, "Default");
        }

        // ── "None" profile ────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_RealFile_NoneProfileExists()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var none = holder.Profiles.Find(p => p.Name == "None");
            Assert.IsNotNull(none);
        }

        [Test]
        public void LoadFromJson_RealFile_NoneProfileHasEmptyData()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var none = holder.Profiles.Find(p => p.Name == "None");
            Assert.IsNotNull(none.Data);
            Assert.AreEqual(0, none.Data.Count);
        }

        [Test]
        public void GetOrCreateProfileForGame_FindsNoneProfileCaseInsensitive()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            // "none" (lowercase) must match "None" from the file
            var profile = holder.GetOrCreateProfileForGame("none");
            Assert.AreEqual("None", profile.Name);
            Assert.AreEqual(3, holder.Profiles.Count); // no new profile created
        }

        [Test]
        public void GetOrCreateProfileForGame_NoneProfile_HasEmptyCommandList()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var profile = holder.GetOrCreateProfileForGame("None");
            Assert.AreEqual(0, profile.Data.Count);
        }

        // ── "Default" and "Flash profile" data ───────────────────────────────

        [Test]
        public void LoadFromJson_RealFile_DefaultProfileHasTwelveCommands()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var def = holder.Profiles.Find(p => p.Name == "Default");
            Assert.AreEqual(12, def.Data.Count);
        }

        [Test]
        public void LoadFromJson_RealFile_FlashProfileHasAllCommands()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var flash = holder.Profiles.Find(p => p.Name == "Flash profile");
            Assert.AreEqual(15, flash.Data.Count);
        }

        [Test]
        public void LoadFromJson_RealFile_DefaultProfile_FxCommandsAreCorrect()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var def = holder.Profiles.Find(p => p.Name == "Default");
            var spring = def.Data.Find(d => d.Cmd == "spring");
            Assert.IsNotNull(spring);
            Assert.AreEqual("fx", spring.Cls);
            Assert.AreEqual(31, spring.Value);
        }

        [Test]
        public void LoadFromJson_RealFile_DefaultProfile_AxisCommandsAreCorrect()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var def = holder.Profiles.Find(p => p.Name == "Default");
            var degrees = def.Data.Find(d => d.Cmd == "degrees");
            Assert.IsNotNull(degrees);
            Assert.AreEqual("axis", degrees.Cls);
            Assert.AreEqual(1080, degrees.Value);
        }

        // ── New game clones Default ───────────────────────────────────────────

        [Test]
        public void GetOrCreateProfileForGame_UnknownGame_ClonesDefault()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var profile = holder.GetOrCreateProfileForGame("Assetto Corsa");
            Assert.AreEqual("Assetto Corsa", profile.Name);
            // Cloned from Default which has 12 entries
            Assert.AreEqual(12, profile.Data.Count);
        }

        [Test]
        public void GetOrCreateProfileForGame_UnknownGame_AddedToList()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            holder.GetOrCreateProfileForGame("Assetto Corsa");
            Assert.AreEqual(4, holder.Profiles.Count);
        }

        [Test]
        public void GetOrCreateProfileForGame_UnknownGame_CloneIsIndependent()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var newProfile = holder.GetOrCreateProfileForGame("iRacing");
            newProfile.Data[0].Value = 9999;
            var def = holder.Profiles.Find(p => p.Name == "Default");
            Assert.AreNotEqual(9999, def.Data[0].Value);
        }

        // ── {} tolerance using temp-file variants of the real fixture ─────────

        [Test]
        public void LoadFromJson_ProfilesAsEmptyObject_DoesNotThrow()
        {
            var path = WriteTempJson(@"{
                ""release"": 2,
                ""global"": { ""donotnotifyUpdates"": false, ""language"": ""en_US"" },
                ""profiles"": {}
            }");
            Assert.DoesNotThrow(() =>
            {
                var holder = ProfileHolder.LoadFromJson(path);
                Assert.IsNotNull(holder);
                Assert.IsNotNull(holder.Profiles);
                Assert.AreEqual(0, holder.Profiles.Count);
            });
        }

        [Test]
        public void LoadFromJson_NoneProfileDataAsEmptyObject_DoesNotThrow()
        {
            // "None" profile with data: {} instead of data: []
            var path = WriteTempJson(@"{
                ""release"": 2,
                ""profiles"": [
                    { ""name"": ""None"", ""data"": {} },
                    { ""name"": ""Default"", ""data"": [] }
                ]
            }");
            Assert.DoesNotThrow(() =>
            {
                var holder = ProfileHolder.LoadFromJson(path);
                var none = holder.Profiles.Find(p => p.Name == "None");
                Assert.IsNotNull(none);
                Assert.AreEqual(0, none.Data.Count);
            });
        }

        [Test]
        public void LoadFromJson_MixedArrayAndObjectData_LoadsValidProfilesOnly()
        {
            // One profile has {} data, another has real data — both must load.
            var path = WriteTempJson(@"{
                ""release"": 2,
                ""profiles"": [
                    { ""name"": ""None"", ""data"": {} },
                    {
                        ""name"": ""Default"",
                        ""data"": [
                            { ""fullname"": ""Effects"", ""cls"": ""fx"", ""instance"": 0, ""cmd"": ""spring"", ""value"": 31 }
                        ]
                    }
                ]
            }");
            var holder = ProfileHolder.LoadFromJson(path);
            Assert.AreEqual(2, holder.Profiles.Count);
            Assert.AreEqual(0, holder.Profiles.Find(p => p.Name == "None").Data.Count);
            Assert.AreEqual(1, holder.Profiles.Find(p => p.Name == "Default").Data.Count);
        }

        [Test]
        public void LoadFromJson_ProfilesAsEmptyObject_GetOrCreateClonesNothing()
        {
            // With {} profiles, there is no default to clone from — must still work.
            var path = WriteTempJson(@"{ ""release"": 2, ""profiles"": {} }");
            var holder = ProfileHolder.LoadFromJson(path);
            var profile = holder.GetOrCreateProfileForGame("iRacing");
            Assert.AreEqual("iRacing", profile.Name);
            Assert.IsNotNull(profile.Data);
            Assert.AreEqual(0, profile.Data.Count);
        }

        // ── Round-trip: save real fixture then reload ─────────────────────────

        [Test]
        public void SaveAndLoad_RealFixture_PreservesAllProfiles()
        {
            var holder = ProfileHolder.LoadFromJson(_realProfilePath);
            var savePath = Path.Combine(_tempDir, "profiles.json");
            holder.SaveToJson(savePath);
            var reloaded = ProfileHolder.LoadFromJson(savePath);
            Assert.AreEqual(holder.Release, reloaded.Release);
            Assert.AreEqual(holder.Profiles.Count, reloaded.Profiles.Count);
            for (int i = 0; i < holder.Profiles.Count; i++)
            {
                Assert.AreEqual(holder.Profiles[i].Name, reloaded.Profiles[i].Name);
                Assert.AreEqual(holder.Profiles[i].Data.Count, reloaded.Profiles[i].Data.Count);
            }
        }

        [Test]
        public void SaveAndLoad_EmptyObjectVariant_SavesAsArray()
        {
            var path = WriteTempJson(@"{ ""release"": 1, ""profiles"": {} }");
            var holder = ProfileHolder.LoadFromJson(path);
            var savePath = Path.Combine(_tempDir, "resaved.json");
            holder.SaveToJson(savePath);
            var raw = File.ReadAllText(savePath);
            // Must be written back as [] not {}
            StringAssert.Contains("\"profiles\": []", raw);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private string WriteTempJson(string content)
        {
            var path = Path.Combine(_tempDir, Path.GetRandomFileName() + ".json");
            File.WriteAllText(path, content);
            return path;
        }
    }
}
