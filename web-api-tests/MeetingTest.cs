using Microsoft.AspNetCore.Mvc;
using Moq;
using ScheduleMeetings.Controllers;
using ScheduleMeetings.Core.Interfaces;
using ScheduleMeetings.Core.Services;
using ScheduleMeetings.Data;
using ScheduleMeetings.Dtos;
using ScheduleMeetings.Entities;

namespace web_api_tests;

public class MeetingTest
{

    private readonly Mock<IUserRepository> _mockUserRep;
    private readonly Mock<IMeetingRepository> _mockMeetRep;
    private readonly MeetingService _mockSvc;

    private readonly MettingsController _controller;
    private readonly List<User> _usersStore;

    private readonly DateTime _today9AM;
    private readonly User user = new User(3, "Test User");
    public MeetingTest()
    {
        _mockMeetRep = new Mock<IMeetingRepository>();
        _mockUserRep = new Mock<IUserRepository>();
        _mockSvc = new MeetingService(_mockMeetRep.Object);
        _usersStore = new List<User>();

        _mockUserRep
            .Setup(r => r.Users)
            .Returns(_usersStore.AsQueryable().ToList());

        // Додавання юзера записує його в колекцію
        _mockUserRep
            .Setup(r => r.Add(It.IsAny<User>()))
            .Callback<User>(u => _usersStore.Add(u));

        _controller = new MettingsController(_mockUserRep.Object, _mockMeetRep.Object, _mockSvc);

        _today9AM = DateTime.UtcNow.Date.AddHours(9);
    }

