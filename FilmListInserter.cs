using Microsoft.Data.SqlClient;

namespace testluca
{
    public class FilmListInserter(SqlConnection connection, FilmManager filmManager, MovieApi movieApi)
    {
        private readonly SqlConnection _connection = connection;
        private readonly FilmManager FilmManager = filmManager;
        private readonly MovieApi _movieApi = movieApi;

        public async Task InsertFilmListFromFileAsync(string filePath)
        {
            var movieTitles = LoadMovieTitlesFromFile(filePath);

            foreach (var title in movieTitles)
            {
                try
                {
                    string? movieTitle = title;
                    string? year = null;

                    if (title.Contains(','))
                    {
                        var parts = title.Split(',', 2);
                        movieTitle = parts[0].Trim();
                        year = parts[1].Trim();

                        if (!int.TryParse(year, out _))
                        {
                            Console.WriteLine($"Ungültiges Jahr für '{title}'. Übersprungen.");
                            continue;
                        }
                    }

                    if (FilmManager.FilmExists(_connection, movieTitle, year))
                    {
                        Console.WriteLine($"Film '{movieTitle}' ({year ?? "kein Jahr"}) existiert bereits. Übersprungen.");
                        continue;
                    }

                    Movie? movie = await MovieApi.GetMovieDataAsync(movieTitle, year);

                    if (movie == null)
                    {
                        Console.WriteLine($"Film '{movieTitle}' ({year ?? "kein Jahr"}) nicht gefunden.");
                        continue;
                    }
                    //director verarbeiten
                    string? dirName = movie.Director;
                    if (string.IsNullOrWhiteSpace(dirName) || dirName == "N/A")
                        dirName = null;
                    else
                        dirName = FilmManager.GetFirstNameBeforeComma(dirName);

                    int dirId = FilmManager.InsertRegisseur(_connection, dirName ?? string.Empty);
                    //writer verarbeiten
                    string? authName = movie.Writer;
                    if (string.IsNullOrWhiteSpace(authName) || authName == "N/A")
                        authName = null;
                    else
                        authName = FilmManager.GetFirstNameBeforeComma(authName);

                    int authId = FilmManager.InsertAutor(_connection, authName ?? string.Empty);

                    // Film einfügen
                    int filmId = FilmManager.InsertFilm(_connection, movie, dirId, authId);

                    // Genre verarbeiten
                    if (!string.IsNullOrWhiteSpace(movie.Genre))
                    {
                        foreach (var g in movie.Genre.Split(',').Select(x => x.Trim()))
                        {
                            int genreId = FilmManager.InsertGenre(_connection, g);
                            FilmManager.InsertFilmGenre(_connection, filmId, genreId);
                        }
                    }

                    // rt bewertung einfügen
                    FilmManager.InsertBewertung(_connection, filmId, movie.RottenTomatoesRating, "Importierter Film", "Rotten Tomatoes");

                    Console.WriteLine($"Film '{movie.Title}' ({movie.Year}) wurde erfolgreich eingefügt!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler bei '{title}': {ex.Message}");
                }
            }
        }




        private static List<string> LoadMovieTitlesFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Datei '{filePath}' wurde nicht gefunden.");
                return [];
            }

            return [.. File.ReadAllLines(filePath)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim())];
        }
    }
}