using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Regen.Models.Requests;

public class GenerateSyntheticRequest
{
    [Required]
    public Dictionary<string, JsonElement> Questions { get; set; } = new();

    public string Prompt { get; set; } = "";

    [Range(1, 100)]
    public int Count { get; set; }
}
