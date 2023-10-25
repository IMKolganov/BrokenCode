using System;

namespace BrokenCode.Etc
{
    public class UserStatistics
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public bool InBackup { get; set; }
        public string EmailLastBackupStatus { get; set; }
        public DateTime? EmailLastBackupDate { get; set; }
        public string DriveLastBackupStatus { get; set; }
        public DateTime? DriveLastBackupDate { get; set; }
        public string CalendarLastBackupStatus { get; set; }
        public DateTime? CalendarLastBackupDate { get; set; }
        public string LicenseType { get; set; }
    }
}
