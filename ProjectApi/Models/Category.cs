namespace ProjectApi.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Quan hệ 1-n với Product
        public ICollection<ProjectApi.Api.Models.Product>? Products { get; set; }
    }
}
