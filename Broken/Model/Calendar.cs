using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrokenCode.Model
{
    public class Calendar
    {
        [Key, ForeignKey("User")]
        public Guid Id { get; set; }
        public virtual User User { get; set; }
        public DateTime? LastBackupDate { get; set; }
        public string LastBackupStatus { get; set; }
    }
}
