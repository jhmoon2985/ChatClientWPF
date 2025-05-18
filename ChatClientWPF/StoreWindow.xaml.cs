using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ChatClientWPF.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatClientWPF
{
    public partial class StoreWindow : Window, INotifyPropertyChanged
    {
        private readonly string _clientId;
        private readonly string _serverUrl;
        private readonly HttpClient _httpClient;
        private int _points;

        public ObservableCollection<ProductItem> Products { get; } = new ObservableCollection<ProductItem>();
        public string PointsText => $"현재 포인트: {_points:N0} P";

        // 구매 명령
        public ICommand PurchaseCommand { get; }

        // 포인트 업데이트 이벤트
        public event EventHandler<PointsUpdatedEventArgs> PointsUpdated;

        public StoreWindow(string clientId, int points, string serverUrl, HttpClient httpClient)
        {
            InitializeComponent();
            DataContext = this;

            this.Name = "StoreWindowControl"; // XAML에서 바인딩 참조용

            _clientId = clientId;
            _points = points;
            _serverUrl = serverUrl;
            _httpClient = httpClient;

            // 구매 명령 초기화
            PurchaseCommand = new RelayCommand<ProductItem>(PurchaseProduct);

            // 상품 목록 초기화
            InitializeProducts();
        }

        private void InitializeProducts()
        {
            // 기본 상품 추가
            Products.Add(new ProductItem
            {
                Id = "basic_points",
                Name = "기본 포인트 패키지",
                Description = "1,000 포인트 충전 (선호도 설정 1회 이용 가능)",
                Price = 1000,
                PointsAmount = 1000
            });

            Products.Add(new ProductItem
            {
                Id = "standard_points",
                Name = "스탠다드 포인트 패키지",
                Description = "3,000 포인트 충전 (선호도 설정 3회 이용 가능)",
                Price = 3000,
                PointsAmount = 3000
            });

            Products.Add(new ProductItem
            {
                Id = "premium_points",
                Name = "프리미엄 포인트 패키지",
                Description = "5,000 포인트 충전 (선호도 설정 5회 이용 가능)",
                Price = 5000,
                PointsAmount = 5000
            });

            // 테스트용 무료 포인트 추가 (실제 서비스에서는 제거)
            Products.Add(new ProductItem
            {
                Id = "free_points",
                Name = "무료 테스트 포인트",
                Description = "테스트용 무료 포인트 1,000 P",
                Price = 0,
                PointsAmount = 1000
            });
        }

        private async void PurchaseProduct(ProductItem product)
        {
            if (product == null)
                return;

            try
            {
                // 실제 결제 처리 로직 (여기서는 무료 포인트만 구현)
                if (product.Price > 0)
                {
                    // TODO: 실제 결제 처리 연동
                    MessageBoxResult result = MessageBox.Show(
                        $"{product.Name}을(를) {product.Price:N0}원에 구매하시겠습니까?",
                        "결제 확인",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;

                    // 결제 성공 가정
                    MessageBox.Show($"결제가 완료되었습니다. {product.PointsAmount:N0} 포인트가 충전됩니다.", "결제 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // 포인트 충전 요청
                await ChargePointsAsync(product.PointsAmount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"구매 처리 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ChargePointsAsync(int amount)
        {
            try
            {
                // 포인트 충전 요청
                var request = new PointChargeRequest
                {
                    ClientId = _clientId,
                    Amount = amount
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_serverUrl}/api/client/{_clientId}/points", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(responseJson);

                    int newPoints = data["points"].ToObject<int>();

                    // 포인트 업데이트
                    _points = newPoints;
                    OnPropertyChanged(nameof(PointsText));

                    // 이벤트 발생
                    PointsUpdated?.Invoke(this, new PointsUpdatedEventArgs(newPoints));

                    MessageBox.Show($"{amount:N0} 포인트가 충전되었습니다.", "포인트 충전 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"서버 오류: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"포인트 충전 요청 실패: {ex.Message}", ex);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 포인트 업데이트 이벤트 아규먼트
    public class PointsUpdatedEventArgs : EventArgs
    {
        public int NewPoints { get; }

        public PointsUpdatedEventArgs(int newPoints)
        {
            NewPoints = newPoints;
        }
    }

    // 상품 아이템 클래스
    public class ProductItem : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _description;
        private int _price;
        private int _pointsAmount;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public int Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PriceText));
            }
        }

        public int PointsAmount
        {
            get => _pointsAmount;
            set
            {
                _pointsAmount = value;
                OnPropertyChanged();
            }
        }

        public string PriceText => Price > 0 ? $"{Price:N0}원" : "무료";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // RelayCommand 클래스
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute) : this(execute, null)
        {
        }

        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}