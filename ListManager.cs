using Microsoft.Data.SqlClient;

namespace testluca
{
    public class ListManager
    {
        public static void ManageLists(SqlConnection connection)
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔════════════════════════════════════════╗");
                Console.WriteLine("║          LISTENVERWALTUNG MENÜ         ║");
                Console.WriteLine("╠════════════════════════════════════════╣");
                Console.ResetColor();
                Console.WriteLine("║ 1. Film zu Liste hinzufügen            ║");
                Console.WriteLine("║ 2. Film aus Liste entfernen            ║");
                Console.WriteLine("║ 3. Bewertung und Kommentar bearbeiten  ║");
                Console.WriteLine("║ 4. Listen Exportieren                  ║");
                Console.WriteLine("║ 5. Zeige Spezielle Listen              ║");
                Console.WriteLine("╠════════════════════════════════════════╣");
                Console.WriteLine("║ 0. Zurück                              ║");
                Console.WriteLine("╚════════════════════════════════════════╝");
                Console.Write("Auswahl: ");

                string? auswahl = Console.ReadLine();

                if (auswahl == "1")
                {
                    AddFilmToList(connection);
                }
                else if (auswahl == "2")
                {
                    RemoveFilmFromList(connection);
                }
                else if (auswahl == "3")
                {
                    EditRatingAndComment(connection);
                }
                else if (auswahl == "5")
                {
                    ShowSpecialListsMenu(connection);
                }
                else if (auswahl == "4")
                {
                    //ExportMovies.ExportFilmsToFileAsync(connection);
                    Console.WriteLine("Exportieren ist noch nicht implementiert.");
                }
                else if (auswahl == "0")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Ungültige Auswahl. Bitte erneut versuchen.");
                    Console.ReadKey();
                }
            }
        }

        private static void AddFilmToList(SqlConnection connection)
        {
            Console.Clear();
            Console.WriteLine("=== Film zu Liste hinzufügen ===\n");

            var lists = new Dictionary<int, string>();
            using (var cmd = new SqlCommand("SELECT liste_id, name FROM Liste", connection))
            {
                using var reader = cmd.ExecuteReader();
                Console.WriteLine("Verfügbare Listen:");
                while (reader.Read())
                {
                    lists.Add(reader.GetInt32(0), reader.GetString(1));
                    Console.WriteLine($"[{reader["liste_id"]}] {reader["name"]}");
                }
            }

            Console.Write("\nListe-ID auswählen: ");
            if (!int.TryParse(Console.ReadLine(), out int listId) || !lists.ContainsKey(listId))
            {
                Console.WriteLine("Ungültige Listen-ID.");
                return;
            }

            Console.Write("Filmtitel suchen: ");
            string? searchTerm = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                Console.WriteLine("Ungültiger Suchbegriff.");
                return;
            }

            var films = new List<dynamic>();
            using (var cmd = new SqlCommand(@"
                SELECT TOP 10 film_id, titel, jahr 
                FROM Filme 
                WHERE titel LIKE @search 
                ORDER BY titel", connection))
            {
                cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");
                using var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Keine Filme gefunden.");
                    return;
                }

                Console.WriteLine("\nGefundene Filme:");
                while (reader.Read())
                {
                    films.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Year = reader.GetString(2)
                    });
                    Console.WriteLine($"[{reader["film_id"]}] {reader["titel"]} ({reader["jahr"]})");
                }
            }

            Console.Write("\nFilm-ID auswählen: ");
            if (!int.TryParse(Console.ReadLine(), out int filmId) || !films.Any(f => f.Id == filmId))
            {
                Console.WriteLine("Ungültige Film-ID.");
                return;
            }

            Console.Write("Möchten Sie eine Bewertung (0-100) hinzufügen? (Leer lassen, um zu überspringen): ");
            string? bewertungInput = Console.ReadLine();
            int? bewertung = null;
            if (int.TryParse(bewertungInput, out int parsedBewertung) && parsedBewertung >= 0 && parsedBewertung <= 100)
            {
                bewertung = parsedBewertung;
            }

            Console.Write("Möchten Sie einen Kommentar hinzufügen? (Leer lassen, um zu überspringen): ");
            string? kommentar = Console.ReadLine();

            const string insertQuery = @"
                IF NOT EXISTS (SELECT 1 FROM Liste_Film WHERE liste_id = @liste_id AND film_id = @film_id)
                BEGIN
                    INSERT INTO Liste_Film (liste_id, film_id) VALUES (@liste_id, @film_id);
                END
                IF NOT EXISTS (SELECT 1 FROM Bewertung WHERE film_id = @film_id AND rating_type = 'User')
                BEGIN
                    INSERT INTO Bewertung (film_id, bewertung, kommentar, rating_type) 
                    VALUES (@film_id, @bewertung, @kommentar, 'User');
                END";

            using (var cmd = new SqlCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@liste_id", listId);
                cmd.Parameters.AddWithValue("@film_id", filmId);
                cmd.Parameters.AddWithValue("@bewertung", (object?)bewertung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@kommentar", string.IsNullOrWhiteSpace(kommentar) ? DBNull.Value : kommentar);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("\nFilm erfolgreich zur Liste hinzugefügt!");
        }


        private static void RemoveFilmFromList(SqlConnection connection)
        {
            Console.Clear();
            Console.WriteLine("=== Film aus Liste entfernen ===\n");

            var lists = new Dictionary<int, string>();
            using (var cmd = new SqlCommand("SELECT liste_id, name FROM Liste", connection))
            {
                using var reader = cmd.ExecuteReader();
                Console.WriteLine("Verfügbare Listen:");
                while (reader.Read())
                {
                    lists.Add(reader.GetInt32(0), reader.GetString(1));
                    Console.WriteLine($"[{reader["liste_id"]}] {reader["name"]}");
                }
            }

            Console.Write("\nListe-ID auswählen: ");
            if (!int.TryParse(Console.ReadLine(), out int listId) || !lists.ContainsKey(listId))
            {
                Console.WriteLine("Ungültige Listen-ID.");
                return;
            }

            var films = new List<dynamic>();
            using (var cmd = new SqlCommand(@"
                SELECT f.film_id, f.titel, f.jahr 
                FROM Filme f
                INNER JOIN Liste_Film lf ON f.film_id = lf.film_id
                WHERE lf.liste_id = @liste_id
                ORDER BY f.titel", connection))
            {
                cmd.Parameters.AddWithValue("@liste_id", listId);
                using var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Keine Filme in dieser Liste gefunden.");
                    return;
                }

                Console.WriteLine("\nFilme in der Liste:");
                while (reader.Read())
                {
                    films.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Year = reader.GetString(2)
                    });
                    Console.WriteLine($"[{reader["film_id"]}] {reader["titel"]} ({reader["jahr"]})");
                }
            }

            Console.Write("\nFilm-ID auswählen: ");
            if (!int.TryParse(Console.ReadLine(), out int filmId) || !films.Any(f => f.Id == filmId))
            {
                Console.WriteLine("Ungültige Film-ID.");
                return;
            }

            const string deleteQuery = "DELETE FROM Liste_Film WHERE liste_id = @liste_id AND film_id = @film_id";
            using (var cmd = new SqlCommand(deleteQuery, connection))
            {
                cmd.Parameters.AddWithValue("@liste_id", listId);
                cmd.Parameters.AddWithValue("@film_id", filmId);
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    Console.WriteLine("Film erfolgreich aus der Liste entfernt!");
                }
                else
                {
                    Console.WriteLine("Fehler: Film konnte nicht entfernt werden.");
                }
            }
        }

        private static void ShowSpecialListsMenu(SqlConnection connection)
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("╔════════════════════════════════════════╗");
                Console.WriteLine("║          SPEZIELLE LISTEN MENÜ         ║");
                Console.WriteLine("╠════════════════════════════════════════╣");
                Console.ResetColor();
                Console.WriteLine("║ 1. Favoriten anzeigen                  ║");
                Console.WriteLine("║ 2. Plan to Watch anzeigen              ║");
                Console.WriteLine("╠════════════════════════════════════════╣");
                Console.WriteLine("║ 0. Zurück                              ║");
                Console.WriteLine("╚════════════════════════════════════════╝");
                Console.Write("Auswahl: ");

                string? auswahl = Console.ReadLine();

                switch (auswahl)
                {
                    case "1":
                        ViewFavorites(connection);
                        break;
                    case "2":
                        ViewPlanToWatch(connection);
                        break;
                    case "0":
                        return; 
                    default:
                        Console.WriteLine("Ungültige Auswahl. Bitte erneut versuchen.");
                        Console.ReadKey();
                        break;
                }
            }
        }
        private static void ViewFavorites(SqlConnection connection)
        {
            Console.Clear();
            Console.WriteLine("=== Favoriten anzeigen ===\n");

            const string query = @"
                SELECT f.titel, f.jahr, 
                       (SELECT b.bewertung FROM Bewertung b WHERE b.film_id = f.film_id AND b.rating_type = 'User') AS benutzerbewertung,
                       (SELECT b.bewertung FROM Bewertung b WHERE b.film_id = f.film_id AND b.rating_type = 'Rotten Tomatoes') AS rotten_tomatoes_bewertung,
                       (SELECT b.kommentar FROM Bewertung b WHERE b.film_id = f.film_id AND b.rating_type = 'User') AS benutzerkommentar
                FROM Filme f
                INNER JOIN Liste_Film lf ON f.film_id = lf.film_id
                INNER JOIN Liste l ON lf.liste_id = l.liste_id
                WHERE l.name = 'Favoriten'
                ORDER BY f.titel";
            using (var cmd = new SqlCommand(query, connection))
            {
                using var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Keine Filme in der Favoritenliste gefunden.");
                    return;
                }

                Console.WriteLine("Favoriten:");
                while (reader.Read())
                {
                    string? benutzerbewertung = reader["benutzerbewertung"] != DBNull.Value ? reader["benutzerbewertung"].ToString() : "Keine";
                    string? rottenTomatoesBewertung = reader["rotten_tomatoes_bewertung"] != DBNull.Value ? reader["rotten_tomatoes_bewertung"].ToString() : "Keine";
                    string? benutzerkommentar = reader["benutzerkommentar"] != DBNull.Value ? reader["benutzerkommentar"].ToString() : "Keiner";

                    Console.WriteLine($"{reader["titel"], -50} ({reader["jahr"], -4}) - Benutzerbewertung: {benutzerbewertung, -10} - Kommentar: {benutzerkommentar, -50} - Rotten Tomatoes: {rottenTomatoesBewertung,-10}");
                }
            }

            Console.WriteLine("\nDrücken Sie eine beliebige Taste, um fortzufahren...");
            Console.ReadKey();
        }



        private static void ViewPlanToWatch(SqlConnection connection)
        {
            Console.Clear();
            Console.WriteLine("=== Plan to Watch anzeigen ===\n");

            const string query = @"
                SELECT f.titel, f.jahr, 
                       (SELECT b.bewertung FROM Bewertung b WHERE b.film_id = f.film_id AND b.rating_type = 'User') AS benutzerbewertung,
                       (SELECT b.bewertung FROM Bewertung b WHERE b.film_id = f.film_id AND b.rating_type = 'Rotten Tomatoes') AS rotten_tomatoes_bewertung,
                       (SELECT b.kommentar FROM Bewertung b WHERE b.film_id = f.film_id AND b.rating_type = 'User') AS benutzerkommentar
                FROM Filme f
                INNER JOIN Liste_Film lf ON f.film_id = lf.film_id
                INNER JOIN Liste l ON lf.liste_id = l.liste_id
                WHERE l.name = 'Plan to Watch'
                ORDER BY f.titel";
            using (var cmd = new SqlCommand(query, connection))
            {
                using var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Keine Filme in der 'Plan to Watch'-Liste gefunden.");
                    return;
                }

                Console.WriteLine("'Plan to Watch'-Liste:");
                while (reader.Read())
                {
                    string? benutzerbewertung = reader["benutzerbewertung"] != DBNull.Value ? reader["benutzerbewertung"].ToString() : "Keine";
                    string? rottenTomatoesBewertung = reader["rotten_tomatoes_bewertung"] != DBNull.Value ? reader["rotten_tomatoes_bewertung"].ToString() : "Keine";
                    string? benutzerkommentar = reader["benutzerkommentar"] != DBNull.Value ? reader["benutzerkommentar"].ToString() : "Keiner";

                    Console.WriteLine($"{reader["titel"],-50} ({reader["jahr"],-4}) - Benutzerbewertung: {benutzerbewertung,-10} - Kommentar: {benutzerkommentar,-50} - Rotten Tomatoes: {rottenTomatoesBewertung,-10}");
                }
            }
            Console.WriteLine("\nDrücken Sie eine beliebige Taste, um fortzufahren...");
            Console.ReadKey();
        }




        private static void EditRatingAndComment(SqlConnection connection)
        {
            Console.Clear();
            Console.WriteLine("=== Bewertung und Kommentar bearbeiten ===\n");

            Console.Write("Filmtitel suchen: ");
            string searchTerm = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                Console.WriteLine("Ungültiger Suchbegriff.");
                return;
            }

            var films = new List<dynamic>();
            using (var cmd = new SqlCommand(@"
                SELECT f.film_id, f.titel, 
                       (SELECT b.bewertung FROM Bewertung b WHERE b.film_id = f.film_id AND b.rating_type = 'User') AS benutzerbewertung,
                       (SELECT b.kommentar FROM Bewertung b WHERE b.film_id = f.film_id AND b.rating_type = 'User') AS benutzerkommentar
                FROM Filme f
                WHERE f.titel LIKE @search
                ORDER BY f.titel", connection))
            {
                cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");
                using var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Keine Filme gefunden.");
                    return;
                }

                Console.WriteLine("\nGefundene Filme:");
                while (reader.Read())
                {
                    films.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Bewertung = reader["benutzerbewertung"] != DBNull.Value ? reader.GetInt32(2) : (int?)null,
                        Kommentar = reader["benutzerkommentar"] != DBNull.Value ? reader.GetString(3) : null
                    });
                    Console.WriteLine($"[{reader["film_id"]}] {reader["titel"]} - Bewertung: {reader["benutzerbewertung"] ?? "Keine"} - Kommentar: {reader["benutzerkommentar"] ?? "Keiner"}");
                }
            }

            Console.Write("\nFilm-ID auswählen: ");
            if (!int.TryParse(Console.ReadLine(), out int filmId) || !films.Any(f => f.Id == filmId))
            {
                Console.WriteLine("Ungültige Film-ID.");
                return;
            }

            Console.Write("Neue Bewertung (0-100, Leer lassen, um unverändert zu lassen): ");
            string? bewertungInput = Console.ReadLine();
            int? bewertung = null;
            if (int.TryParse(bewertungInput, out int parsedBewertung) && parsedBewertung >= 0 && parsedBewertung <= 100)
            {
                bewertung = parsedBewertung;
            }

            Console.Write("Neuer Kommentar (Leer lassen, um unverändert zu lassen): ");
            string? kommentar = Console.ReadLine();

            const string updateQuery = @"
                IF EXISTS (SELECT 1 FROM Bewertung WHERE film_id = @film_id AND rating_type = 'User')
                BEGIN
                    UPDATE Bewertung 
                    SET bewertung = ISNULL(@bewertung, bewertung), 
                        kommentar = ISNULL(@kommentar, kommentar)
                    WHERE film_id = @film_id AND rating_type = 'User';
                END
                ELSE
                BEGIN
                    INSERT INTO Bewertung (film_id, bewertung, kommentar, rating_type) 
                    VALUES (@film_id, @bewertung, @kommentar, 'User');
                END";
            using (var cmd = new SqlCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@film_id", filmId);
                cmd.Parameters.AddWithValue("@bewertung", (object?)bewertung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@kommentar", string.IsNullOrWhiteSpace(kommentar) ? DBNull.Value : kommentar);
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("\nBewertung und Kommentar erfolgreich aktualisiert!");
        }
    }
}