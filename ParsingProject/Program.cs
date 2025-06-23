using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Npgsql;
using ParsingProject.Models;

class Program

{
    private static readonly string ApiKey = "Sshhhh_it_is_secret...";
    private static readonly string ConnectionString = "Host=localhost;Port=5439;Username=postgres;Database=MovieDB";

    static async Task Main(string[] args)
    {
        List<Movie> movies = await GetMoviesAsync(1, 3);
        
        Console.WriteLine("Конец");
        foreach (var movie in movies)
        {
            Console.WriteLine(movie.Rating.GetAnyRating());
            // await InsertMovieAsync(movie);
        }
        
    }

    public static async Task<List<Movie>> GetMoviesAsync(int page, int limit)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.kinopoisk.dev");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("X-API-KEY", ApiKey);


        var query = new Dictionary<string, string>
        {
            ["page"] = page.ToString(),
            ["limit"] = limit.ToString()
        };

        var queryString = new FormUrlEncodedContent(query).ReadAsStringAsync().Result;
        
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"/v1.4/movie?{queryString}");

        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        string responseBody = await response.Content.ReadAsStringAsync();
        
        Root root = JsonConvert.DeserializeObject<Root>(responseBody);

        var movies = new List<Movie>();
        foreach (var movie in root.Docs.ToArray())
        {
            movies.Add(movie);
        }
        return movies;
    }
    
    public static async Task InsertMovieAsync(Movie movie)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // Вставка рейтинга
            var insertRatingCmd = new NpgsqlCommand(@"
                INSERT INTO rating (kp, imdb, film_critics, russian_film_critics, await)
                VALUES (@kp, @imdb, @film_critics, @russian_film_critics, @await)
                RETURNING id; ", conn, transaction);

            insertRatingCmd.Parameters.AddWithValue("kp", movie.Rating.Kp);
            insertRatingCmd.Parameters.AddWithValue("imdb", movie.Rating.Imdb);
            insertRatingCmd.Parameters.AddWithValue("film_critics", movie.Rating.FilmCritics);
            insertRatingCmd.Parameters.AddWithValue("russian_film_critics", movie.Rating.RussianFilmCritics);
            insertRatingCmd.Parameters.AddWithValue("await", movie.Rating.Await);

            int ratingId = (int)(await insertRatingCmd.ExecuteScalarAsync())!;

            // Вставка фильма
            var insertMovieCmd = new NpgsqlCommand(@"
                INSERT INTO movie (name, alter_name, year, rating_id)
                VALUES (@name, @alter_name, @year, @rating_id); ", conn, transaction);

            insertMovieCmd.Parameters.AddWithValue("name", movie.Name ?? "__У фильма пропущено название__");
            insertMovieCmd.Parameters.AddWithValue("alter_name", movie.AlterName ?? (object)DBNull.Value);
            insertMovieCmd.Parameters.AddWithValue("year", movie.Year ?? (object)DBNull.Value);
            insertMovieCmd.Parameters.AddWithValue("rating_id", ratingId);

            await insertMovieCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Ошибка при вставке фильма: {ex.Message}");
        }
    }
}
