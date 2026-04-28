using vatsys;

namespace DTIWindow.Integration
{
    public static class ErrorReporter
    {
        public static void ThrowError(string source, string message)
        {
            Errors.Add(new Exception(message) { Source = source });
        }
    }
}
