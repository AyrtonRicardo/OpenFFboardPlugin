using NUnit.Framework;
using OpenFFBoardPlugin.DTO;
using OpenFFBoardPlugin.Utils;
using System.Collections.Generic;
using System.IO;

namespace OpenFFBoardPlugin.Tests
{
    [TestFixture]
    public class ProfileHolderTests
    {
        private string _tempDir;
        private string _profilePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _profilePath = Path.Combine(_tempDir, "profiles.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── LoadFromJson ──────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_FileNotExists_ReturnsNull()
        {
            var result = ProfileHolder.LoadFromJson(Path.Combine(_tempDir, "missing.json"));
            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromJson_ValidFile_LoadsMetadata()
        {
            File.WriteAllText(_profilePath, @"{
                ""release"": 2,
                ""global"": { ""donotnotifyUpdates"": true, ""language"": ""en"" },
                ""profiles"": []
            }");
            var holder = ProfileHolder.LoadFromJson(_profilePath);
            Assert.IsNotNull(holder);
            Assert.AreEqual(2, holder.Release);
            Assert.IsTrue(holder.Global.DonotnotifyUpdates);
        }

        [Test]
        public void LoadFromJson_ProfilesAsPopulatedArray_LoadsAll()
        {
            File.WriteAllText(_profilePath, @"{
                ""release"": 1,
                ""profiles"": [
                    { ""name"": ""default"", ""data"": [] },
                    { ""name"": ""Assetto Corsa"", ""data"": [] }
                ]
            }");
            var holder = ProfileHolder.LoadFromJson(_profilePath);
            Assert.AreEqual(2, holder.Profiles.Count);
            Assert.AreEqual("default", holder.Profiles[0].Name);
            Assert.AreEqual("Assetto Corsa", holder.Profiles[1].Name);
        }

        [Test]
        public void LoadFromJson_ProfilesAsEmptyArray_ReturnsEmptyList()
        {
            File.WriteAllText(_profilePath, @"{ ""release"": 1, ""profiles"": [] }");
            var holder = ProfileHolder.LoadFromJson(_profilePath);
            Assert.IsNotNull(holder.Profiles);
            Assert.AreEqual(0, holder.Profiles.Count);
        }

        [Test]
        public void LoadFromJson_ProfilesAsEmptyObject_ReturnsEmptyList()
        {
            // THE KEY BUG: {} instead of [] must not throw and must yield an empty list.
            File.WriteAllText(_profilePath, @"{ ""release"": 1, ""profiles"": {} }");
            Assert.DoesNotThrow(() =>
            {
                var holder = ProfileHolder.LoadFromJson(_profilePath);
                Assert.IsNotNull(holder);
                Assert.IsNotNull(holder.Profiles);
                Assert.AreEqual(0, holder.Profiles.Count);
            });
        }

        [Test]
        public void LoadFromJson_ProfileDataAsEmptyObject_ReturnsEmptyDataList()
        {
            // Same bug one level deeper: a profile's "data" field is {} instead of [].
            File.WriteAllText(_profilePath, @"{
                ""release"": 1,
                ""profiles"": [
                    { ""name"": ""default"", ""data"": {} }
                ]
            }");
            var holder = ProfileHolder.LoadFromJson(_profilePath);
            Assert.AreEqual(1, holder.Profiles.Count);
            Assert.IsNotNull(holder.Profiles[0].Data);
            Assert.AreEqual(0, holder.Profiles[0].Data.Count);
        }

        [Test]
        public void LoadFromJson_ProfileWithCommands_LoadsCommandData()
        {
            File.WriteAllText(_profilePath, @"{
                ""release"": 1,
                ""profiles"": [{
                    ""name"": ""default"",
                    ""data"": [
                        { ""fullname"": ""spring"", ""cls"": ""fx"", ""instance"": 0, ""cmd"": ""spring"", ""value"": 30 },
                        { ""fullname"": ""power"",  ""cls"": ""axis"", ""instance"": 0, ""cmd"": ""power"",  ""value"": 100 }
                    ]
                }]
            }");
            var holder = ProfileHolder.LoadFromJson(_profilePath);
            var data = holder.Profiles[0].Data;
            Assert.AreEqual(2, data.Count);
            Assert.AreEqual("fx", data[0].Cls);
            Assert.AreEqual("spring", data[0].Cmd);
            Assert.AreEqual(30, data[0].Value);
            Assert.AreEqual("axis", data[1].Cls);
            Assert.AreEqual(100, data[1].Value);
        }

        // ── SaveToJson ────────────────────────────────────────────────────────

        [Test]
        public void SaveToJson_WritesCamelCaseJson()
        {
            var holder = new ProfileHolder
            {
                Release = 1,
                Profiles = new List<Profile> { new Profile { Name = "default", Data = new List<ProfileData>() } }
            };
            holder.SaveToJson(_profilePath);
            var raw = File.ReadAllText(_profilePath);
            StringAssert.Contains("\"profiles\"", raw);
            StringAssert.Contains("\"name\"", raw);
            StringAssert.Contains("\"default\"", raw);
        }

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesAllData()
        {
            var original = new ProfileHolder
            {
                Release = 3,
                Profiles = new List<Profile>
                {
                    new Profile
                    {
                        Name = "default",
                        Data = new List<ProfileData>
                        {
                            new ProfileData { Fullname = "spring", Cls = "fx", Cmd = "spring", Value = 50, Instance = 0 }
                        }
                    }
                }
            };
            original.SaveToJson(_profilePath);
            var loaded = ProfileHolder.LoadFromJson(_profilePath);
            Assert.AreEqual(3, loaded.Release);
            Assert.AreEqual(1, loaded.Profiles.Count);
            Assert.AreEqual("default", loaded.Profiles[0].Name);
            Assert.AreEqual(1, loaded.Profiles[0].Data.Count);
            Assert.AreEqual("spring", loaded.Profiles[0].Data[0].Cmd);
            Assert.AreEqual(50, loaded.Profiles[0].Data[0].Value);
        }

        // ── GetOrCreateProfileForGame ─────────────────────────────────────────

        [Test]
        public void GetOrCreateProfileForGame_ExistingGame_ReturnsSameProfile()
        {
            var holder = HolderWithDefault();
            holder.Profiles.Add(new Profile { Name = "Assetto Corsa", Data = new List<ProfileData>() });
            var profile = holder.GetOrCreateProfileForGame("Assetto Corsa");
            Assert.AreEqual("Assetto Corsa", profile.Name);
            Assert.AreEqual(2, holder.Profiles.Count); // no new profile added
        }

        [Test]
        public void GetOrCreateProfileForGame_CaseInsensitiveMatch_ReturnsSame()
        {
            var holder = HolderWithDefault();
            holder.Profiles.Add(new Profile { Name = "Assetto Corsa", Data = new List<ProfileData>() });
            var profile = holder.GetOrCreateProfileForGame("assetto corsa");
            Assert.AreEqual("Assetto Corsa", profile.Name);
            Assert.AreEqual(2, holder.Profiles.Count);
        }

        [Test]
        public void GetOrCreateProfileForGame_NewGame_DefaultExists_ClonesDefaultData()
        {
            var holder = HolderWithDefault(springValue: 40);
            var profile = holder.GetOrCreateProfileForGame("iRacing");
            Assert.AreEqual("iRacing", profile.Name);
            Assert.AreEqual(1, profile.Data.Count);
            Assert.AreEqual(40, profile.Data[0].Value);
        }

        [Test]
        public void GetOrCreateProfileForGame_NewGame_DefaultExists_IsDeepCopy()
        {
            var holder = HolderWithDefault(springValue: 40);
            var newProfile = holder.GetOrCreateProfileForGame("iRacing");
            // Mutating the clone must not affect the default
            newProfile.Data[0].Value = 99;
            Assert.AreEqual(40, holder.Profiles[0].Data[0].Value);
        }

        [Test]
        public void GetOrCreateProfileForGame_NewGame_DefaultExists_AddsToList()
        {
            var holder = HolderWithDefault();
            holder.GetOrCreateProfileForGame("iRacing");
            Assert.AreEqual(2, holder.Profiles.Count);
            Assert.IsTrue(holder.Profiles.Exists(p => p.Name == "iRacing"));
        }

        [Test]
        public void GetOrCreateProfileForGame_NewGame_NoDefault_ReturnsBlankProfile()
        {
            var holder = new ProfileHolder { Profiles = new List<Profile>() };
            var profile = holder.GetOrCreateProfileForGame("iRacing");
            Assert.AreEqual("iRacing", profile.Name);
            Assert.IsNotNull(profile.Data);
            Assert.AreEqual(0, profile.Data.Count);
        }

        [Test]
        public void GetOrCreateProfileForGame_NullProfiles_InitializesListAndCreates()
        {
            var holder = new ProfileHolder { Profiles = null };
            var profile = holder.GetOrCreateProfileForGame("iRacing");
            Assert.IsNotNull(holder.Profiles);
            Assert.AreEqual("iRacing", profile.Name);
        }

        [Test]
        public void GetOrCreateProfileForGame_CalledTwice_DoesNotDuplicate()
        {
            var holder = HolderWithDefault();
            holder.GetOrCreateProfileForGame("iRacing");
            holder.GetOrCreateProfileForGame("iRacing");
            Assert.AreEqual(2, holder.Profiles.Count); // default + iRacing only
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static ProfileHolder HolderWithDefault(int springValue = 30)
        {
            return new ProfileHolder
            {
                Release = 1,
                Profiles = new List<Profile>
                {
                    new Profile
                    {
                        Name = "default",
                        Data = new List<ProfileData>
                        {
                            new ProfileData { Fullname = "spring", Cls = "fx", Cmd = "spring", Value = springValue, Instance = 0 }
                        }
                    }
                }
            };
        }
    }
}
