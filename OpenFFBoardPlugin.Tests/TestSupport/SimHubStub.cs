// Minimal stub so JsonHandler.cs compiles without the real SimHub.Logging.dll.
// The stub matches the call shape: SimHub.Logging.Current.Error/Warn/Info(object).
// All methods are no-ops — tests verify behaviour through return values and
// file contents, not through logging side-effects.
namespace SimHub
{
    public class Logging
    {
        public static readonly TestLog Current = new TestLog();

        public class TestLog
        {
            public void Error(object message) { }
            public void Warn(object message) { }
            public void Info(object message) { }
            public void Debug(object message) { }
        }
    }
}
