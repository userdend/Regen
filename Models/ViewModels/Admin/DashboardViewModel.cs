namespace Regen.Models.ViewModels.Admin;

public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsersToday { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int DeletedUsers { get; set; }
    public int TotalBatches { get; set; }
    public int SuccessfulEntries { get; set; }
    public int PendingEntries { get; set; }
    public int FailedEntries { get; set; }
    public List<HttpStatusChartViewModel> HttpStatusChart { get; set; } = new();
}