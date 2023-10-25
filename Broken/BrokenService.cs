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
        private readonly object _syncLock = new object();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private static readonly ILog Log = LogManager.GetLogger(typeof(BrokenService));

        public BrokenService(UserDbContext db, ILicenseServiceProvider licenseServiceProvider)
        {
            _db = db;
            _licenseServiceProvider = licenseServiceProvider;
        }

        public async Task<IActionResult> GetReport(GetReportRequest request)
        {
            try
            {
                await _semaphore.WaitAsync();

                while (true)
                {
                    try
                    {
                        if (_counter > 2)
                            return new StatusCodeResult(500);

                        return await GetReportAsync(request);
                    }
                    catch(Exception ex)
                    {
                        Log.Debug($"Attempt {_counter} failed {ex.Message}");
                        _counter++;

                        Thread.Sleep(1000);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<IActionResult> GetReportAsync(GetReportRequest request)
        {
            var filteredUsers = _db.Users.Where(d => d.DomainId == request.DomainId).Where(b => InBackup(b)).OrderBy(o => o.UserEmail).Cast<User>();

            int totalCount = filteredUsers != null ? filteredUsers.Count() : 0;
            //filteredUsers = filteredUsers.Take(request.PageSize).Skip(request.PageSize * request.PageNumber); Error!
            filteredUsers = filteredUsers.Skip(request.PageSize * request.PageNumber).Take(request.PageSize);


            Dictionary<Guid, LicenseInfo> userLicenses = new Dictionary<Guid, LicenseInfo>();
            var licenseService = GetLicenseServiceAndConfigure();//error

            if (licenseService != null)
            {
                Log.Info($"Total licenses for domain '{request.DomainId}': {licenseService.GetLicensedUserCountAsync(request.DomainId)}");

                List<string> emails = filteredUsers.Select(u => u.UserEmail).ToList();
                ICollection<LicenseInfo> result = null;

                try
                {
                    result = await licenseService.GetLicensesAsync(request.DomainId, emails);// Error!
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

            //var usersData = await filteredUsers
            //    .Select(u => new
            //    {
            //        u.Id,
            //        u.UserEmail,
            //        u.BackupEnabled,
            //        EmailLastBackupStatus = u.Email.LastBackupStatus,
            //        EmailLastBackupDate = u.Email.LastBackupDate,
            //        DriveLastBackupStatus = u.Drive.LastBackupStatus,
            //        DriveLastBackupDate = u.Drive.LastBackupDate,
            //        CalendarLastBackupStatus = u.Calendar.LastBackupStatus,
            //        CalendarLastBackupDate = u.Calendar.LastBackupDate,
            //    })
            //    .ToListAsync();

            //var transformedUsersData = filteredUsers
            //    .Select(u =>
            //    {
            //        string licenseType = userLicenses.ContainsKey(u.Id) ? (userLicenses[u.Id].IsTrial ? "Trial" : "Paid") : "None";

            //        return new UserStatistics
            //        {
            //            Id = u.Id,
            //            UserName = u.UserEmail,
            //            InBackup = u.BackupEnabled,
            //            EmailLastBackupStatus = u.EmailLastBackupStatus,
            //            EmailLastBackupDate = u.EmailLastBackupDate,
            //            DriveLastBackupStatus = u.DriveLastBackupStatus,
            //            DriveLastBackupDate = u.DriveLastBackupDate,
            //            CalendarLastBackupStatus = u.CalendarLastBackupStatus,
            //            CalendarLastBackupDate = u.CalendarLastBackupDate,
            //            LicenseType = licenseType
            //        };
            //    });

            return new OkObjectResult(new
            {
                TotalCount = totalCount,
                Data = filteredUsers
            });
        }

        private bool InBackup(User user)
        {
            return user.BackupEnabled && user.State == UserState.InDomain;
        }

        private ILicenseService GetLicenseServiceAndConfigure()
        {
            var result = _licenseServiceProvider.GetLicenseService();//error

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
