using BrokenCode.Etc;
using BrokenCode.Interfaces;
using BrokenCode;
using Microsoft.AspNetCore.Mvc;
using Moq;
using BrokenCode.Model;
using Microsoft.EntityFrameworkCore;
using BrokenCodeTests.EFAsync;

namespace BrokenCodeTests;
public class BrokenServiceTests
{
    private Mock<UserDbContext> _userDbContext;
    private Guid DomainId = new Guid("550e8400-e29b-41d4-a716-446655440000");


    [Fact]
    public async Task GetReport_ReturnsOkObjectResult_WhenCalledWithValidData()
    {
        _userDbContext = new Mock<UserDbContext>();
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

        var queryableUserList = userList.AsQueryable();

        var dbSetMock = new Mock<DbSet<User>>();

        dbSetMock.As<IAsyncEnumerable<User>>()
            .Setup(m => m.GetAsyncEnumerator(default))
            .Returns(new TestAsyncEnumerator<User>(queryableUserList.GetEnumerator()));

        dbSetMock.As<IQueryable<User>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<User>(queryableUserList.Provider));

        dbSetMock.As<IQueryable<User>>().Setup(m => m.Expression).Returns(queryableUserList.Expression);
        dbSetMock.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(queryableUserList.ElementType);
        dbSetMock.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(queryableUserList.GetEnumerator());

        _userDbContext.Setup(db => db.Users).Returns(dbSetMock.Object);
    }
}
