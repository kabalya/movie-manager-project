using Newtonsoft.Json;

namespace ParsingProject.Models;

public class Movie
{
    public int Id { get; set; }
    public string Name { get; set; }
    [JsonProperty("alternativeName")]
    public string AlterName { get; set; }
    public int? Year { get; set; }
    public Rating Rating { get; set; }
}