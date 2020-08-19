using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot_Before_Chime
{
    public class SeekerData
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string location { get; set; }
        public string question { get; set; }
        public bool webVisitor { get; set; }
        public bool domainAuthenticated { get; set; }
        public string sip { get; set; }
        public string SeekerDN { get; set; }
        public string ip { get; set; }
        public string referrerURL { get; set; }
        public string hostname { get; set; }
        public int id { get; set; }
        public string sessionGuid { get; set; }
        public int seekerOffsetMinutes { get; set; }
        public string platform { get; set; }
    }
}
