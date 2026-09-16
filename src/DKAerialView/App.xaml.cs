using System.Windows;
using System.Windows.Threading;

namespace DKAerialView;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"처리되지 않은 오류가 발생했습니다.\n\n{e.Exception.GetType().Name}: {e.Exception.Message}",
            "DK AerialView 오류",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}
