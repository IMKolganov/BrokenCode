using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrokenCode.Etc;
using BrokenCode.Interfaces;
using BrokenCode.Model;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrokenCode
{
    public class BrokenService
    {
        private readonly UserDbContext _db;
        private readonly ILicenseServiceProvider _licenseServiceProvider;
        private int _counter;

        private static readonly ILog Log = LogManager.GetLogger(typeof(BrokenService));

        public BrokenService(UserDbContext db, ILicenseServiceProvider licenseServiceProvider)
        {
            _db = db;
            _licenseServiceProvider = licenseServiceProvider;
        }

        public async Task<IActionResult> GetReport(GetReportRequest request)
        {
            bool acquiredLock = false;
            try
            {
                Monitor.Enter(_counter, ref acquiredLock);

                while (true)
                {
                    try
                    {
                        if (_counter > 10)
                            return new StatusCodeResult(500);

                        return await GetReportAsync(request);
                    }
                    catch
                    {
                        Log.Debug($"Attempt {_counter} failed");
                        _counter++;

                        Thread.Sleep(1000);
                    }
                }
            }
            finally
            {
                if (acquiredLock)
                    Monitor.Exit(_counter);
            }
        }

        private async Task<IActionResult> GetReportAsync(GetReportRequest request)
        {
            var filteredUsers = _db.Users.Where(d => d.DomainId == request.DomainId).Where(b => InBackup(b)).OrderBy(o => o.UserEmail).Cast<User>();

            int totalCount = filteredUsers != null ? filteredUsers.Count() : 0;
            filteredUsers = filteredUsers.Take(request.PageSize).Skip(request.PageSize * request.PageNumber);

            Dictionary<Guid, LicenseInfo> userLicenses = new Dictionary<Guid, LicenseInfo>();
            using var licenseService = GetLicenseServiceAndConfigure();

            if (licenseService != null)
            {
                Log.Info($"Total licenses for domain '{request.DomainId}': {licenseService.GetLicensedUserCountAsync(request.DomainId)}");

                List<string> emails = filteredUsers.Select(u => u.UserEmail).ToList();
                ICollection<LicenseInfo> result = null;

                try
                {
                    result = licenseService.GetLicensesAsync(request.DomainId, emails).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log.Error($"Problem of getting licenses information: {ex.Message}");
                    throw ex;
                }

                if (result != null)
                {
                    foreach (User user in filteredUsers)
                    {
                        if (result.Count(r => r.Email == user.UserEmail) > 0)
                        {
                            userLicenses.Add(user.Id, result.Where(r => r.Email == user.UserEmail).First());
                        }
                    }
                }
            }

            var usersData = (await filteredUsers.ToListAsync())
                .Select(u =>
                {
                    string licenseType = userLicenses.ContainsKey(u.Id) ? (userLicenses[u.Id].IsTrial ? "Trial" : "Paid") : "None";

                    return new UserStatistics
                    {
                        Id = u.Id,
                        UserName = u.UserEmail,
                        InBackup = u.BackupEnabled,
                        EmailLastBackupStatus = u.Email.LastBackupStatus,
                        EmailLastBackupDate = u.Email.LastBackupDate,
                        DriveLastBackupStatus = u.Drive.LastBackupStatus,
                        DriveLastBackupDate = u.Drive.LastBackupDate,
                        CalendarLastBackupStatus = u.Calendar.LastBackupStatus,
                        CalendarLastBackupDate = u.Calendar.LastBackupDate,
                        LicenseType = licenseType
                    };
                });

            return new OkObjectResult(new
            {
                TotalCount = totalCount,
                Data = usersData
            });
        }

        private bool InBackup(User user)
        {
            return user.BackupEnabled && user.State == UserState.InDomain;
        }

        private ILicenseService GetLicenseServiceAndConfigure()
        {
            using var result = _licenseServiceProvider.GetLicenseService();

            Configure(result.Settings);

            return result;
        }

        private void Configure(LicenseServiceSettings settings)
        {
            if (settings != null)
            {
                settings.TimeOut = 5000;
            }
            else
            {
                settings = new LicenseServiceSettings
                {
                    TimeOut = 5000
                };
            }
        }
    }
}
