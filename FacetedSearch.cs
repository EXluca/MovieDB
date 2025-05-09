using Microsoft.Data.SqlClient;

namespace testluca
{
    class FacetedSearch
    {
        public static void SearchFilms(SqlConnection connection, string? searchByGenre = null, string? searchByTitle = null, string? searchByWriter = null, string? searchByDirector = null, string? sortBy = "title")
        {
            sortBy = (sortBy?.ToLower()) switch
            {
                "user" => "ur.bewertung DESC",
                "rt" => "rt.bewertung DESC",
                "year" => "f.jahr",
                _ => "f.titel"
            };
            var query = @"
                SELECT DISTINCT f.film_id, f.titel, f.jahr, r.name AS regisseur, a.name AS autor,
                                rt.bewertung AS rotten_tomatoes_rating, ur.bewertung AS user_rating
                FROM Filme f
                LEFT JOIN Regisseur r ON f.regisseur_id = r.regisseur_id
                LEFT JOIN Autor a ON f.autor_id = a.autor_id
                LEFT JOIN Film_Genre fg ON f.film_id = fg.film_id
                LEFT JOIN Genre g ON fg.genre_id = g.genre_id
                LEFT JOIN Bewertung rt ON f.film_id = rt.film_id AND rt.rating_type = 'Rotten Tomatoes'
                LEFT JOIN Bewertung ur ON f.film_id = ur.film_id AND ur.rating_type = 'User'
                WHERE (@searchByGenre IS NULL OR g.name = @searchByGenre)
                  AND (@searchByTitle IS NULL OR f.titel LIKE '%' + @searchByTitle + '%')
                  AND (@searchByWriter IS NULL OR a.name LIKE '%' + @searchByWriter + '%')
                  AND (@searchByDirector IS NULL OR r.name LIKE '%' + @searchByDirector + '%')
                ORDER BY " + sortBy;

            using var cmd = new SqlCommand(query, connection);

            cmd.Parameters.AddWithValue("@searchByGenre", string.IsNullOrEmpty(searchByGenre) ? DBNull.Value : searchByGenre);
            cmd.Parameters.AddWithValue("@searchByTitle", string.IsNullOrEmpty(searchByTitle) ? DBNull.Value : searchByTitle);
            cmd.Parameters.AddWithValue("@searchByWriter", string.IsNullOrEmpty(searchByWriter) ? DBNull.Value : searchByWriter);
            cmd.Parameters.AddWithValue("@searchByDirector", string.IsNullOrEmpty(searchByDirector) ? DBNull.Value : searchByDirector);

            using var reader = cmd.ExecuteReader();
            Console.WriteLine($"{"FilmId",-10}{"Titel",-50}{"Jahr",-10}{"Regisseur",-40}{"Autor",-40}{"RT-Rating",-15}{"User-Rating",-15}");
            Console.WriteLine(new string('-', 180));
            while (reader.Read())
            {
                Console.WriteLine($"{reader.GetInt32(0),-10}{reader.GetString(1),-50}{reader.GetString(2),-10}{(reader.IsDBNull(3) ? "Unbekannt" : reader.GetString(3)),-40}{(reader.IsDBNull(4) ? "Unbekannt" : reader.GetString(4)),-40}{(reader.IsDBNull(5) ? "N/A" : reader.GetInt32(5).ToString()),-15}{(reader.IsDBNull(6) ? "N/A" : reader.GetInt32(6).ToString()),-15}");
            }
        }
    }
}