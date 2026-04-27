using System.Reflection;
using System.Text;

namespace DTIWindow.Integration
{
    public static class CrashLogger
    {
        private static readonly string _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "vatsys-dti-window"
        );

        public static void Attach()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            try { System.Windows.Forms.Application.ThreadException += OnThreadException; }
            catch { }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                WriteLog("UnhandledException", ex, e.IsTerminating);
        }

        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            WriteLog("ThreadException", e.Exception, false);
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            WriteLog("UnobservedTaskException", e.Exception, false);
            e.SetObserved();
        }

        private static void WriteLog(string type, Exception ex, bool isTerminating)
        {
            try
            {
                Directory.CreateDirectory(_logDirectory);

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var path = Path.Combine(_logDirectory, $"crash-{timestamp}.log");
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

                var sb = new StringBuilder();
                sb.AppendLine("=== Traffic Info Plugin — Crash Log ===");
                sb.AppendLine($"Time:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Version: {version}");
                sb.AppendLine($"Type:    {type}");
                sb.AppendLine($"Fatal:   {isTerminating}");
                sb.AppendLine();
                AppendException(sb, ex);

                File.WriteAllText(path, sb.ToString());
            }
            catch { }
        }

        private static void AppendException(StringBuilder sb, Exception ex, int depth = 0)
        {
            var pad = new string(' ', depth * 2);
            sb.AppendLine($"{pad}{ex.GetType().FullName}: {ex.Message}");
            sb.AppendLine($"{pad}Stack trace:");
            foreach (var line in (ex.StackTrace ?? "(none)").Split('\n'))
                sb.AppendLine($"{pad}  {line.TrimEnd()}");

            if (ex is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    sb.AppendLine($"{pad}--- Aggregate inner ---");
                    AppendException(sb, inner, depth + 1);
                }
            }
            else if (ex.InnerException != null)
            {
                sb.AppendLine($"{pad}--- Inner exception ---");
                AppendException(sb, ex.InnerException, depth + 1);
            }
        }
    }
}
