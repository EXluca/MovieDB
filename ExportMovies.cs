using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace testluca
{
    class ExportMovies
    {
        public static async Task ExportFilmsToFileAsync(SqlConnection connection, string listType = "ALL", string format = "JSON")
        {
            string query = GetQueryForListType(listType);
            string fileName = $"{listType.ToUpper()}.{format.ToUpper()}";

            using var cmd = new SqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            if (format.Equals("JSON", StringComparison.CurrentCultureIgnoreCase))
            {
                await ExportToJsonAsync(reader, fileName);
            }
            else if (format.Equals("CSV", StringComparison.CurrentCultureIgnoreCase))
            {
                await ExportToCsvAsync(reader, fileName);
            }
            else
            {
                Console.WriteLine("Ungültiges Exportformat. Unterstützte Formate: JSON, CSV.");
            }
        }

        private static string GetQueryForListType(string listType)
        {
            string baseQuery = @"
                SELECT f.titel, f.jahr, r.name AS regisseur, a.name AS autor, 
                       STRING_AGG(g.name, ', ') AS genres,
                       (SELECT b.bewertung FROM Bewertung b WHERE b.film_id = f.film_id AND b.rating_type = 'Rotten Tomatoes') AS rotten_tomatoes_bewertung
                FROM Filme f
                LEFT JOIN Regisseur r ON f.regisseur_id = r.regisseur_id
                LEFT JOIN Autor a ON f.autor_id = a.autor_id
                LEFT JOIN Film_Genre fg ON f.film_id = fg.film_id
                LEFT JOIN Genre g ON fg.genre_id = g.genre_id";

            if (listType.ToUpper() == "PLAN TO WATCH")
            {
                return baseQuery + @"
                INNER JOIN Liste_Film lf ON f.film_id = lf.film_id
                INNER JOIN Liste l ON lf.liste_id = l.liste_id
                WHERE l.name = 'Plan to Watch'
                GROUP BY f.titel, f.jahr, r.name, a.name, f.film_id";
            }
            else if (listType.ToUpper() == "FAVORITEN")
            {
                return baseQuery + @"
                INNER JOIN Liste_Film lf ON f.film_id = lf.film_id
                INNER JOIN Liste l ON lf.liste_id = l.liste_id
                WHERE l.name = 'Favoriten'
                GROUP BY f.titel, f.jahr, r.name, a.name, f.film_id";
            }
            else
            {
                return baseQuery + @"
                GROUP BY f.titel, f.jahr, r.name, a.name, f.film_id";
            }
        }


        private static async Task ExportToJsonAsync(SqlDataReader reader, string fileName)
        {
            var movies = new List<object>();

            while (reader.Read())
            {
                var movie = new
                {
                    Titel = reader.GetString(0),
                    Jahr = reader.GetString(1),
                    Regisseur = reader.IsDBNull(2) ? "Unbekannt" : reader.GetString(2),
                    Autor = reader.IsDBNull(3) ? "Unbekannt" : reader.GetString(3),
                    Genres = reader.IsDBNull(4) ? "Keine Genres" : reader.GetString(4),
                    RottenTomatoesBewertung = reader.IsDBNull(5) ? "Keine" : reader.GetInt32(5).ToString()
                };
                movies.Add(movie);
            }
            string json = JsonSerializer.Serialize(movies, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fileName, json);
            Console.WriteLine($"Filme erfolgreich nach {fileName} exportiert.");
        }


        private static async Task ExportToCsvAsync(SqlDataReader reader, string fileName)
        {
            using var writer = new StreamWriter(fileName);
            await writer.WriteLineAsync("Titel,Jahr,Regisseur,Autor,Genres,RottenTomatoesBewertung");

            while (reader.Read())
            {
                string title = reader.GetString(0);
                string year = reader.GetString(1);
                string director = reader.IsDBNull(2) ? "Unbekannt" : reader.GetString(2);
                string author = reader.IsDBNull(3) ? "Unbekannt" : reader.GetString(3);
                string genres = reader.IsDBNull(4) ? "Keine Genres" : reader.GetString(4);
                string rottenTomatoes = reader.IsDBNull(5) ? "Keine" : reader.GetInt32(5).ToString();

                await writer.WriteLineAsync($"{title},{year},{director},{author},{genres},{rottenTomatoes}");
            }
            Console.WriteLine($"Filme erfolgreich nach {fileName} exportiert.");
        }
    }
}