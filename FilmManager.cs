using Microsoft.Data.SqlClient;

namespace testluca
{
    public class FilmManager
    {
        private readonly MovieApi movieApi = new();

        public static async Task AddFilmAsync(SqlConnection connection)
        {
            Console.Clear();
            Console.WriteLine("=== Film hinzufügen ===");
            Console.WriteLine();
            Console.Write("Filmtitel: ");
            string? movieTitle = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(movieTitle))
            {
                Console.WriteLine("Ungültiger Filmtitel.");
                return;
            }

            Console.Write("Erscheinungsjahr (optional, drücke Enter, um es zu überspringen): ");
            string? year = Console.ReadLine();

            if (FilmExists(connection, movieTitle, year))
            {
                Console.WriteLine($"Film '{movieTitle}' existiert bereits in der Datenbank.");
            }
            else
            {
                Movie? movie = await MovieApi.GetMovieDataAsync(movieTitle, string.IsNullOrWhiteSpace(year) ? null : year);
                if (movie == null)
                {
                    Console.WriteLine("Film nicht gefunden.");
                }
                else
                {
                    await InsertMovieIntoDatabase(connection, movie);
                    Console.WriteLine($"Film '{movie.Title}' wurde erfolgreich hinzugefügt!");
                }
            }
        }


        public static void DeleteFilm(SqlConnection connection)
        {
            Console.Clear();
            Console.WriteLine("=== Film löschen ===");
            Console.WriteLine();
            Console.Write("Filmtitel (oder Teil des Titels) zum Suchen: ");
            string? searchTitle = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(searchTitle))
            {
                Console.WriteLine("Ungültiger Suchbegriff.");
                return;
            }

            const string searchQuery = @"
                SELECT TOP 10 film_id, titel, jahr 
                FROM Filme 
                WHERE titel LIKE '%' + @searchTitle + '%'
                ORDER BY titel";
            using var searchCmd = new SqlCommand(searchQuery, connection);
            searchCmd.Parameters.AddWithValue("@searchTitle", searchTitle);

            using var reader = searchCmd.ExecuteReader();
            var films = new List<(int FilmId, string Title, string? Year)>();

            Console.WriteLine("\nGefundene Filme:");
            Console.WriteLine($"{"FilmId",-10}{"Titel",-10}{"Jahr",-10}");
            Console.WriteLine(new string('-', 70));

            while (reader.Read())
            {
                int filmId = reader.GetInt32(0);
                string title = reader.GetString(1);
                string? year = reader.IsDBNull(2) ? "Unbekannt" : reader.GetString(2);

                films.Add((filmId, title, year));
                Console.WriteLine($"{filmId,-10}{title,-50}{year,-10}");
            }

            reader.Close();

            if (films.Count == 0)
            {
                Console.WriteLine("Keine Filme gefunden.");
                return;
            }

            Console.Write("\nGib die FilmId des zu löschenden Films ein: ");
            if (!int.TryParse(Console.ReadLine(), out int selectedFilmId) || !films.Any(f => f.FilmId == selectedFilmId))
            {
                Console.WriteLine("Ungültige FilmId.");
                return;
            }

            void Exec(string sql)
            {
                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", selectedFilmId);
                cmd.ExecuteNonQuery();
            }

            Exec("DELETE FROM Film_Genre   WHERE film_id = @id");
            Exec("DELETE FROM Bewertung     WHERE film_id = @id");
            Exec("DELETE FROM Liste_Film    WHERE film_id = @id");
            Exec("DELETE FROM Filme         WHERE film_id = @id");

