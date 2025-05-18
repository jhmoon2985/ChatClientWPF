using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace ChatClientWPF
{
    public partial class SplashWindow : Window
    {
        private readonly MainWindow _mainWindow;

        public SplashWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            Loaded += SplashWindow_Loaded;
        }

        private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 애니메이션 효과 추가
            var storyboard = new Storyboard();

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(1.5))
            };
            Storyboard.SetTarget(fadeIn, this);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeIn);

            storyboard.Begin();

            // 서버 연결 시작
            await Task.Delay(2000); // 로고 보여주기 위한 지연

            StatusText.Text = "서버에 연결 중...";
            await Task.Delay(500);

            try
            {
                // MainWindow에서 서버 연결 시작
                await _mainWindow.InitializeConnectionAsync();

                StatusText.Text = "연결 성공! 화면 전환 중...";
                await Task.Delay(1000);

                // 페이드 아웃 애니메이션
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.5))
                };
                fadeOut.Completed += (s, args) =>
                {
                    _mainWindow.Show();
                    Close();
                };

                BeginAnimation(OpacityProperty, fadeOut);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"연결 실패: {ex.Message}";
                MessageBox.Show($"서버 연결에 실패했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                Application.Current.Shutdown();
            }
        }
    }
}