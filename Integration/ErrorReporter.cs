using System.Reflection;
using vatsys;

namespace DTIWindow.Integration
{
    public static class ErrorReporter
    {
        public static void ThrowError(string source, string message)
        {
            try
            {
                Type? errorsType = typeof(MMI).Assembly.GetType("vatsys.Errors");
                if (errorsType == null)
                    return;

                MethodInfo? addMethod = errorsType.GetMethod("Add", BindingFlags.Static | BindingFlags.Public);
                if (addMethod == null)
                    return;

                addMethod.Invoke(null, new object[] { new Exception(message), source });
            }
            catch (Exception)
            {
            }
        }
    }
}
