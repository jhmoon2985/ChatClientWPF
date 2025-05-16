using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChatClientWPF.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
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
        // 선호도 관련 속성 추가
        private ComboBoxItem _selectedPreferredGender;
        private int _maxDistance = 10000;
        private string _preferredGenderValue = "any";

        public ComboBoxItem SelectedPreferredGender
        {
            get => _selectedPreferredGender;
            set
            {
                _selectedPreferredGender = value;
                if (value != null && value.Tag != null)
                {
                    _preferredGenderValue = value.Tag.ToString();
                }
                OnPropertyChanged();
            }
        }

        public int MaxDistance
        {
            get => _maxDistance;
            set
            {
                _maxDistance = value;
                OnPropertyChanged();
            }
        }
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

                    // 선호도 설정 로드
                    if (!string.IsNullOrEmpty(config.PreferredGender))
                    {
                        SetPreferredGender(config.PreferredGender);
                    }
                    if (config.MaxDistance > 0)
                    {
                        MaxDistance = config.MaxDistance;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 파일을 로드하는 데 실패했습니다: {ex.Message}");
            }
        }
        private void SetPreferredGender(string preferredGender)
        {
            ComboBox preferredGenderComboBox = (ComboBox)FindName("PreferredGenderComboBox");
            foreach (ComboBoxItem item in preferredGenderComboBox.Items)
            {
                if (item.Tag.ToString() == preferredGender)
                {
                    SelectedPreferredGender = item;
                    _preferredGenderValue = preferredGender;
                    break;
                }
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
                    Gender = SelectedGenderValue,
                    PreferredGender = _preferredGenderValue,
                    MaxDistance = MaxDistance
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

                JoinQueue();
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

            // 이미지 메시지 수신 콜백 추가
            _hubConnection.On<object>("ReceiveImageMessage", async (messageData) =>
            {
                string jsonResponse = messageData.ToString();
                JObject data = JObject.Parse(jsonResponse);
                string senderId = data["senderId"].ToString();
                string imageId = data["imageId"].ToString();
                string thumbnailUrl = data["thumbnailUrl"].ToString();
                string imageUrl = data["imageUrl"].ToString();
                DateTime timestamp = data["timestamp"].ToObject<DateTime>();

                await Dispatcher.InvokeAsync(() =>
                {
                    bool isFromMe = senderId == _clientId;
                    Messages.Add(new ChatMessage
                    {
                        IsFromMe = isFromMe,
                        Content = "[이미지]",
                        Timestamp = timestamp,
                        ThumbnailUrl = $"{ServerUrl}{thumbnailUrl}",
                        ImageUrl = $"{ServerUrl}{imageUrl}",
                        IsImageMessage = true
                    });

                    // 자동 스크롤
                    if (MessageList.Items.Count > 0)
                    {
                        MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);
                    }
                });
            });

            // 선호도 업데이트 응답을 위한 콜백 추가
            _hubConnection.On("PreferencesUpdated", async () =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    Messages.Add(new ChatMessage
                    {
                        IsSystemMessage = true,
                        Content = "매칭 선호도가 서버에 저장되었습니다."
                    });
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
                    Gender = gender,
                    PreferredGender = _preferredGenderValue,
                    MaxDistance = _maxDistance
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"등록 중 오류 발생: {ex.Message}");
            }
        }
        private async Task UpdatePreferencesAsync()
        {
            try
            {
                if (IsConnected)
                {
                    await _hubConnection.InvokeAsync("UpdatePreferences", _preferredGenderValue, MaxDistance);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"선호도 업데이트 중 오류 발생: {ex.Message}");
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

        private async void ReMatch_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected || IsMatched)
                return;

            try
            {
                MatchStatus = "매칭 대기열에 참가 중...";
                await _hubConnection.InvokeAsync("JoinWaitingQueue", Latitude, Longitude, SelectedGender.Tag.ToString(),
                    _preferredGenderValue, MaxDistance);
            }
            catch (Exception ex)
            {
                MatchStatus = "매칭 대기열 참가 실패";
                MessageBox.Show($"대기열 참가 중 오류 발생: {ex.Message}");
            }
        }

        private async void JoinQueue_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected || IsMatched)
                return;

            try
            {
                MatchStatus = "매칭 대기열에 참가 중...";
                await _hubConnection.InvokeAsync("JoinWaitingQueue", Latitude, Longitude, SelectedGender.Tag.ToString(),
                    _preferredGenderValue, MaxDistance);
            }
            catch (Exception ex)
            {
                MatchStatus = "매칭 대기열 참가 실패";
                MessageBox.Show($"대기열 참가 중 오류 발생: {ex.Message}");
            }
        }

        private async void JoinQueue()
        {
            if (!IsConnected || IsMatched)
                return;

            try
            {
                MatchStatus = "매칭 대기열에 참가 중...";
                await _hubConnection.InvokeAsync("JoinWaitingQueue", Latitude, Longitude, SelectedGender.Tag.ToString(),
                    _preferredGenderValue, MaxDistance);
            }
            catch (Exception ex)
            {
                MatchStatus = "매칭 대기열 참가 실패";
                MessageBox.Show($"대기열 참가 중 오류 발생: {ex.Message}");
            }
        }

        // 선호도 설정 저장 버튼 이벤트 핸들러
        private async void SavePreferences_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected)
                return;

            try
            {
                if (SelectedPreferredGender != null && SelectedPreferredGender.Tag != null)
                {
                    _preferredGenderValue = SelectedPreferredGender.Tag.ToString();
                }

                await UpdatePreferencesAsync();
                SaveClientIdToStorage();

                // 사용자에게 저장 완료 알림
                Messages.Add(new ChatMessage
                {
                    IsSystemMessage = true,
                    Content = $"매칭 선호도가 저장되었습니다. 선호 성별: {GetPreferredGenderDisplayText()}, 최대 거리: {MaxDistance}km"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"선호도 저장 중 오류 발생: {ex.Message}");
            }
        }

        // 선호 성별 표시 텍스트 반환
        private string GetPreferredGenderDisplayText()
        {
            switch (_preferredGenderValue)
            {
                case "male": return "남성만";
                case "female": return "여성만";
                default: return "제한 없음";
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

        // 이미지 전송 버튼 클릭 이벤트 처리
        private async void SendImage_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected || !IsMatched)
                return;

            // 파일 선택 대화상자
            var openFileDialog = new OpenFileDialog
            {
                Title = "이미지 선택",
                Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.gif|모든 파일|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 파일 업로드
                    await UploadImageAsync(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"이미지 전송 중 오류 발생: {ex.Message}");
                }
            }
        }

        // 이미지 업로드 메서드
        // ChatClientWPF/MainWindow.xaml.cs의 UploadImageAsync 메서드 수정

        private async Task UploadImageAsync(string filePath)
        {
            try
            {
                // 파일 정보 확인
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    MessageBox.Show("파일을 찾을 수 없습니다.");
                    return;
                }

                // 파일 크기 제한 확인 (5MB = 5,242,880 바이트)
                //const long maxFileSize = 5242880;
                const long maxFileSize = 512000;
                if (fileInfo.Length > maxFileSize)
                {
                    MessageBox.Show($"이미지 크기가 제한을 초과했습니다.\n최대 크기: 5MB\n현재 크기: {fileInfo.Length / 1024}KB",
                        "용량 초과", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // 이미지 압축 제안
                    if (MessageBox.Show("이미지를 압축하여 전송하시겠습니까?", "이미지 압축",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string compressedPath = await CompressImageAsync(filePath, maxFileSize);
                        if (!string.IsNullOrEmpty(compressedPath))
                        {
                            // 압축된 이미지로 재시도
                            await UploadImageAsync(compressedPath);
                            return;
                        }
                    }
                    return;
                }

                // 진행 상태 표시
                Messages.Add(new ChatMessage
                {
                    IsSystemMessage = true,
                    Content = "이미지 업로드 중..."
                });

                // 멀티파트 폼 데이터 생성
                using var content = new MultipartFormDataContent();
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
                content.Add(streamContent, "image", Path.GetFileName(filePath));

                // 업로드 요청
                var response = await _httpClient.PostAsync($"{ServerUrl}/api/client/{_clientId}/image", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"이미지 업로드 실패: {errorMessage}");

                    // 실패 메시지 표시
                    Messages.Add(new ChatMessage
                    {
                        IsSystemMessage = true,
                        Content = "이미지 전송에 실패했습니다."
                    });
                    return;
                }

                // 응답 정보 확인
                var responseJson = await response.Content.ReadAsStringAsync();
                var responseData = JObject.Parse(responseJson);

                Messages.Add(new ChatMessage
                {
                    IsSystemMessage = true,
                    Content = "이미지를 전송했습니다."
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지 업로드 중 오류 발생: {ex.Message}");

                // 오류 메시지 표시
                Messages.Add(new ChatMessage
                {
                    IsSystemMessage = true,
                    Content = $"이미지 전송 중 오류가 발생했습니다: {ex.Message}"
                });
            }
        }

        // 이미지 압축 메서드
        private async Task<string> CompressImageAsync(string sourcePath, long targetMaxSize)
        {
            try
            {
                // 임시 파일 경로 생성
                string tempPath = Path.Combine(Path.GetTempPath(), $"compressed_{Path.GetFileName(sourcePath)}");
                string extension = Path.GetExtension(sourcePath).ToLowerInvariant();

                // 포맷에 따라 다른 처리
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
                {
                    // WPF의 이미지 처리 사용
                    BitmapImage sourceImage = new BitmapImage();
                    sourceImage.BeginInit();
                    sourceImage.CacheOption = BitmapCacheOption.OnLoad;
                    sourceImage.UriSource = new Uri(sourcePath);
                    sourceImage.EndInit();

                    // 품질 설정 (JPEG용)
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();

                    // 초기 품질 설정
                    int quality = 90;
                    bool sizeReduced = false;

                    do
                    {
                        encoder = new JpegBitmapEncoder();
                        encoder.QualityLevel = quality;

                        using (FileStream fs = new FileStream(tempPath, FileMode.Create))
                        {
                            encoder.Frames.Add(BitmapFrame.Create(sourceImage));
                            encoder.Save(fs);
                        }

                        FileInfo compressedFile = new FileInfo(tempPath);
                        if (compressedFile.Length <= targetMaxSize)
                        {
                            sizeReduced = true;
                        }
                        else
                        {
                            quality -= 10; // 품질 단계적 감소
                        }

                        // 품질이 너무 낮아지면 중단
                        if (quality < 30)
                        {
                            MessageBox.Show("이미지를 충분히 압축할 수 없습니다. 더 작은 이미지를 사용해주세요.");
                            return null;
                        }

                    } while (!sizeReduced);

                    return tempPath;
                }
                else if (extension == ".gif")
                {
                    // GIF는 압축 지원하지 않음
                    MessageBox.Show("GIF 파일 압축은 지원하지 않습니다. 더 작은 파일을 사용해주세요.");
                    return null;
                }
                else
                {
                    // 지원하지 않는 형식
                    MessageBox.Show("지원하지 않는 이미지 형식입니다.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지 압축 중 오류 발생: {ex.Message}");
                return null;
            }
        }

        // MIME 타입 반환 메서드
        private string GetMimeType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        // 이미지 클릭 이벤트 핸들러 (원본 이미지 보기)
        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image && image.Tag is string imageUrl)
            {
                try
                {
                    // 이미지 뷰어 창 띄우기
                    var imageViewer = new ImageViewerWindow(imageUrl);
                    imageViewer.Owner = this;
                    imageViewer.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"이미지를 표시하는 중 오류 발생: {ex.Message}");
                }
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
        private string _thumbnailUrl;
        private string _imageUrl;
        private bool _isImageMessage;

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

        public bool IsImageMessage
        {
            get => _isImageMessage || !string.IsNullOrEmpty(ImageUrl);
            set
            {
                _isImageMessage = value;
                OnPropertyChanged();
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