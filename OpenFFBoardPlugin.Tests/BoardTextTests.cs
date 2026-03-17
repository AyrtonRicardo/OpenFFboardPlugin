using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework;

namespace OpenFFBoardPlugin.Tests
{
    /// <summary>
    /// Tests for the BoardText helper class (defined in SettingsControl.xaml.cs).
    /// BoardText implements INotifyPropertyChanged so the WPF list can react to
    /// selection changes.
    /// </summary>
    [TestFixture]
    public class BoardTextTests
    {
        // ── Name property ─────────────────────────────────────────────────────

        [Test]
        public void Name_DefaultIsNull()
        {
            var bt = new BoardText();

            Assert.IsNull(bt.Name);
        }

        [Test]
        public void Name_CanBeSet()
        {
            var bt = new BoardText { Name = "HID\\VID_1209&PID_FFB0" };

            Assert.AreEqual("HID\\VID_1209&PID_FFB0", bt.Name);
        }

        [Test]
        public void Name_CanBeSetToNull()
        {
            var bt = new BoardText { Name = "something" };
            bt.Name = null;

            Assert.IsNull(bt.Name);
        }

        [Test]
        public void Name_CanBeSetToEmptyString()
        {
            var bt = new BoardText { Name = string.Empty };

            Assert.AreEqual(string.Empty, bt.Name);
        }

        // ── DeviceIndex property ──────────────────────────────────────────────

        [Test]
        public void DeviceIndex_DefaultIsZero()
        {
            var bt = new BoardText();

            Assert.AreEqual(0, bt.DeviceIndex);
        }

        [Test]
        public void DeviceIndex_CanBeSet()
        {
            var bt = new BoardText { DeviceIndex = 3 };

            Assert.AreEqual(3, bt.DeviceIndex);
        }

        [Test]
        public void DeviceIndex_CanBeNegative()
        {
            // No validation — any int is accepted
            var bt = new BoardText { DeviceIndex = -1 };

            Assert.AreEqual(-1, bt.DeviceIndex);
        }

        // ── IsEnabled property ────────────────────────────────────────────────

        [Test]
        public void IsEnabled_DefaultIsFalse()
        {
            var bt = new BoardText();

            Assert.IsFalse(bt.IsEnabled);
        }

        [Test]
        public void IsEnabled_CanBeSetToTrue()
        {
            var bt = new BoardText { IsEnabled = true };

            Assert.IsTrue(bt.IsEnabled);
        }

        [Test]
        public void IsEnabled_CanBeToggledBackToFalse()
        {
            var bt = new BoardText { IsEnabled = true };
            bt.IsEnabled = false;

            Assert.IsFalse(bt.IsEnabled);
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        [Test]
        public void IsEnabled_WhenSetToTrue_FiresPropertyChangedEvent()
        {
            var bt = new BoardText();
            var fired = new List<string>();
            bt.PropertyChanged += (s, e) => fired.Add(e.PropertyName);

            bt.IsEnabled = true;

            CollectionAssert.Contains(fired, "IsEnabled");
        }

        [Test]
        public void IsEnabled_WhenSetToFalse_FiresPropertyChangedEvent()
        {
            var bt = new BoardText { IsEnabled = true };
            var fired = new List<string>();
            bt.PropertyChanged += (s, e) => fired.Add(e.PropertyName);

            bt.IsEnabled = false;

            CollectionAssert.Contains(fired, "IsEnabled");
        }

        [Test]
        public void IsEnabled_EventArgs_ContainsCorrectPropertyName()
        {
            var bt = new BoardText();
            PropertyChangedEventArgs capturedArgs = null;
            bt.PropertyChanged += (s, e) => capturedArgs = e;

            bt.IsEnabled = true;

            Assert.IsNotNull(capturedArgs);
            Assert.AreEqual("IsEnabled", capturedArgs.PropertyName);
        }

        [Test]
        public void IsEnabled_SetSameValueTwice_EventFiredEachTime()
        {
            // BoardText fires on every set, not just on value change
            var bt = new BoardText();
            int count = 0;
            bt.PropertyChanged += (s, e) => count++;

            bt.IsEnabled = true;
            bt.IsEnabled = true;

            Assert.AreEqual(2, count);
        }

        [Test]
        public void PropertyChanged_NoSubscribers_DoesNotThrow()
        {
            var bt = new BoardText();

            Assert.DoesNotThrow(() => bt.IsEnabled = true);
        }

        [Test]
        public void PropertyChanged_MultipleSubscribers_AllNotified()
        {
            var bt = new BoardText();
            int count = 0;
            bt.PropertyChanged += (s, e) => count++;
            bt.PropertyChanged += (s, e) => count++;

            bt.IsEnabled = true;

            Assert.AreEqual(2, count);
        }

        // ── Object initialiser ────────────────────────────────────────────────

        [Test]
        public void ObjectInitialiser_SetsAllProperties()
        {
            var bt = new BoardText
            {
                Name = "device-0",
                DeviceIndex = 0,
                IsEnabled = true
            };

            Assert.AreEqual("device-0", bt.Name);
            Assert.AreEqual(0, bt.DeviceIndex);
            Assert.IsTrue(bt.IsEnabled);
        }

        [Test]
        public void TwoInstances_DoNotShareIsEnabledState()
        {
            var a = new BoardText();
            var b = new BoardText();
            a.IsEnabled = true;

            Assert.IsFalse(b.IsEnabled);
        }
    }
}
