using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClientWPF.Models
{
    public class ClientConfig
    {
        public string ClientId { get; set; }
        public string ServerUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Gender { get; set; }
        public string PreferredGender { get; set; } = "any";
        public int MaxDistance { get; set; } = 10000;
        // 포인트 관련 정보 추가
        public int Points { get; set; } = 0;
        // 선호도 설정 활성화 시간
        public DateTime? PreferenceActiveUntil { get; set; } = null;
    }

    // 새로운 모델 - 포인트 정보
    public class PointInfo
    {
        public int Points { get; set; } = 0;
        public DateTime? PreferenceActiveUntil { get; set; } = null;
        public bool IsPreferenceActive => PreferenceActiveUntil.HasValue && PreferenceActiveUntil.Value > DateTime.UtcNow;
        public string TimeLeftText => IsPreferenceActive ?
            $"선호도 설정 만료: {(PreferenceActiveUntil.Value - DateTime.UtcNow).Minutes}분 {(PreferenceActiveUntil.Value - DateTime.UtcNow).Seconds}초" :
            "선호도 설정 비활성화";
    }

    // 새로운 모델 - 포인트 충전 요청
    public class PointChargeRequest
    {
        public string ClientId { get; set; }
        public int Amount { get; set; }
    }

    // 새로운 모델 - 선호도 활성화 요청
    public class ActivatePreferenceRequest
    {
        public string ClientId { get; set; }
        public string PreferredGender { get; set; }
        public int MaxDistance { get; set; }
    }
}
