namespace Regen.Models.ViewModels.Admin;

public class HttpStatusChartViewModel
{
    public string StatusCode { get; set; } = string.Empty;

    public List<int> Data { get; set; } = new();
}