namespace testluca
{
    public class Movie
    {
        public int FilmId { get; set; }
        public string? Title { get; set; }
        public string? Year { get; set; }
        public string? Director { get; set; }
        public string? Writer { get; set; }
        public string? Genre { get; set; }
        public List<Rating>? Ratings { get; set; }
        public int RottenTomatoesRating { get; set; }
        public string? Response { get; set; }
    }

    public class Rating
    {
        public string? Source { get; set; }
        public string? Value { get; set; }
    }
}