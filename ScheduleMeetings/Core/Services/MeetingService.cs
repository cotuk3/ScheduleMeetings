using ScheduleMeetings.Core.Interfaces;
using ScheduleMeetings.Dtos;
using ScheduleMeetings.Entities;

namespace ScheduleMeetings.Core.Services;

public class MeetingService : IMeetingService
{
    private readonly IMeetingRepository _meetings;

    public MeetingService(IMeetingRepository meetings)
    {
        _meetings = meetings;
    }

    public TimeSlot FreeTimeSlotForMeeting(MeetingFindDto meetingDetails)
    {
        DateTime start = meetingDetails.TimeSlot.StartTime;
        TimeSlot timeSlot;

        // Pre‐validate the desired window and inputs
        if(meetingDetails == null)
            throw new ArgumentNullException(nameof(meetingDetails));

        if(meetingDetails.Ids == null || !meetingDetails.Ids.Any())
            throw new ArgumentException("At least one user must be specified.", nameof(meetingDetails.Ids));

        while(true)
        {
            timeSlot = new TimeSlot(start, meetingDetails.TimeSlot.EndTime);
            if(!IsDateValid(timeSlot, meetingDetails.Duration))
                throw new Exception("No available meeting for this time!");

            bool isFree = true;
            foreach(int i in meetingDetails.Ids)
            {
                isFree = IsNoMeeting(i, timeSlot, meetingDetails.Duration, out DateTime newStart);
                start = newStart;
                if(!isFree)
                {
                    break;
                }
            }
            if(isFree)
            {
                break;
            }
        }

        timeSlot = new(start, start.AddMinutes(meetingDetails.Duration));

        if(IsInRange(timeSlot))
            return timeSlot;
        else
            throw new Exception("No available meeting for this time!");
    }

    private bool IsNoMeeting(int id, TimeSlot timeSlot, int duration, out DateTime newStart)
    {
        DateTime newStartTime = NormalizeToBusinessHours(timeSlot.StartTime, new TimeSpan(0, duration, 0));
        DateTime newEndTime = timeSlot.EndTime;

        var allMeetings = _meetings.GetAllMeetingsByUser(id);

        var breakTime = new TimeSpan(0, 15, 0);
        DateTime windowStart = newStartTime - breakTime;
        DateTime windowEnd = newStartTime.AddMinutes(duration) + breakTime;

        var meetingsOverlap = allMeetings
            .Where(m =>
                m.timeSlot.StartTime < windowEnd      // start of other meeting sooner than end of This for 15 min
                && m.timeSlot.EndTime > windowStart // enf of other meeing later than start of This for 15 min
            );

        if(!meetingsOverlap.Any())
        {
            newStart = newStartTime;
            return true;
        }

        newStart = meetingsOverlap.Last().timeSlot.EndTime.Add(breakTime);
        return false;
    }

    private bool IsDateValid(TimeSlot timeSlot, int duration)
    {
        return timeSlot.StartTime < timeSlot.EndTime && timeSlot.StartTime >= DateTime.UtcNow.Date
            && timeSlot.EndTime - timeSlot.StartTime >= new TimeSpan(0, duration, 0);
    }

    private bool IsInRange(TimeSlot timeSlot)
    {
        DateTime startDate = timeSlot.StartTime.Date;
        DateTime endDate = timeSlot.EndTime.Date;

        return timeSlot.StartTime >= startDate.AddHours(9) && timeSlot.EndTime <= endDate.AddHours(17);
    }

    private DateTime NormalizeToBusinessHours(DateTime cursor, TimeSpan meetingDuration)
    {
        // business hours are from 9:00 to 17:00
        var workDayStart = TimeSpan.FromHours(9);
        var workDayEnd = TimeSpan.FromHours(17);

        // If current day is a weekend, move to next Monday at 9:00
        while(cursor.DayOfWeek == DayOfWeek.Saturday || cursor.DayOfWeek == DayOfWeek.Sunday)
        {
            cursor = cursor.Date.AddDays(1).Add(workDayStart);
        }

        var timeOfDay = cursor.TimeOfDay;
        var day = cursor.Date;

        // if the cursor is before business hours, set it to 9:00 of the same day
        if(timeOfDay < workDayStart)
            return day.Add(workDayStart);

        // If the cursor is after business hours, set it to 9:00 of the next business day
        if(timeOfDay + meetingDuration > workDayEnd)
        {
            // Next day
            var next = day.AddDays(1);
            // Skip weekends
            while(next.DayOfWeek == DayOfWeek.Saturday || next.DayOfWeek == DayOfWeek.Sunday)
            {
                next = next.AddDays(1);
            }
            return next.Add(workDayStart);
        }
        return cursor;
    }
}
