using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;

namespace Zapto.AspNetCore.NetFx.Tests;

public class AspNetFixture : IAsyncLifetime
{
    private static readonly string[] IisExpressPaths = new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IIS Express", "iisexpress.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "IIS Express", "iisexpress.exe"),
        "iisexpress.exe"
    };

    private Process? _iisProcess;

    public string BaseAddress { get; private set; } = "";

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        // Pick a free port (static if preferred for CI/CD)
        int port = 5005;
        string sitePath = ProjectLocator.GetWebRoot();

        // Ensure iisexpress.exe is available
        string? iisExpressPath = null;

        foreach (var path in IisExpressPaths)
        {
            if (File.Exists(path) || path == "iisexpress.exe")
            {
                iisExpressPath = path;
                break;
            }
        }

        if (iisExpressPath == null)
        {
            throw new FileNotFoundException("Could not find iisexpress.exe. Please ensure IIS Express is installed.");
        }

        // Start IIS Express
        Environment.SetEnvironmentVariable("WEBSITE_PATH", sitePath);
        _iisProcess = Process.Start(iisExpressPath, $"/config:{sitePath}/applicationhost.config /site:Site /apppool:Clr4IntegratedAppPool");

        BaseAddress = $"http://localhost:{port}/";
        Client = new HttpClient { BaseAddress = new Uri(BaseAddress) };

        // Wait until site is reachable
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var _ = await Client.GetAsync("/");
                break;
            }
            catch
            {
                await Task.Delay(500);
            }

            if (_iisProcess!.HasExited)
            {
                throw new Exception("IIS Express process exited unexpectedly.");
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        if (_iisProcess is { HasExited: false })
        {
            _iisProcess.Kill();
            _iisProcess.Dispose();
        }
        return default;
    }
}

public static class ProjectLocator
{
    public static string GetWebRoot()
    {
        // Find 'tests' folder
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null && dir.GetDirectories().All(d => d.Name != "tests"))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new DirectoryNotFoundException("Could not find 'tests' folder in directory hierarchy.");
        }

        // Go to 'sandbox/WebFormsApp'
        var webFormsAppDir = Path.Combine(dir.FullName, "sandbox", "WebFormsApp");

        if (!Directory.Exists(webFormsAppDir))
        {
            throw new DirectoryNotFoundException($"Could not find 'WebFormsApp' folder at expected location: {webFormsAppDir}");
        }

        return webFormsAppDir;
    }
}