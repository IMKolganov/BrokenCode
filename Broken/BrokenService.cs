using BrokenCode.Etc;
using BrokenCode.Interfaces;
using BrokenCode.Model;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrokenCode;

public class BrokenService
{
    private readonly UserDbContext _db;
    private readonly ILicenseServiceProvider _licenseServiceProvider;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private static readonly ILog Log = LogManager.GetLogger(typeof(BrokenService));

    public BrokenService(UserDbContext db, ILicenseServiceProvider licenseServiceProvider)
    {
        _db = db;
        _licenseServiceProvider = licenseServiceProvider;
    }

    public async Task<IActionResult> GetReport(GetReportRequest request)
    {
        int maxCounter = 10;
        await _semaphore.WaitAsync();

        for (int attempt = 0; attempt < maxCounter; attempt++)
        {
            try
            {
                return await GetReportAsync(request);
            }
            catch (Exception ex)
            {
                Log.Debug($"Attempt {attempt + 1} failed: {ex.Message}");

                if (attempt >= maxCounter - 1)
                {
                    return new StatusCodeResult(500); // Возвращаем ошибку 500 после последней попытки
                }

                await Task.Delay(1000);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return new StatusCodeResult(500); // если все 10 раз неудача
    }


    private async Task<IActionResult> GetReportAsync(GetReportRequest request)
    {
        var filteredUsers = PreparePageUsersByDomainId(request.DomainId, request.PageSize, request.PageNumber);

        var userLicenses = await GetLicenceTypeAsync(request.DomainId, filteredUsers);

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
            TotalCount = usersData.Count(),
            Data = usersData
        });
    }

    private IQueryable<User> PreparePageUsersByDomainId(Guid domainId, int pageSize, int pageNumber)
    {
        var filteredUsers = _db.Users
                               .Where(d => d.DomainId == domainId)
                               .Where(b => InBackup(b))
                               .OrderBy(o => o.UserEmail)
                               .Skip(pageSize * pageNumber)
                               .Take(pageSize);

        return filteredUsers;
    }

    private async Task<Dictionary<Guid, LicenseInfo>> GetLicenceTypeAsync(Guid domainId, IQueryable<User> filteredUsers)
    {
        Dictionary<Guid, LicenseInfo> userLicenses = new Dictionary<Guid, LicenseInfo>();
        var licenseService = GetLicenseServiceAndConfigure();

        if (licenseService != null)
        {
            Log.Info($"Total licenses for domain '{domainId}': {await licenseService.GetLicensedUserCountAsync(domainId)}");

            List<string> emails = await filteredUsers.Select(u => u.UserEmail).ToListAsync();
            ICollection<LicenseInfo> result = null;

            try
            {
                result = await licenseService.GetLicensesAsync(domainId, emails);
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
        return userLicenses;
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