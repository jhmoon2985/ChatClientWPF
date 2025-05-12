using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatClientWPF
{
    /// <summary>
    /// MainWindow.xaml.cs
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private HubConnection _hubConnection;
        private readonly HttpClient _httpClient;
        private string _clientId;
        private string _messageInput;
        private bool _isConnected;
        private bool _isMatched;
        private string _partnerGender;
        private double _distance;
        private string _connectionStatus;
        private string _matchStatus;
        private ComboBoxItem _selectedGender;
        private string _serverUrl = "http://localhost:5115"; // Default server URL

        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();
        public string MessageInput
        {
            get => _messageInput;
            set
            {
                _messageInput = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDisconnected));
                OnPropertyChanged(nameof(CanJoinQueue));
                OnPropertyChanged(nameof(CanSendMessage));
            }
        }

        public bool IsDisconnected => !_isConnected;

        public bool IsMatched
        {
            get => _isMatched;
            set
            {
                _isMatched = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSendMessage));
                OnPropertyChanged(nameof(CanEndChat));
                OnPropertyChanged(nameof(CanJoinQueue));
            }
        }

        public string PartnerGender
        {
            get => _partnerGender;
            set
            {
                _partnerGender = value;
                OnPropertyChanged();
            }
        }

        public double Distance
        {
            get => _distance;
            set
            {
                _distance = value;
                OnPropertyChanged();
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        public string MatchStatus
        {
            get => _matchStatus;
            set
            {
                _matchStatus = value;
                OnPropertyChanged();
            }
        }

        public ComboBoxItem SelectedGender
        {
            get => _selectedGender;
            set
            {
                _selectedGender = value;
                OnPropertyChanged(nameof(SelectedGender));
            }
        }

        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                _serverUrl = value;
                OnPropertyChanged();
            }
        }

        public bool CanJoinQueue => IsConnected && !IsMatched;
        public bool CanSendMessage => IsConnected && IsMatched;
        public bool CanEndChat => IsConnected && IsMatched;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _httpClient = new HttpClient();
            ConnectionStatus = "연결 끊김";
            MatchStatus = "매칭 대기 중";

            LoadClientIdFromStorage();
        }

        private void LoadClientIdFromStorage()
        {
            try
            {
                if (System.IO.File.Exists("client_config.json"))
                {
                    var json = System.IO.File.ReadAllText("client_config.json");
                    var config = JsonConvert.DeserializeObject<ClientConfig>(json);
                    _clientId = config.ClientId;
                    ServerUrl = config.ServerUrl ?? ServerUrl;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 파일을 로드하는 데 실패했습니다: {ex.Message}");
            }
        }

        private void SaveClientIdToStorage()
        {
            try
            {
                var config = new ClientConfig
                {
                    ClientId = _clientId,
                    ServerUrl = ServerUrl
                };
                var json = JsonConvert.SerializeObject(config);
                System.IO.File.WriteAllText("client_config.json", json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 파일을 저장하는 데 실패했습니다: {ex.Message}");
            }
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (IsConnected)
            {
                await DisconnectAsync();
                return;
            }

            try
            {
                ConnectionStatus = "연결 중...";

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{ServerUrl}/chathub")
                    .WithAutomaticReconnect()
                    .Build();

                RegisterHubCallbacks();

                await _hubConnection.StartAsync();
                await RegisterClientAsync();

                IsConnected = true;
                ConnectionStatus = "연결됨";

                // 현재 위치 업데이트 (기본값은 서울 중심부)
                await UpdateLocationAsync(37.5642135, 127.0016985);
                await UpdateGenderAsync();
            }
            catch (Exception ex)
            {
                ConnectionStatus = "연결 실패";
                MessageBox.Show($"서버 연결에 실패했습니다: {ex.Message}");
            }
        }

        private void RegisterHubCallbacks()
        {
            _hubConnection.On<object>("Registered", async (response) =>
            {
                string jsonResponse = response.ToString(); // 또는 response.ToString()
                JObject data = JObject.Parse(jsonResponse);
                _clientId = data["clientId"].ToString();
                SaveClientIdToStorage();

                await Dispatcher.InvokeAsync(() =>
                {
                    ConnectionStatus = $"등록됨: {_clientId}";
                });
            });

            _hubConnection.On("EnqueuedToWaiting", async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MatchStatus = "매칭 대기 중...";
                    IsMatched = false;
                });
            });

            _hubConnection.On<object>("Matched", async (matchData) =>
            {
                var data = JObject.FromObject(matchData);
                string partnerGender = data["PartnerGender"].ToString();
                double distance = data["Distance"].ToObject<double>();

                await Dispatcher.InvokeAsync(() =>
                {
                    IsMatched = true;
                    PartnerGender = partnerGender;
                    Distance = distance;
                    MatchStatus = $"매칭됨: {(partnerGender == "male" ? "남성" : "여성")}, 거리: {distance:F1}km";

                    Messages.Clear();
                    Messages.Add(new ChatMessage
                    {
                        IsSystemMessage = true,
                        Content = $"새로운 상대방과 연결되었습니다. 상대방 성별: {(partnerGender == "male" ? "남성" : "여성")}, 거리: {distance:F1}km"
                    });
                });
            });

            _hubConnection.On("MatchEnded", async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    IsMatched = false;
                    MatchStatus = "매칭 종료됨";
                    Messages.Add(new ChatMessage
                    {
                        IsSystemMessage = true,
                        Content = "상대방이 대화를 종료했습니다."
                    });
                });
            });

            _hubConnection.On<object>("ReceiveMessage", async (messageData) =>
            {
                var data = JObject.FromObject(messageData);
                string senderId = data["SenderId"].ToString();
                string message = data["Message"].ToString();
                DateTime timestamp = data["Timestamp"].ToObject<DateTime>();

                await Dispatcher.InvokeAsync(() =>
                {
                    bool isFromMe = senderId == _clientId;
                    Messages.Add(new ChatMessage
                    {
                        IsFromMe = isFromMe,
                        Content = message,
                        Timestamp = timestamp
                    });

                    // 자동 스크롤
                    if (MessageList.Items.Count > 0)
                    {
                        MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);
                    }
                });
            });

            _hubConnection.Closed += async (error) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    IsConnected = false;
                    IsMatched = false;
                    ConnectionStatus = "연결 끊김";
                    MatchStatus = "매칭 없음";

                    if (error != null)
                    {
                        Messages.Add(new ChatMessage
                        {
                            IsSystemMessage = true,
                            Content = $"서버 연결이 끊어졌습니다: {error.Message}"
                        });
                    }
                });
            };
        }

        private async Task RegisterClientAsync()
        {
            try
            {
                await _hubConnection.InvokeAsync("Register", _clientId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"등록 중 오류 발생: {ex.Message}");
            }
        }

        private async Task UpdateLocationAsync(double latitude, double longitude)
        {
            try
            {
                if (IsConnected)
                {
                    await _hubConnection.InvokeAsync("UpdateLocation", latitude, longitude);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"위치 업데이트 중 오류 발생: {ex.Message}");
            }
        }
        private async Task UpdateGenderAsync()
        {
            try
            {
                if (IsConnected)
                {
                    await _hubConnection.InvokeAsync("UpdateGender", SelectedGender.Tag);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"성별 업데이트 중 오류 발생: {ex.Message}");
            }
        }
        private async void JoinQueue_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected || IsMatched)
                return;

            try
            {
                MatchStatus = "매칭 대기열에 참가 중...";
                await _hubConnection.InvokeAsync("JoinWaitingQueue", SelectedGender);
            }
            catch (Exception ex)
            {
                MatchStatus = "매칭 대기열 참가 실패";
                MessageBox.Show($"대기열 참가 중 오류 발생: {ex.Message}");
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            if (!IsConnected || !IsMatched || string.IsNullOrWhiteSpace(MessageInput))
                return;

            try
            {
                await _hubConnection.InvokeAsync("SendMessage", MessageInput);
                MessageInput = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"메시지 전송 중 오류 발생: {ex.Message}");
            }
        }

        private async void EndChat_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected || !IsMatched)
                return;

            try
            {
                await _hubConnection.InvokeAsync("EndChat");
                Messages.Add(new ChatMessage
                {
                    IsSystemMessage = true,
                    Content = "대화를 종료하고 새로운 상대를 찾습니다."
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"대화 종료 중 오류 발생: {ex.Message}");
            }
        }

        private async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    IsConnected = false;
                    IsMatched = false;
                    ConnectionStatus = "연결 끊김";
                    MatchStatus = "매칭 없음";
                    _hubConnection = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"연결 종료 중 오류 발생: {ex.Message}");
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DisconnectAsync().Wait();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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

    public class ClientConfig
    {
        public string ClientId { get; set; }
        public string ServerUrl { get; set; }
    }
}