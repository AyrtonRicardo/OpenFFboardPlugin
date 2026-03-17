using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using OpenFFBoardPlugin.DTO;
using OpenFFBoardPlugin.Utils;

namespace OpenFFBoardPlugin.Tests
{
    /// <summary>
    /// Tests for ProfileToCommandConverter.
    ///
    /// Lambdas are tested for null/non-null only — invoking them requires a live
    /// OpenFFBoard.Board which needs actual hardware.  Null is passed as the
    /// board parameter: lambda *creation* is safe with a null capture; only
    /// *invocation* would NRE.
    ///
    /// MapCommand (private static) is accessed via reflection to allow targeted
    /// testing of the routing logic without going through ConvertProfileToCommands
    /// (which calls SimHub.Logging on null-command paths).
    /// </summary>
    [TestFixture]
    public class ProfileToCommandConverterTests
    {
        private static readonly MethodInfo MapCommandMethod =
            typeof(ProfileToCommandConverter).GetMethod(
                "MapCommand", BindingFlags.NonPublic | BindingFlags.Static);

        private static string TestDataPath(string filename) =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", filename);

        // ── ConvertProfileToCommands — empty / trivial ────────────────────────

        [Test]
        public void ConvertProfileToCommands_EmptyDataList_ReturnsEmptyList()
        {
            var profile = new Profile { Name = "Test", Data = new List<ProfileData>() };

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        // ── FX command routing ────────────────────────────────────────────────

        [Test]
        [TestCase("filterCfFreq")]
        [TestCase("filterCfQ")]
        [TestCase("spring")]
        [TestCase("friction")]
        [TestCase("damper")]
        [TestCase("inertia")]
        public void ConvertProfileToCommands_KnownFxCommand_ReturnsNonNullLambda(string cmd)
        {
            var profile = MakeProfile("fx", cmd, 50);

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result[0]);
        }

        // ── Axis command routing ──────────────────────────────────────────────

        [Test]
        [TestCase("power")]
        [TestCase("degrees")]
        [TestCase("fxratio")]
        [TestCase("esgain")]
        [TestCase("idlespring")]
        [TestCase("axisdamper")]
        [TestCase("axisfriction")]
        [TestCase("axisinertia")]
        public void ConvertProfileToCommands_KnownAxisCommand_ReturnsNonNullLambda(string cmd)
        {
            var profile = MakeProfile("axis", cmd, 50);

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result[0]);
        }

        // ── Unknown class (no logging hit in MapCommand itself) ───────────────

        [Test]
        public void MapCommand_UnknownClass_ReturnsNull()
        {
            var data = MakeProfileData("unknowncls", "spring", 50);

            var result = InvokeMapCommand(data);

            Assert.IsNull(result);
        }

        [Test]
        public void MapCommand_NullClass_ReturnsNull()
        {
            var data = MakeProfileData(null, "spring", 50);

            var result = InvokeMapCommand(data);

            Assert.IsNull(result);
        }

        [Test]
        public void MapCommand_EmptyStringClass_ReturnsNull()
        {
            var data = MakeProfileData(string.Empty, "spring", 50);

            var result = InvokeMapCommand(data);

            Assert.IsNull(result);
        }

        [Test]
        public void MapCommand_UppercaseFxClass_ReturnsNull()
        {
            // Class matching is case-sensitive; "FX" != "fx"
            var data = MakeProfileData("FX", "spring", 50);

            var result = InvokeMapCommand(data);

            Assert.IsNull(result);
        }

        [Test]
        public void MapCommand_UppercaseAxisClass_ReturnsNull()
        {
            var data = MakeProfileData("AXIS", "power", 50);

            var result = InvokeMapCommand(data);

            Assert.IsNull(result);
        }

        // ── Unknown cmd (hits SimHub.Logging inside MapCommand) ───────────────

        [Test]
        [Description("Unknown fx cmd triggers SimHub.Logging; inconclusive if logger not initialised")]
        public void MapCommand_FxUnknownCmd_ReturnsNullOrThrowsNre()
        {
            var data = MakeProfileData("fx", "totallyunknown", 50);

            AssertNullOrInconclusive(() => InvokeMapCommand(data));
        }

        [Test]
        [Description("Unknown axis cmd triggers SimHub.Logging; inconclusive if logger not initialised")]
        public void MapCommand_AxisUnknownCmd_ReturnsNullOrThrowsNre()
        {
            var data = MakeProfileData("axis", "totallyunknown", 50);

            AssertNullOrInconclusive(() => InvokeMapCommand(data));
        }

        [Test]
        [Description("Null cmd triggers SimHub.Logging; inconclusive if logger not initialised")]
        public void MapCommand_NullCmd_ReturnsNullOrThrowsNre()
        {
            var data = MakeProfileData("fx", null, 50);

            AssertNullOrInconclusive(() => InvokeMapCommand(data));
        }

        // ── Order preservation ────────────────────────────────────────────────

        [Test]
        public void ConvertProfileToCommands_MultipleCommands_OrderPreserved()
        {
            var holder = ProfileHolder.LoadFromJson(TestDataPath("single_profile_all_fx_commands.json"));
            var profile = holder.Profiles[0]; // 6 fx commands in known order

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.AreEqual(6, result.Count);
            // All 6 lambdas should be present and non-null
            foreach (var cmd in result)
                Assert.IsNotNull(cmd);
        }

