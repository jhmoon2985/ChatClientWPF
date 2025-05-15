using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ChatClientWPF
{
    public partial class ImageViewerWindow : Window
    {
        public ImageViewerWindow(string imageUrl)
        {
            InitializeComponent();

            try
            {
                // 이미지 로드
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageUrl);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                ImageDisplay.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지를 로드하는 데 실패했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}