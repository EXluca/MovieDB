using Newtonsoft.Json;

namespace testluca
{
    public class MovieApi
    {
        private static readonly string apiKey = "e4534050";

        public static async Task<Movie?> GetMovieDataAsync(string movieTitle, string? year = null)
        {
            string url = $"http://www.omdbapi.com/?t={movieTitle}&apikey={apiKey}";
            if (!string.IsNullOrWhiteSpace(year))
            {
                url += $"&y={year}";
            }

            using var client = new HttpClient();
            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            var movie = JsonConvert.DeserializeObject<Movie>(body);

            if (movie?.Response == "True")
            {
                if (movie.Ratings != null)
                {
                    var rt = movie.Ratings.FirstOrDefault(r => r.Source == "Rotten Tomatoes");
                    if (rt != null && rt.Value != null && int.TryParse(rt.Value.Replace("%", ""), out int pct))
                        movie.RottenTomatoesRating = pct;
                }
                return movie;
            }
            return null;
        }
    }
}