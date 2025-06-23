namespace ParsingProject.Models;

public class Root
{
    public List<Movie> Docs { get; set; }
    public int Total { get; set; }
    public int Limit { get; set; }
    public int Page { get; set; }
    public int Pages { get; set; }
}