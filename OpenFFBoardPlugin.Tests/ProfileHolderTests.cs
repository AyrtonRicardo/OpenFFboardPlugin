using System.IO;
using NUnit.Framework;
using OpenFFBoardPlugin.DTO;

namespace OpenFFBoardPlugin.Tests
{
    [TestFixture]
    public class ProfileHolderTests
    {
        private static string TestDataPath(string filename) =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", filename);

        // ── File loading ─────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_FileNotFound_ReturnsNull()
        {
            var result = ProfileHolder.LoadFromJson(@"C:\does\not\exist\profiles.json");

            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromJson_ValidFile_ReturnsNonNull()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            Assert.IsNotNull(result);
        }

        // ── Profile count ────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_TwoProfilesFile_LoadsCorrectCount()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            Assert.AreEqual(2, result.Profiles.Count);
        }

        [Test]
        public void LoadFromJson_EmptyProfilesList_ReturnsHolderWithEmptyList()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("empty_profiles_list.json"));

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Profiles);
            Assert.AreEqual(0, result.Profiles.Count);
        }

        [Test]
        public void LoadFromJson_ProfileWithEmptyData_LoadsWithEmptyDataList()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("profile_with_empty_data.json"));

            Assert.AreEqual(1, result.Profiles.Count);
            Assert.IsNotNull(result.Profiles[0].Data);
            Assert.AreEqual(0, result.Profiles[0].Data.Count);
        }

        // ── Metadata ─────────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_Release_LoadedCorrectly()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            Assert.AreEqual(2, result.Release);
        }

        [Test]
        public void LoadFromJson_GlobalSettings_LoadedCorrectly()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            Assert.IsNotNull(result.Global);
            Assert.AreEqual(false, result.Global.DonotnotifyUpdates);
            Assert.AreEqual("en", result.Global.Language);
        }

        // ── Profile names ────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_ProfileNames_LoadedCorrectly()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            Assert.AreEqual("iRacing", result.Profiles[0].Name);
            Assert.AreEqual("Assetto Corsa", result.Profiles[1].Name);
        }

        // ── Profile data items ───────────────────────────────────────────────

        [Test]
        public void LoadFromJson_FirstProfileData_CorrectCount()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            Assert.AreEqual(4, result.Profiles[0].Data.Count);
        }

        [Test]
        public void LoadFromJson_ProfileData_FieldsLoadedCorrectly()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            var first = result.Profiles[0].Data[0];
            Assert.AreEqual("FX.spring",  first.Fullname);
            Assert.AreEqual("fx",         first.Cls);
            Assert.AreEqual(1,            first.Instance);
            Assert.AreEqual("spring",     first.Cmd);
            Assert.AreEqual(80,           first.Value);
        }

        [Test]
        public void LoadFromJson_AllFxCommands_LoadedCorrectly()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("single_profile_all_fx_commands.json"));
            var data = result.Profiles[0].Data;

            Assert.AreEqual(6, data.Count);
            Assert.AreEqual("filterCfFreq", data[0].Cmd);
            Assert.AreEqual("filterCfQ",    data[1].Cmd);
            Assert.AreEqual("spring",       data[2].Cmd);
            Assert.AreEqual("friction",     data[3].Cmd);
            Assert.AreEqual("damper",       data[4].Cmd);
            Assert.AreEqual("inertia",      data[5].Cmd);
        }

        [Test]
        public void LoadFromJson_AllAxisCommands_LoadedCorrectly()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("single_profile_all_axis_commands.json"));
            var data = result.Profiles[0].Data;

            Assert.AreEqual(8, data.Count);
            Assert.AreEqual("power",        data[0].Cmd);
            Assert.AreEqual("degrees",      data[1].Cmd);
            Assert.AreEqual("fxratio",      data[2].Cmd);
            Assert.AreEqual("esgain",       data[3].Cmd);
            Assert.AreEqual("idlespring",   data[4].Cmd);
            Assert.AreEqual("axisdamper",   data[5].Cmd);
            Assert.AreEqual("axisfriction", data[6].Cmd);
            Assert.AreEqual("axisinertia",  data[7].Cmd);
        }

        [Test]
        public void LoadFromJson_ProfileDataValues_LoadedAsInts()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("single_profile_all_axis_commands.json"));
            var data = result.Profiles[0].Data;

            Assert.AreEqual(100,  data[0].Value); // power
            Assert.AreEqual(900,  data[1].Value); // degrees
            Assert.AreEqual(80,   data[2].Value); // fxratio
        }

        // ── Profile list iteration ────────────────────────────────────────────

        [Test]
        public void LoadFromJson_CanFindProfileByName_CaseSensitive()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            var found = result.Profiles.Find(p =>
                p.Name.Equals("iRacing", System.StringComparison.InvariantCultureIgnoreCase));

            Assert.IsNotNull(found);
        }

        [Test]
        public void LoadFromJson_FindProfileByName_CaseInsensitiveMatchWorks()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            var found = result.Profiles.Find(p =>
                p.Name.Equals("IRACING", System.StringComparison.InvariantCultureIgnoreCase));

            Assert.IsNotNull(found, "Case-insensitive search should find 'iRacing'");
        }

        [Test]
        public void LoadFromJson_FindProfileByName_UnknownGameReturnsNull()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            var found = result.Profiles.Find(p =>
                p.Name.Equals("UnknownGame", System.StringComparison.InvariantCultureIgnoreCase));

            Assert.IsNull(found);
        }

        [Test]
        public void LoadFromJson_OrderPreserved_ProfilesInFileOrder()
        {
            var result = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));

            // First profile in file is iRacing, second is Assetto Corsa
            Assert.AreEqual("iRacing",       result.Profiles[0].Name);
            Assert.AreEqual("Assetto Corsa", result.Profiles[1].Name);
        }
    }
}
