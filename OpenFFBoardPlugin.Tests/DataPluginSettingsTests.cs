using NUnit.Framework;

namespace OpenFFBoardPlugin.Tests
{
    [TestFixture]
    public class DataPluginSettingsTests
    {
        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void DefaultAutoConnectOnStartup_IsFalse()
        {
            var settings = new DataPluginSettings();

            Assert.IsFalse(settings.AutoConnectOnStartup);
        }

        [Test]
        public void DefaultProfileJsonPath_IsNull()
        {
            var settings = new DataPluginSettings();

            Assert.IsNull(settings.ProfileJsonPath);
        }

        [Test]
        public void DefaultSelectedHidDeviceId_IsNull()
        {
            var settings = new DataPluginSettings();

            Assert.IsNull(settings.SelectedHidDeviceId);
        }

        // ── Mutation ──────────────────────────────────────────────────────────

        [Test]
        public void AutoConnectOnStartup_CanBeSetToTrue()
        {
            var settings = new DataPluginSettings();
            settings.AutoConnectOnStartup = true;

            Assert.IsTrue(settings.AutoConnectOnStartup);
        }

        [Test]
        public void AutoConnectOnStartup_CanBeToggledBackToFalse()
        {
            var settings = new DataPluginSettings();
            settings.AutoConnectOnStartup = true;
            settings.AutoConnectOnStartup = false;

            Assert.IsFalse(settings.AutoConnectOnStartup);
        }

        [Test]
        public void ProfileJsonPath_CanBeSet()
        {
            var settings = new DataPluginSettings();
            settings.ProfileJsonPath = @"C:\profiles";

            Assert.AreEqual(@"C:\profiles", settings.ProfileJsonPath);
        }

        [Test]
        public void ProfileJsonPath_CanBeSetToNull()
        {
            var settings = new DataPluginSettings();
            settings.ProfileJsonPath = @"C:\profiles";
            settings.ProfileJsonPath = null;

            Assert.IsNull(settings.ProfileJsonPath);
        }

        [Test]
        public void ProfileJsonPath_CanBeSetToEmptyString()
        {
            var settings = new DataPluginSettings();
            settings.ProfileJsonPath = string.Empty;

            Assert.AreEqual(string.Empty, settings.ProfileJsonPath);
        }

        [Test]
        public void SelectedHidDeviceId_CanBeSet()
        {
            var settings = new DataPluginSettings();
            settings.SelectedHidDeviceId = "HID\\VID_1209&PID_FFB0";

            Assert.AreEqual("HID\\VID_1209&PID_FFB0", settings.SelectedHidDeviceId);
        }

        [Test]
        public void SelectedHidDeviceId_CanBeSetToNull()
        {
            var settings = new DataPluginSettings();
            settings.SelectedHidDeviceId = "some-id";
            settings.SelectedHidDeviceId = null;

            Assert.IsNull(settings.SelectedHidDeviceId);
        }

        // ── Independence ──────────────────────────────────────────────────────

        [Test]
        public void TwoInstances_DoNotShareState()
        {
            var a = new DataPluginSettings();
            var b = new DataPluginSettings();
            a.ProfileJsonPath = @"C:\a";

            Assert.IsNull(b.ProfileJsonPath);
        }
    }
}
