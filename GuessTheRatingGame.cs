using Microsoft.Data.SqlClient;

namespace testluca
{
    public class GuessTheRatingGame
    {
        private const string HighScoreFile = "highscores.txt";

        public static void Play(SqlConnection connection)
        {
            int score = 0;
            bool gameOver = false;

            while (!gameOver)
            {
                Console.Clear();
                Console.WriteLine("=== Errate das Rotten Tomatoes Rating ===\n");

                var randomFilmQuery = @"
                    SELECT TOP 1 
                        f.film_id, 
                        f.titel, 
                        (SELECT TOP 1 b.bewertung 
                         FROM Bewertung b 
                         WHERE b.film_id = f.film_id 
                           AND b.rating_type = 'Rotten Tomatoes' 
                           AND b.bewertung != '0' 
                           AND b.bewertung IS NOT NULL) AS rotten_tomatoes_bewertung
                    FROM Filme f
                    WHERE EXISTS (
                        SELECT 1 
                        FROM Bewertung b 
                        WHERE b.film_id = f.film_id 
                          AND b.rating_type = 'Rotten Tomatoes'
                          AND b.bewertung != '0'
                          AND b.bewertung IS NOT NULL)
                    ORDER BY NEWID()";
                int filmId = 0;
                string filmTitle = string.Empty;
                int actualRating = 0;

                using (var cmd = new SqlCommand(randomFilmQuery, connection))
                {
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        filmId = reader.GetInt32(0);
                        filmTitle = reader.GetString(1);
                        actualRating = reader["rotten_tomatoes_bewertung"] != DBNull.Value ? reader.GetInt32(2) : 0;
                    }
                }

                if (filmId == 0)
                {
                    Console.WriteLine("Es konnte kein Film mit einem Rotten Tomatoes Rating gefunden werden.");
                    return;
                }

                // Spiel starten
                Console.WriteLine($"Wie hoch ist das Rotten Tomatoes Rating für den Film \"{filmTitle}\"?");
                Console.Write("Dein Tipp (0-100): ");

                if (!int.TryParse(Console.ReadLine(), out int userGuess) || userGuess < 0 || userGuess > 100)
                {
                    Console.WriteLine("Ungültige Eingabe. Bitte gib eine Zahl zwischen 0 und 100 ein.");
                    continue;
                }

                // Punkte berechnen
                int difference = Math.Abs(actualRating - userGuess);

                if (difference >= 25)
                {
                    Console.WriteLine($"\nDas tatsächliche Rotten Tomatoes Rating ist: {actualRating}%");
                    Console.WriteLine($"Dein Tipp: {userGuess}%");
                    Console.WriteLine("Du warst zu weit entfernt! Spiel vorbei.");
                    gameOver = true;
                }
                else
                {
                    int points = 0;

                    if (difference == 0)
                    {
                        points = 100;
                    }
                    else if (difference <= 10)
                    {
                        points = 50;
                    }
                    else if (difference <= 20)
                    {
                        points = 20;
                    }

                    score += points;

                    Console.WriteLine($"\nDas tatsächliche Rotten Tomatoes Rating ist: {actualRating}%");
                    Console.WriteLine($"Dein Tipp: {userGuess}%");
                    Console.WriteLine($"Punkte für diese Runde: {points}");
                    Console.WriteLine($"Aktueller Punktestand: {score}");
                }

                Console.WriteLine("\nMöchtest du weiterspielen? (j/n): ");
                string? input = Console.ReadLine()?.ToLower();
                if (input != "j")
                {
                    gameOver = true;
                }
            }

            // Highscore speichern
            SaveHighScore(connection, score);

            // Highscores anzeigen
            DisplayHighScores(connection);

            Console.WriteLine("\nDrücke eine beliebige Taste, um zum Menü zurückzukehren...");
            Console.ReadKey();
        }

        private static void SaveHighScore(SqlConnection connection, int score)
        {
            Console.Write("Bitte gib deinen Namen für die Highscore-Liste ein: ");
            string? input = Console.ReadLine();
            string name = string.IsNullOrWhiteSpace(input) ? "Unbekannt" : input.Trim();

            const string insertQuery = "INSERT INTO Highscores (name, score) VALUES (@name, @score)";
            using var cmd = new SqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@score", score);
            cmd.ExecuteNonQuery();
        }

        private static void DisplayHighScores(SqlConnection connection)
        {
            Console.Clear();
            Console.WriteLine("=== Highscores ===\n");

            const string selectQuery = "SELECT TOP 5 name, score, date FROM Highscores ORDER BY score DESC";
            using var cmd = new SqlCommand(selectQuery, connection);
            using var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                Console.WriteLine("Keine Highscores vorhanden.");
                return;
            }

            while (reader.Read())
            {
                string name = reader.GetString(0);
                int score = reader.GetInt32(1);
                DateTime date = reader.GetDateTime(2);

                Console.WriteLine($"{name}: {score} Punkte (am {date:dd.MM.yyyy})");
            }
        }
    }
}