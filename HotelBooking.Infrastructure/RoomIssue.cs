namespace HotelBooking.Infrastructure;

public class RoomIssue
{
    public int IssueId { get; set; }

    public int RoomId { get; set; }

    public string Description { get; set; } = null!;

    public string Status { get; set; } = "Open";

    public DateTime ReportedAt { get; set; }

    public virtual Room Room { get; set; } = null!;
}
