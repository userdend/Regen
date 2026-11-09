namespace Regen.Models.Requests;

public class CreateScheduleRequest
{
    public int IntervalMilliseconds { get; set; }

    public List<string> Entries { get; set; } = new();
}