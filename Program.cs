using Microsoft.Data.SqlClient;
using System.Runtime.InteropServices;

namespace testluca
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        static async Task Main()
        {
            string connectionString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;";
            string useDBQuery = "USE Test10;";

            keybd_event(0x7A, 0, 0x0000, 0);
            Thread.Sleep(50);
            keybd_event(0x7A, 0, 0x0002, 0);
            Thread.Sleep(50);
            keybd_event(0x11, 0, 0x0000, 0);
            Thread.Sleep(50);
            mouse_event(0x0800, 0, 0, 780, 0);
            Thread.Sleep(50);
            keybd_event(0x11, 0, 0x0002, 0);

            var filmManager = new FilmManager();
            var movieApi = new MovieApi();

            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔════════════════════════════════════════╗");
                Console.WriteLine("║          FILMVERWALTUNG MENÜ           ║");
                Console.WriteLine("╠════════════════════════════════════════╣");
                Console.ResetColor();
                Console.WriteLine("║ 1. Film hinzufügen                     ║");
                Console.WriteLine("║ 2. Film löschen                        ║");
                Console.WriteLine("║ 3. FilmListe importieren               ║");
                Console.WriteLine("║ 4. Errate die Bewertung                ║");
                Console.WriteLine("║ 5. Filme exportieren                   ║");
                Console.WriteLine("║ 6. Suche                               ║");
                Console.WriteLine("║ 7. Listenverwaltung                    ║");
                Console.WriteLine("╠════════════════════════════════════════╣");
                Console.WriteLine("║ 0. Beenden                             ║");
                Console.WriteLine("╚════════════════════════════════════════╝");
                Console.Write("Auswahl: ");

                string? auswahl = Console.ReadLine();

                using var connection = new SqlConnection(connectionString);
                connection.Open();
                new SqlCommand(useDBQuery, connection).ExecuteNonQuery();

                DatabaseManager.CreateTables(connection);

                switch (auswahl)
                {
                    case "1":
                        await FilmManager.AddFilmAsync(connection);
                        break;
                    case "2":
                        FilmManager.DeleteFilm(connection);
                        break;
                    case "3":
                        var filmListInserter = new FilmListInserter(connection, filmManager, movieApi);
                        string? filePath = "filme.txt";
                        if (!string.IsNullOrWhiteSpace(filePath))
                        {
                            await filmListInserter.InsertFilmListFromFileAsync(filePath);
                        }
                        else
                        {
                            Console.WriteLine("Ungültiger Dateipfad.");
                        }
                        break;
                    case "4":
                        GuessTheRatingGame.Play(connection);
                        continue;
                    case "7":
                        ListManager.ManageLists(connection);
                        continue;
                    case "6":
                        Console.WriteLine("Gib ein Genre ein (oder drücke Enter, um es zu überspringen):");
                        string? genre = Console.ReadLine();

                        Console.WriteLine("Gib einen Titel ein (oder drücke Enter, um es zu überspringen):");
                        string? title = Console.ReadLine();

                        Console.WriteLine("Gib einen Autor (Writer) ein (oder drücke Enter, um es zu überspringen):");
                        string? writer = Console.ReadLine();

                        Console.WriteLine("Gib einen Regisseur (Director) ein (oder drücke Enter, um es zu überspringen):");
                        string? director = Console.ReadLine();

                        Console.WriteLine("Sortiere nach 'title', 'year', 'rt' (rotten tomatoes) oder 'user' (eigene bewertung) (Standard: 'title'):");
                        string? sortBy = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(sortBy)) sortBy = "title";

                        Console.Clear();
                        FacetedSearch.SearchFilms(connection, genre, title, writer, director, sortBy);
                        break;
                    case "5":
                        Console.Clear();
                        Console.WriteLine("=== Filme exportieren ===\n");
                        Console.WriteLine("1. Alle Filme exportieren");
                        Console.WriteLine("2. 'Plan to Watch' exportieren");
                        Console.WriteLine("3. 'Favoriten' exportieren");
                        Console.Write("\nWähle die Liste aus (1-3): ");

                        string? listChoice = Console.ReadLine();
                        string listType = listChoice switch
                        {
                            "1" => "ALL",
                            "2" => "PLAN TO WATCH",
                            "3" => "FAVORITEN",
                            _ => null
                        };
                        if (listType == null)
                        {
                            Console.WriteLine("Ungültige Auswahl. Export abgebrochen.");
                            break;
                        }

                        Console.Write("\nWähle das Exportformat (JSON/CSV): ");
                        string? format = Console.ReadLine()?.ToUpper();
                        if (format != "JSON" && format != "CSV")
                        {
                            Console.WriteLine("Ungültiges Format. Unterstützte Formate: JSON, CSV.");
                            break;
                        }

                        try
                        {
                            await ExportMovies.ExportFilmsToFileAsync(connection, listType, format);
                            Console.WriteLine($"\nExport erfolgreich! Die Datei wurde im {format}-Format erstellt.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Fehler beim Export: {ex.Message}");
                        }
                        break;
                    case "0":
                        Console.WriteLine("Programm beendet.");
                        return;
                    default:
                        Console.WriteLine("Ungültige Auswahl. Bitte erneut versuchen.");
                        break;
                }
                Console.WriteLine("\nDrücken Sie eine beliebige Taste, um fortzufahren...");
                Console.ReadKey();
            }
        }
    }
}