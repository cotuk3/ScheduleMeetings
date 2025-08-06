using Microsoft.AspNetCore.Mvc;
using ScheduleMeetings.Core.Interfaces;
using ScheduleMeetings.Data;
using ScheduleMeetings.Dtos;
using ScheduleMeetings.Entities;

namespace ScheduleMeetings.Controllers;

[ApiController]
[Route("api")]
public class MettingsController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IMeetingRepository _meetingsRep;
    private readonly IMeetingService _meetingsServ;

    public MettingsController(IUserRepository userRep, IMeetingRepository meetingsRep, IMeetingService service)
    {
        _users = userRep;
        _meetingsRep = meetingsRep;
        _meetingsServ = service;
    }


    [HttpPost("users")]
    public ActionResult<User> CreateUser([FromBody] UserAddDto user)
    {
        if(user == null || string.IsNullOrWhiteSpace(user.Name))
        {
            return BadRequest("Invalid user data.");
        }
        User newUser = new User(_users.Users.Count + 1, user.Name);

        _users.Add(newUser);
        return Ok(newUser);
    }

    [HttpGet("users/{userId}/meetings")]
    public ActionResult<List<Meeting>> GetMeetingsByUser(int userId)
    {
        var userMeetings = _meetingsRep.GetAllMeetingsByUser(userId);
        if(!userMeetings.Any())
        {
            return NotFound($"No meetings found for user with ID {userId}.");
        }
        return Ok(userMeetings);
    }

    [HttpPost("meetings")]
    public ActionResult<TimeSlot> ScheduleMeetingByUsers([FromBody] MeetingFindDto meetingDetails)
    {
        try
        {
            TimeSlot timeSlot = _meetingsServ.FreeTimeSlotForMeeting(meetingDetails);
            return Ok(timeSlot);
        }
        catch(Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