            Console.WriteLine($"Film mit der FilmId '{selectedFilmId}' und alle zugehörigen Einträge wurden gelöscht.");
        }



        public static bool FilmExists(SqlConnection connection, string titel, string? jahr)
        {
            const string query = "SELECT COUNT(*) FROM Filme WHERE titel = @titel AND (@jahr IS NULL OR jahr = @jahr)";
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@titel", titel);
            cmd.Parameters.AddWithValue("@jahr", string.IsNullOrEmpty(jahr) ? DBNull.Value : jahr);
            int count = (int)cmd.ExecuteScalar();
            return count > 0;
        }


        public static async Task InsertMovieIntoDatabase(SqlConnection connection, Movie movie)
        {
            string? dirName = movie.Director;
            if (string.IsNullOrWhiteSpace(dirName) || dirName == "N/A")
                dirName = null;
            else
                dirName = GetFirstNameBeforeComma(dirName);

            int dirId = await Task.Run(() => InsertRegisseur(connection, dirName ?? string.Empty));

            string? authName = movie.Writer;
            if (string.IsNullOrWhiteSpace(authName) || authName == "N/A")
                authName = null;
            else
                authName = GetFirstNameBeforeComma(authName);

            int authId = await Task.Run(() => InsertAutor(connection, authName ?? string.Empty)); 

            int filmId = await Task.Run(() => InsertFilm(connection, movie, dirId, authId));

            if (!string.IsNullOrWhiteSpace(movie.Genre))
            {
                foreach (var g in movie.Genre.Split(',').Select(x => x.Trim()))
                {
                    int genreId = await Task.Run(() => InsertGenre(connection, g));
                    await Task.Run(() => InsertFilmGenre(connection, filmId, genreId));
                }
            }

            Console.Write("Kommentar: ");
            string? kommentar = Console.ReadLine();
            await Task.Run(() => InsertBewertung(connection, filmId, movie.RottenTomatoesRating, kommentar ?? string.Empty, "Rotten Tomatoes"));
        }


        public static string? GetFirstNameBeforeComma(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return null;

            int commaIndex = fullName.IndexOf(',');
            if (commaIndex >= 0)
            {
                return fullName[..commaIndex].Trim();
            }
            return fullName.Trim();
        }

        public static int InsertRegisseur(SqlConnection connection, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            const string chk = "SELECT regisseur_id FROM Regisseur WHERE name = @name";
            using (var c = new SqlCommand(chk, connection))
            {
                c.Parameters.AddWithValue("@name", name);
                var exists = c.ExecuteScalar();
                if (exists != null) return Convert.ToInt32(exists);
            }

            const string ins = @"
                INSERT INTO Regisseur (name)
                VALUES (@name);
                SELECT SCOPE_IDENTITY();";
            using (var c = new SqlCommand(ins, connection))
            {
                c.Parameters.AddWithValue("@name", name);
                return Convert.ToInt32(c.ExecuteScalar());
            }
        }

        public static int InsertAutor(SqlConnection connection, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            const string chk = "SELECT autor_id FROM Autor WHERE name = @name";
            using (var c = new SqlCommand(chk, connection))
            {
                c.Parameters.AddWithValue("@name", name);
                var exists = c.ExecuteScalar();
                if (exists != null) return Convert.ToInt32(exists);
            }

            const string ins = @"
                INSERT INTO Autor (name)
                VALUES (@name);
                SELECT SCOPE_IDENTITY();";
            using (var c = new SqlCommand(ins, connection))
            {
                c.Parameters.AddWithValue("@name", name);
                return Convert.ToInt32(c.ExecuteScalar());
            }
        }

        public static int InsertFilm(SqlConnection connection, Movie movie, int dirId, int authId)
        {
            const string ins = @"
                INSERT INTO Filme (titel, jahr, regisseur_id, autor_id)
                VALUES (@titel, @jahr, @regisseur_id, @autor_id);
                SELECT SCOPE_IDENTITY();";
            using var c = new SqlCommand(ins, connection);
            c.Parameters.AddWithValue("@titel", movie.Title);
            c.Parameters.AddWithValue("@jahr", movie.Year != null && movie.Year.Length > 4 ? movie.Year[..4] : movie.Year ?? "");
            c.Parameters.AddWithValue("@regisseur_id", dirId > 0 ? (object)dirId : DBNull.Value);
            c.Parameters.AddWithValue("@autor_id", authId > 0 ? (object)authId : DBNull.Value);
            return Convert.ToInt32(c.ExecuteScalar());
        }

        public static int InsertGenre(SqlConnection connection, string genre)
        {
            const string chk = "SELECT genre_id FROM Genre WHERE name = @name";
            using (var c = new SqlCommand(chk, connection))
            {
                c.Parameters.AddWithValue("@name", genre);
                var exists = c.ExecuteScalar();
                if (exists != null) return Convert.ToInt32(exists);
            }

            const string ins = @"
                INSERT INTO Genre (name)
                VALUES (@name);
                SELECT SCOPE_IDENTITY();";
            using (var c = new SqlCommand(ins, connection))
            {
                c.Parameters.AddWithValue("@name", genre);
                return Convert.ToInt32(c.ExecuteScalar());
            }
        }

        public static void InsertFilmGenre(SqlConnection connection, int filmId, int genreId)
        {
            const string ins = "INSERT INTO Film_Genre (film_id, genre_id) VALUES (@film_id, @genre_id)";
            using var c = new SqlCommand(ins, connection);
            c.Parameters.AddWithValue("@film_id", filmId);
            c.Parameters.AddWithValue("@genre_id", genreId);
            c.ExecuteNonQuery();
        }

        public static void InsertBewertung(SqlConnection connection, int filmId, int bewertung, string kommentar, string ratingType)
        {
            const string ins = @"
                INSERT INTO Bewertung (film_id, bewertung, kommentar, rating_type)
                VALUES (@film_id, @bewertung, @kommentar, @rating_type)";
            using var c = new SqlCommand(ins, connection);
            c.Parameters.AddWithValue("@film_id", filmId);
            c.Parameters.AddWithValue("@bewertung", bewertung);
            c.Parameters.AddWithValue("@kommentar", kommentar);
            c.Parameters.AddWithValue("@rating_type", ratingType);
            c.ExecuteNonQuery();
        }
    }
}