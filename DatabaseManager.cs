using Microsoft.Data.SqlClient;

namespace testluca
{
    public class DatabaseManager
    {
        public static void CreateTables(SqlConnection connection)
        {
            CreateTable(connection, createRegisseurTable);
            CreateTable(connection, createAutorTable);
            CreateTable(connection, createGenreTable);
            CreateTable(connection, createListeTable);
            CreateDefaultListen(connection);
            CreateTable(connection, createFilmeTable);
            CreateTable(connection, createFilmGenreTable);
            CreateTable(connection, createBewertungTable);
            CreateTable(connection, createListeFilmTable);
            CreateTable(connection, createHighscoresTable);
        }

        private static void CreateTable(SqlConnection connection, string sql)
        {
            using var cmd = new SqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        private static void CreateDefaultListen(SqlConnection connection)
        {
            string insertListen = @"
                IF NOT EXISTS (SELECT * FROM Liste WHERE name = 'Favoriten')
                    INSERT INTO Liste (name) VALUES ('Favoriten');
                
                IF NOT EXISTS (SELECT * FROM Liste WHERE name = 'Plan to Watch')
                    INSERT INTO Liste (name) VALUES ('Plan to Watch');";

            using var cmd = new SqlCommand(insertListen, connection);
            cmd.ExecuteNonQuery();
        }

        private const string createRegisseurTable = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Regisseur')
            BEGIN
                CREATE TABLE Regisseur (
                    regisseur_id INT PRIMARY KEY IDENTITY,
                    name VARCHAR(255)
                );
            END";

        private const string createAutorTable = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Autor')
            BEGIN
                CREATE TABLE Autor (
                    autor_id INT PRIMARY KEY IDENTITY,
                    name VARCHAR(255)
                );
            END";

        private const string createGenreTable = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Genre')
            BEGIN
                CREATE TABLE Genre (
                    genre_id INT PRIMARY KEY IDENTITY,
                    name VARCHAR(255)
                );
            END";

        private const string createListeTable = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Liste')
            BEGIN
                CREATE TABLE Liste (
                    liste_id INT PRIMARY KEY IDENTITY,
                    name VARCHAR(255)
                );
            END";

        private const string createFilmeTable = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Filme')
            BEGIN
                CREATE TABLE Filme (
                    film_id INT PRIMARY KEY IDENTITY,
                    titel VARCHAR(255),
                    jahr VARCHAR(4),
                    regisseur_id INT,
                    autor_id INT,
                    FOREIGN KEY (regisseur_id) REFERENCES Regisseur(regisseur_id),
                    FOREIGN KEY (autor_id) REFERENCES Autor(autor_id)
                );
            END";

        private const string createFilmGenreTable = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Film_Genre')
            BEGIN
                CREATE TABLE Film_Genre (
                    film_id INT,
                    genre_id INT,
                    PRIMARY KEY (film_id, genre_id),
                    FOREIGN KEY (film_id) REFERENCES Filme(film_id),
                    FOREIGN KEY (genre_id) REFERENCES Genre(genre_id)
                );
            END";

        private const string createBewertungTable = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Bewertung')
            BEGIN
                CREATE TABLE Bewertung (
                    film_id INT,
                    bewertung INT,
                    kommentar NVARCHAR(MAX),
                    rating_type VARCHAR(50),
                    FOREIGN KEY (film_id) REFERENCES Filme(film_id)
                );
            END";

        private const string createListeFilmTable = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Liste_Film')
            BEGIN
                CREATE TABLE Liste_Film (
                    liste_id INT,
                    film_id INT,
                    PRIMARY KEY (liste_id, film_id),
                    FOREIGN KEY (liste_id) REFERENCES Liste(liste_id),
                    FOREIGN KEY (film_id) REFERENCES Filme(film_id)
                );
            END";
        private const string createHighscoresTable = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Highscores')
            BEGIN
                CREATE TABLE Highscores (
                    id INT PRIMARY KEY IDENTITY,
                    name NVARCHAR(100) NOT NULL,
                    score INT NOT NULL,
                    date DATETIME DEFAULT GETDATE()
                );
            END";
    }
}