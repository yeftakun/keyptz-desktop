using System.Threading;
using System.Windows.Forms;

namespace Key2Xbox.Rewrite;

internal static class Program
{
    private const string MutexName = "KeyPTZ_vMix_Controller_Mutex_12345";
    private const string AppDataFolderName = "keyPTZ-desktop";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("KeyPTZ sebelumnya sudah berjalan!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ApplicationConfiguration.Initialize();
        var dataDirectory = ResolveDataDirectory();
        var paths = new AppPaths(dataDirectory);
        Application.Run(new TrayApplicationContext(paths));
    }

    private static string ResolveDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            return AppContext.BaseDirectory;
        }

        var appDataDirectory = Path.Combine(localAppData, AppDataFolderName);
        Directory.CreateDirectory(appDataDirectory);
        TryMigrateLegacyData(AppContext.BaseDirectory, appDataDirectory);
        return appDataDirectory;
    }

    private static void TryMigrateLegacyData(string sourceDirectory, string targetDirectory)
    {
        if (PathsEqual(sourceDirectory, targetDirectory))
        {
            return;
        }

        try
        {
            var sourceConfig = Path.Combine(sourceDirectory, "config.json");
            var targetConfig = Path.Combine(targetDirectory, "config.json");

            if (!File.Exists(targetConfig) && File.Exists(sourceConfig))
            {
                File.Copy(sourceConfig, targetConfig, overwrite: false);
            }

            var targetProfiles = Path.Combine(targetDirectory, "profiles");
            Directory.CreateDirectory(targetProfiles);

            var sourceProfilesCandidates = new[]
            {
                Path.Combine(sourceDirectory, "profiles"),
                Path.Combine(sourceDirectory, "profile")
            };

            foreach (var sourceProfiles in sourceProfilesCandidates)
            {
                if (!Directory.Exists(sourceProfiles))
                {
                    continue;
                }

                foreach (var sourceFile in Directory.GetFiles(sourceProfiles, "*.json"))
                {
                    var fileName = Path.GetFileName(sourceFile);
                    var targetFile = Path.Combine(targetProfiles, fileName);
                    if (!File.Exists(targetFile))
                    {
                        File.Copy(sourceFile, targetFile, overwrite: false);
                    }
                }
            }
        }
        catch
        {
            // Keep startup resilient if migration fails.
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        var leftFull = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var rightFull = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(leftFull, rightFull, StringComparison.OrdinalIgnoreCase);
    }
}