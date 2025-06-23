namespace ParsingProject.Models;

public class Rating
{
    public double Kp { get; set; }
    public double Imdb { get; set; }
    public double FilmCritics { get; set; }
    public double RussianFilmCritics { get; set; }
    public double Await { get; set; }

    public double GetAnyRating()
    {
        var fields = new double[] { Kp, Imdb, FilmCritics, RussianFilmCritics, Await };
        return fields.FirstOrDefault(f => f != 0);
    }
}