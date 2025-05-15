// Models/ImageMessage.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChatClientWPF.Models
{
    public class ImageMessage : INotifyPropertyChanged
    {
        private string _imageId;
        private string _thumbnailUrl;
        private string _imageUrl;
        private bool _isFromMe;
        private DateTime _timestamp;
        private BitmapImage _thumbnail;
        private BitmapImage _fullImage;
        private bool _isLoading = true;

        public string ImageId
        {
            get => _imageId;
            set
            {
                _imageId = value;
                OnPropertyChanged();
            }
        }

        public string ThumbnailUrl
        {
            get => _thumbnailUrl;
            set
            {
                _thumbnailUrl = value;
                OnPropertyChanged();
            }
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                _imageUrl = value;
                OnPropertyChanged();
            }
        }

        public bool IsFromMe
        {
            get => _isFromMe;
            set
            {
                _isFromMe = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MessageAlignment));
                OnPropertyChanged(nameof(MessageBackground));
            }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTime));
            }
        }

        public BitmapImage Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged();
                IsLoading = false;
            }
        }

        public BitmapImage FullImage
        {
            get => _fullImage;
            set
            {
                _fullImage = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public HorizontalAlignment MessageAlignment =>
            IsFromMe ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        public Brush MessageBackground =>
            IsFromMe ? Brushes.LightBlue : Brushes.White;

        public string FormattedTime => Timestamp.ToString("HH:mm:ss");

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}