namespace Insurance.Api.Dtos
{
    public class ProductsDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public int productTypeId { get; set; }
        public float salesPrice { get; set; }
    }
}
