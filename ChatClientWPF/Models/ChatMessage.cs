using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace ChatClientWPF.Models
{
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content;
        private bool _isFromMe;
        private bool _isSystemMessage;
        private DateTime _timestamp;

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
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

        public bool IsSystemMessage
        {
            get => _isSystemMessage;
            set
            {
                _isSystemMessage = value;
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

        public HorizontalAlignment MessageAlignment =>
            IsSystemMessage ? HorizontalAlignment.Center :
            IsFromMe ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        public Brush MessageBackground =>
            IsSystemMessage ? Brushes.LightGray :
            IsFromMe ? Brushes.LightBlue : Brushes.White;

        public string FormattedTime => Timestamp.ToString("HH:mm:ss");

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
