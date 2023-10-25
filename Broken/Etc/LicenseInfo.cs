using System;

namespace BrokenCode.Etc
{
    public class LicenseInfo
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public bool IsTrial { get; set; }
    }
}
