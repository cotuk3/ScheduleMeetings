# Meeting Scheduling Service

This library provides a way to locate an available time slot for one or more users’ meetings within a specified window. It respects business hours, existing reservations, desired meeting duration and has break between every meeting for 15 minutes.

---

## Setup Instructions

1. Install prerequisites  
   
   - .NET SDK 6.0 or later  
   - Optional: Visual Studio 2022 (or any IDE with .NET support)

2. Clone the repository  
   
   ```bash
   git clone https://github.com/your-org/meeting-scheduler.git
   cd meeting-scheduler

3. Restore NuGet packages
   ```bash
   dotnet restore

4. Build the solution
   ```bash
   dotnet build
   
5. Run unit tests
   ```bash
   dotnet test

## Known Limitations & Edge Cases Not Handled
- Business hours are currently hard-coded to 09:00–17:00 UTC time. No configuration for alternate hours.
- Timezone support is not implemented. All DateTime values use UTC.
- Concurrent booking conflicts aren’t handled; race conditions may occur if multiple clients schedule simultaneously.
- No capacity to split a single request across non-contiguous free slots (e.g., two 30-minute blocks instead of one 60-minute block).
