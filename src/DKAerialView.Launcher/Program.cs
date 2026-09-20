using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DKAerialView.Launcher;

internal static class Program
{
    private const string DesktopRuntimeName = "Microsoft.WindowsDesktop.App";
    private const int RequiredMajorVersion = 10;
    private const string RuntimeDownloadUrl = "https://dotnet.microsoft.com/download/dotnet/10.0";

    private const uint MbOk = 0x00000000;
    private const uint MbYesNo = 0x00000004;
    private const uint MbIconError = 0x00000010;
    private const uint MbIconWarning = 0x00000030;
    private const int IdYes = 6;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [STAThread]
    private static int Main()
    {
        try
        {
            if (!IsRequiredDesktopRuntimeInstalled())
            {
                var result = MessageBoxW(
                    IntPtr.Zero,
                    "DK AerialView를 실행하려면 Microsoft .NET 10 Desktop Runtime (x64)이 필요합니다.\n\n공식 다운로드 페이지를 여시겠습니까?",
                    "DK AerialView - 필요한 구성 요소",
                    MbYesNo | MbIconWarning);

                if (result == IdYes)
                    OpenUrl(RuntimeDownloadUrl);

                return 10;
            }

            var appPath = Path.Combine(AppContext.BaseDirectory, "app", "DK-AerialView.App.exe");
            if (!File.Exists(appPath))
            {
                MessageBoxW(
                    IntPtr.Zero,
                    $"실제 프로그램 파일을 찾을 수 없습니다.\n\n{appPath}\n\nZIP을 다시 압축 해제해 주세요.",
                    "DK AerialView - 실행 오류",
                    MbOk | MbIconError);
                return 20;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                WorkingDirectory = Path.GetDirectoryName(appPath)!,
                UseShellExecute = true
            });

            return 0;
        }
        catch (Exception ex)
        {
            MessageBoxW(
                IntPtr.Zero,
                $"DK AerialView를 시작하지 못했습니다.\n\n{ex.Message}",
                "DK AerialView - 실행 오류",
                MbOk | MbIconError);
            return 99;
        }
    }

    private static bool IsRequiredDesktopRuntimeInstalled()
    {
        var dotnetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "dotnet",
            "dotnet.exe");

        if (!File.Exists(dotnetPath))
            return false;

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = dotnetPath,
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
                return false;

            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000) || process.ExitCode != 0)
                return false;

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!line.StartsWith(DesktopRuntimeName + " ", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 &&
                    Version.TryParse(parts[1], out var version) &&
                    version.Major == RequiredMajorVersion)
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}
