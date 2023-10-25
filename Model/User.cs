using System;
using System.ComponentModel.DataAnnotations;

namespace BrokenCode.Model
{
    public class User
    {
        public Guid Id { get; set; }

        public Guid DomainId { get; set; }

        [Required]
        [MaxLength(128)]
        public string UserEmail { get; set; }

        public string UserName { get; set; }

        public bool BackupEnabled { get; set; }

        public UserState State { get; set; }

        public virtual Email Email { get; set; }

        public virtual Drive Drive { get; set; }

        public virtual Calendar Calendar { get; set; }

    }
}