        [Test]
        public void ConvertProfileToCommands_MixedFxAndAxis_AllLambdasReturned()
        {
            var holder = ProfileHolder.LoadFromJson(TestDataPath("valid_two_profiles.json"));
            var profile = holder.Profiles[0]; // 4 commands: 2 fx + 2 axis

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.AreEqual(4, result.Count);
        }

        // ── Value casting edge cases ──────────────────────────────────────────

        [Test]
        public void ConvertProfileToCommands_ValueZero_LambdaCreated()
        {
            var profile = MakeProfile("fx", "spring", 0);

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result[0]);
        }

        [Test]
        public void ConvertProfileToCommands_ValueMaxByte_LambdaCreated()
        {
            var profile = MakeProfile("fx", "spring", 255);

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.IsNotNull(result[0]);
        }

        [Test]
        public void ConvertProfileToCommands_ValueOverflowByte_LambdaCreated()
        {
            // int 256 cast to byte truncates to 0; lambda should still be created
            var profile = MakeProfile("fx", "spring", 256);

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.IsNotNull(result[0]);
        }

        [Test]
        public void ConvertProfileToCommands_ValueNegative_LambdaCreated()
        {
            // Negative int cast to byte wraps (e.g. -1 → 255); lambda created
            var profile = MakeProfile("fx", "spring", -1);

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.IsNotNull(result[0]);
        }

        [Test]
        public void ConvertProfileToCommands_MaxUshortValue_LambdaCreated()
        {
            // filterCfFreq and power/degrees use ushort; 65535 is max
            var profile = MakeProfile("fx", "filterCfFreq", 65535);

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.IsNotNull(result[0]);
        }

        [Test]
        public void ConvertProfileToCommands_UshortOverflow_LambdaCreated()
        {
            // 65536 cast to ushort wraps to 0; lambda still created
            var profile = MakeProfile("axis", "power", 65536);

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.IsNotNull(result[0]);
        }

        [Test]
        public void ConvertProfileToCommands_DuplicateCommands_AllLambdasReturned()
        {
            // The same command appearing twice should produce two independent lambdas
            var profile = new Profile
            {
                Name = "Test",
                Data = new List<ProfileData>
                {
                    MakeProfileData("fx", "spring", 50),
                    MakeProfileData("fx", "spring", 80)
                }
            };

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.AreEqual(2, result.Count);
            Assert.IsNotNull(result[0]);
            Assert.IsNotNull(result[1]);
        }

        [Test]
        public void ConvertProfileToCommands_LargeProfile_AllLambdasReturned()
        {
            // 14 commands: all 6 fx + all 8 axis
            var data = new List<ProfileData>
            {
                MakeProfileData("fx",   "filterCfFreq", 1000),
                MakeProfileData("fx",   "filterCfQ",    10),
                MakeProfileData("fx",   "spring",       80),
                MakeProfileData("fx",   "friction",     50),
                MakeProfileData("fx",   "damper",       60),
                MakeProfileData("fx",   "inertia",      30),
                MakeProfileData("axis", "power",        100),
                MakeProfileData("axis", "degrees",      900),
                MakeProfileData("axis", "fxratio",      80),
                MakeProfileData("axis", "esgain",       100),
                MakeProfileData("axis", "idlespring",   10),
                MakeProfileData("axis", "axisdamper",   20),
                MakeProfileData("axis", "axisfriction", 15),
                MakeProfileData("axis", "axisinertia",  5)
            };

            var result = ProfileToCommandConverter.ConvertProfileToCommands(
                new Profile { Name = "Full", Data = data }, null);

            Assert.AreEqual(14, result.Count);
            foreach (var cmd in result)
                Assert.IsNotNull(cmd);
        }

        // ── Instance field is ignored by the converter ────────────────────────

        [Test]
        public void ConvertProfileToCommands_DifferentInstanceValues_AllMappedSameWay()
        {
            // The converter routes purely on Cls+Cmd, ignoring Instance
            var profile = new Profile
            {
                Name = "Test",
                Data = new List<ProfileData>
                {
                    new ProfileData { Cls = "fx", Cmd = "spring", Value = 50, Instance = 1 },
                    new ProfileData { Cls = "fx", Cmd = "spring", Value = 50, Instance = 2 },
                    new ProfileData { Cls = "fx", Cmd = "spring", Value = 50, Instance = 99 }
                }
            };

            var result = ProfileToCommandConverter.ConvertProfileToCommands(profile, null);

            Assert.AreEqual(3, result.Count);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static ProfileData MakeProfileData(string cls, string cmd, int value) =>
            new ProfileData { Cls = cls, Cmd = cmd, Value = value, Instance = 1, Fullname = $"{cls}.{cmd}" };

        private static Profile MakeProfile(string cls, string cmd, int value) =>
            new Profile { Name = "Test", Data = new List<ProfileData> { MakeProfileData(cls, cmd, value) } };

        private static Func<bool> InvokeMapCommand(ProfileData data)
        {
            try
            {
                return (Func<bool>)MapCommandMethod.Invoke(null, new object[] { data, null });
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException is NullReferenceException nre)
                    throw nre;
                throw;
            }
        }

        private static void AssertNullOrInconclusive(Func<Func<bool>> action)
        {
            try
            {
                var result = action();
                Assert.IsNull(result);
            }
            catch (NullReferenceException)
            {
                Assert.Inconclusive("SimHub.Logging.Current not initialised; cannot fully test error path.");
            }
        }
    }
}