    [Fact]
    public void Schedule_NoConflict_ReturnsRequestedSlot()
    {
        // Arrange
        var dto = new MeetingFindDto([1, 2], 60, new TimeSlot(_today9AM.AddHours(1), _today9AM.AddHours(2)));

        // No existing meetings for any user
        _mockMeetRep.Setup(s => s.GetAllMeetingsByUser(It.IsAny<int>()))
                .Returns(Enumerable.Empty<Meeting>());

        // Act
        var result = _controller.ScheduleMeetingByUsers(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ts = Assert.IsType<TimeSlot>(ok.Value);
        Assert.Equal(dto.TimeSlot.StartTime, ts.StartTime);
        Assert.Equal(dto.TimeSlot.StartTime.AddMinutes(dto.Duration), ts.EndTime);
    }

    [Fact]
    public void Schedule_PartialOverlap_ShiftsByEndPlusBrake()
    {
        // Arrange
        var existingSlot = new TimeSlot(
            _today9AM.AddHours(2),   // 11:00
            _today9AM.AddHours(3)    // 12:00
        );

        // Existing meeting for this user that overlaps with requested slot
        var meetingExists = new Meeting(2, new List<User> { user }, existingSlot);

        _mockMeetRep.Setup(s => s.GetAllMeetingsByUser(user.Id))
            .Returns(new[] { meetingExists });

        // Requested slot that partially overlaps with existing one
        var desiredStart = _today9AM.AddHours(2).AddMinutes(30);// 11:30
        var desiredEnd = desiredStart.AddHours(1).AddMinutes(30);// 13:00
        var dto = new MeetingFindDto
        (
            [user.Id], 30,
            new TimeSlot(
                desiredStart,
                desiredEnd)
        );

        // Act
        var result = _controller.ScheduleMeetingByUsers(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ts = Assert.IsType<TimeSlot>(ok.Value);

        // Brake time is 15 minutes, so shift to 12:15
        var expectedStart = _today9AM.AddHours(3).AddMinutes(15);
        var expectedEnd = expectedStart.AddMinutes(dto.Duration);

        Assert.Equal(expectedStart, ts.StartTime);
        Assert.Equal(expectedEnd, ts.EndTime);
    }

    [Fact]
    public void Schedule_ExactlyInBrakeRange()
    {
        // Arrange
        // Existing meeting: 10:00–11:00
        var existingSlot = new TimeSlot(
            _today9AM.AddHours(1),   // 10:00
            _today9AM.AddHours(2)    // 11:00
        );
        var meetingExists = new Meeting(3, [user], existingSlot);
        _mockMeetRep.Setup(s => s.GetAllMeetingsByUser(user.Id))
                .Returns([meetingExists]);

        // Request exactly at 11:15 (which is 15 min buffer after 11:00)
        var desiredStart = _today9AM.AddHours(2).AddMinutes(15);
        var desiredEnd = desiredStart.AddHours(1);
        var dto = new MeetingFindDto
        (
            [user.Id], 60,
            new TimeSlot(
                desiredStart,
                desiredEnd
        )
        );

        // Act
        var result = _controller.ScheduleMeetingByUsers(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ts = Assert.IsType<TimeSlot>(ok.Value);

        Assert.Equal(desiredStart, ts.StartTime);
        Assert.Equal(desiredEnd, ts.EndTime);
    }

    [Fact]
    public void Schedule_StartOutOfRangeAndInvalidDate_ReturnsBadRequest()
    {
        // Arrange
        // Request in the past AND outside business hours
        var yesterday = DateTime.Today.AddDays(-1).AddHours(8);
        var dto = new MeetingFindDto
        (
            [1, 2], 30,
            new TimeSlot(
                yesterday,
                yesterday.AddMinutes(30))
        );

        // Act
        var result = _controller.ScheduleMeetingByUsers(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void Schedule_FilledAllDay_ReturnsBadRequest()
    {
        // Edge case: user has back-to-back meetings from 9–17 with no gaps
        var busy = new List<Meeting>();
        var cursor = _today9AM;
        while(cursor < _today9AM.AddHours(8))
        {
            var end = cursor.AddHours(1);
            busy.Add(new Meeting(1, [user], new TimeSlot(cursor, end)));
            cursor = end; // no break inserted
        }

        _mockMeetRep.Setup(s => s.GetAllMeetingsByUser(user.Id))
                .Returns(busy);

        // Try to schedule 30-min meeting at 9:00
        var dto = new MeetingFindDto
        (
            [user.Id], 30, new TimeSlot(
                _today9AM,
                _today9AM.AddHours(1))
        );

        var result = _controller.ScheduleMeetingByUsers(dto);

        // Since all slots are fully booked, next possible start = last end + 15min
        // which is 17:00 + 15 = 17:15 → end at 17:45, which is out of range and returns BadRequest
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void Schedule_OutsideRange_ReturnsClosestMeetingInRange()
    {
        // Arrange
        // User requests meeting at 8:00, which is before business hours
        var dto = new MeetingFindDto
        (
            [user.Id], 60, new TimeSlot(
                _today9AM.AddHours(-1),
                _today9AM)
        );

        // No existing meetings for any user
        _mockMeetRep.Setup(s => s.GetAllMeetingsByUser(It.IsAny<int>()))
            .Returns(Enumerable.Empty<Meeting>());


        // Act
        var result = _controller.ScheduleMeetingByUsers(dto);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ts = Assert.IsType<TimeSlot>(ok.Value);

        // Assert
        // Since 8:00 is out of range, it should return the first available slot at 9:00 -10:00
        var expectedStart = _today9AM;
        var expectedEnd = _today9AM.AddMinutes(dto.Duration);

        Assert.Equal(expectedStart, ts.StartTime);
        Assert.Equal(expectedEnd, ts.EndTime);
    }

    [Fact]
    public void FreeTimeSlotForMeeting_WindowSpansTwoDays_NoMeetingsOnFirstDay_SchedulesAtNextDayStart()
    {
        // Arrange
        // Range from today 16:30 to 12:00 of next day
        var windowStart = _today9AM.AddHours(7).AddMinutes(30);
        // Window spans until tomorrow 12:00
        var windowEnd = DateTime.Today.AddDays(1).AddHours(12);
        var duration = 60;

        var dto = new MeetingFindDto([user.Id], duration, new TimeSlot(windowStart, windowEnd));

        // No meetings for user on either day
        _mockMeetRep.Setup(r => r.GetAllMeetingsByUser(user.Id))
            .Returns(Array.Empty<Meeting>());

        // Act
        var result = _controller.ScheduleMeetingByUsers(dto);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ts = Assert.IsType<TimeSlot>(ok.Value);

        // Assert
        // Should schedule at next business day 9:00
        var expectedStart = DateTime.Today
            .AddDays(1)
            .AddHours(9);

        Assert.Equal(expectedStart, ts.StartTime);
        Assert.Equal(expectedStart.AddMinutes(duration), ts.EndTime);
    }

    // User tests
    [Fact]
    public void CreateUser_NullDto_ReturnsBadRequest()
    {
        // Act
        var result = _controller.CreateUser(null);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid user data.", bad.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateUser_EmptyOrWhitespaceName_ReturnsBadRequest(string badName)
    {
        // Arrange
        var dto = new UserAddDto(badName);

        // Act
        var result = _controller.CreateUser(dto);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid user data.", bad.Value);
    }

    [Fact]
    public void CreateUser_ValidDto_ReturnsOkWithUserAndAddsToRepo()
    {
        // Arrange
        var dto = new UserAddDto("User");

        // Act
        var result = _controller.CreateUser(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<User>(ok.Value);

        Assert.Equal(1, created.Id);
        Assert.Equal("User", created.Name);
        Assert.Single(_usersStore);
        Assert.Equal(created, _usersStore[0]);
    }

    [Fact]
    public void GetMeetingsByUser_NoMeetings_ReturnsNotFound()
    {
        // Arrange
        int userId = 42;
        _mockMeetRep
            .Setup(r => r.GetAllMeetingsByUser(userId))
            .Returns(new List<Meeting>());

        // Act
        var result = _controller.GetMeetingsByUser(userId);

        // Assert
        var nf = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"No meetings found for user with ID {userId}.", nf.Value);
    }

    [Fact]
    public void GetMeetingsByUser_WithMeetings_ReturnsOkWithList()
    {
        // Arrange
        int userId = 7;
        var slot = new TimeSlot(_today9AM, _today9AM.AddHours(1));
        var meeting = new Meeting(1, new List<User> { new User(userId, "Bob") }, slot);

        _mockMeetRep
            .Setup(r => r.GetAllMeetingsByUser(userId))
            .Returns(new List<Meeting> { meeting });

        // Act
        var result = _controller.GetMeetingsByUser(userId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<Meeting>>(ok.Value);

        Assert.Single(returned);
        Assert.Equal(meeting, returned[0]);
    }

}