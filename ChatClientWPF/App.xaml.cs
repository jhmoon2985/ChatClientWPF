using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChatClientWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // MainWindow 객체 생성
            var mainWindow = new MainWindow();

            // SplashWindow 생성 및 표시
            var splashWindow = new SplashWindow(mainWindow);
            splashWindow.Show();

            // MainWindow는 SplashWindow에서 표시
        }
    }
}