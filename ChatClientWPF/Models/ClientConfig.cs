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
    }
}
