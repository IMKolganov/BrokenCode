using BrokenCode.Etc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrokenCode.Interfaces
{
    public interface ILicenseService : IDisposable
    {
        Task<ICollection<LicenseInfo>> GetLicensesAsync(Guid domainId, ICollection<string> emails);

        Task<int> GetLicensedUserCountAsync(Guid domainId);

        LicenseServiceSettings Settings { get; set; }
    }
}
