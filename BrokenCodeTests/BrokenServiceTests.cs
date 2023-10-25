using BrokenCode.Etc;
using BrokenCode.Interfaces;
using BrokenCode;
using Microsoft.AspNetCore.Mvc;
using Moq;
using BrokenCode.Model;
using Moq.EntityFrameworkCore;

namespace BrokenCodeTests;
public class BrokenServiceTests
{
    private Mock<UserDbContext> _userDbContext;
    private Guid DomainId = new Guid("550e8400-e29b-41d4-a716-446655440000");


    [Fact]
    public async Task GetReport_ReturnsOkObjectResult_WhenCalledWithValidData()
    {
        var licenseServiceMock = new Mock<ILicenseService>();

        licenseServiceMock.Setup(ls => ls.GetLicensesAsync(It.IsAny<Guid>(), It.IsAny<ICollection<string>>()))
                          .ReturnsAsync(new List<LicenseInfo>
                          {
                          new LicenseInfo
                              {
                                   Email = "test1@example.com",
                                   IsTrial = false,
                                   UserId = Guid.NewGuid()
                              }
                          }); ;

        licenseServiceMock.Setup(ls => ls.GetLicensedUserCountAsync(It.IsAny<Guid>())).ReturnsAsync(5);
        InitMockUserDbContext();

        var licenseServiceProviderMock = new Mock<ILicenseServiceProvider>();
        licenseServiceProviderMock
            .Setup(lp => lp.GetLicenseService())
            .Returns(licenseServiceMock.Object);

        var service = new BrokenService(_userDbContext.Object, licenseServiceProviderMock.Object);

        var request = new GetReportRequest
        {
            DomainId = DomainId,
            PageNumber = 0,
            PageSize = 10000
        };

        var result = await service.GetReport(request);

        Assert.IsType<OkObjectResult>(result);
    }

    private void InitMockUserDbContext()
    {
        var userList = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                DomainId = DomainId,
                UserEmail = "test1@example.com",
                UserName = "Test User 1",
                BackupEnabled = true,
                State = UserState.InDomain,
                                Email = new Email(),
                Drive = new Drive(), 
                Calendar = new Calendar()
            },
            new User
            {
                Id = Guid.NewGuid(),
                DomainId = DomainId,
                UserEmail = "test2@example.com",
                UserName = "Test User 2",
                BackupEnabled = true,
                State = UserState.InDomain,
                Email = new Email(),
                Drive = new Drive(),
                Calendar = new Calendar()
            },
        };

        _userDbContext = new Mock<UserDbContext>();
        _userDbContext.Setup(x => x.Users).ReturnsDbSet(userList);
    }
}
