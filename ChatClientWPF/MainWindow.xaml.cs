using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private string _serverUrl = "http://localhost:5115"; // Default server URL// 위도와 경도를 직접 바인딩하기 위한 속성
        private double _latitude = 37.5642135;
        public double Latitude
        {
            get => _latitude;
            set
            {
                _latitude = value;
                OnPropertyChanged();
                // 위치 텍스트도 함께 업데이트
                LocationText = $"위치: {_latitude:F6}, {_longitude:F6}";
            }
        }

        private double _longitude = 127.0016985;
        public double Longitude
        {
            get => _longitude;
            set
            {
                _longitude = value;
                OnPropertyChanged();
                // 위치 텍스트도 함께 업데이트
                LocationText = $"위치: {_latitude:F6}, {_longitude:F6}";
            }
        }
        // 위치 텍스트 표시용 속성
        private string _locationText = "위치: 37.5642135, 127.0016985";
        public string LocationText
        {
            get => _locationText;
            set
            {
                _locationText = value;
                OnPropertyChanged();
            }
        }

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

        private string _selectedGenderValue = "male";
        public string SelectedGenderValue
        {
            get => _selectedGenderValue;
            set
            {
                _selectedGenderValue = value;
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
                    _latitude = config.Latitude;
                    _longitude = config.Longitude;
                    SetGender(config.Gender);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 파일을 로드하는 데 실패했습니다: {ex.Message}");
            }
        }

        private void SetGender(string gender)
        {
            ComboBox genderComboBox = (ComboBox)FindName("GenderComboBox");
            foreach (ComboBoxItem item in genderComboBox.Items)
            {
                if (item.Tag.ToString() == gender)
                {
                    SelectedGender = item;
                    SelectedGenderValue = SelectedGender.Tag.ToString();
                    break;
                }
            }
        }

        private void SaveClientIdToStorage()
        {
            try
            {
                var config = new ClientConfig
                {
                    ClientId = _clientId,
                    ServerUrl = ServerUrl,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    Gender = SelectedGenderValue
                };
                var json = JsonConvert.SerializeObject(config);
                System.IO.File.WriteAllText("client_config.json", json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 파일을 저장하는 데 실패했습니다: {ex.Message}");
            }
        }
        Stopwatch sp = new Stopwatch();
        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            sp.Restart();
            if (IsConnected)
            {
                await DisconnectAsync();
                return;
            }
            System.Diagnostics.Debug.WriteLine($"Connect_Click 1: {sp.ElapsedMilliseconds}ms");
            try
            {
                ConnectionStatus = "연결 중...";

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{ServerUrl}/chathub")
                    .WithAutomaticReconnect()
                    .Build();
                System.Diagnostics.Debug.WriteLine($"Connect_Click 2: {sp.ElapsedMilliseconds}ms");
                RegisterHubCallbacks();
                System.Diagnostics.Debug.WriteLine($"Connect_Click 3: {sp.ElapsedMilliseconds}ms");
                await _hubConnection.StartAsync();
                System.Diagnostics.Debug.WriteLine($"Connect_Click 4: {sp.ElapsedMilliseconds}ms");
                await RegisterClientAsync();
                System.Diagnostics.Debug.WriteLine($"Connect_Click 5: {sp.ElapsedMilliseconds}ms");
                IsConnected = true;
                ConnectionStatus = "연결됨";

                // 현재 위치 업데이트 (기본값은 서울 중심부)
                //await UpdateLocationAsync(37.5642135, 127.0016985);
                //System.Diagnostics.Debug.WriteLine($"Connect_Click 6: {sp.ElapsedMilliseconds}ms");
                //await UpdateGenderAsync();
                //System.Diagnostics.Debug.WriteLine($"Connect_Click 7: {sp.ElapsedMilliseconds}ms");
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
                //var data = JObject.FromObject(matchData);
                string jsonResponse = matchData.ToString(); // 또는 response.ToString()
                JObject data = JObject.Parse(jsonResponse);
                string partnerGender = data["partnerGender"].ToString();
                double distance = data["distance"].ToObject<double>();

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
                string jsonResponse = messageData.ToString(); // 또는 response.ToString()
                JObject data = JObject.Parse(jsonResponse);
                string senderId = data["senderId"].ToString();
                string message = data["message"].ToString();
                DateTime timestamp = data["timestamp"].ToObject<DateTime>();

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
                // 기본 위치 설정 (서울 중심부)
                //double latitude = 37.5642135;
                //double longitude = 127.0016985;

                // 성별 정보 가져오기 (선택된 ComboBoxItem이 없으면 기본값으로 male 사용)
                string gender = "male";
                if (SelectedGender != null && SelectedGender.Tag != null)
                {
                    gender = SelectedGender.Tag.ToString();
                }

                // 모든 정보를 한 번에 전송
                await _hubConnection.InvokeAsync("Register", new
                {
                    ClientId = _clientId,
                    Latitude = _latitude,
                    Longitude = _longitude,
                    Gender = gender
                });
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
                await _hubConnection.InvokeAsync("JoinWaitingQueue", Latitude, Longitude, SelectedGender.Tag.ToString());
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

        // 종료 버튼 이벤트 핸들러 추가
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // 먼저 연결 해제
            if (IsConnected)
            {
                DisconnectAsync().ContinueWith(t =>
                {
                    // UI 스레드에서 앱 종료
                    Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                });
            }
            else
            {
                // 연결이 없으면 바로 종료
                Application.Current.Shutdown();
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
            //DisconnectAsync().Wait();
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
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Gender { get; set; }
    }
}