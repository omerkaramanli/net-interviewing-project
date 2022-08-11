namespace Insurance.Api.Services
{
    public sealed class InsuranceService : IInsuranceService
    {
        private readonly ILogger<InsuranceService> _logger;
        private readonly HttpClient _httpClient;

        public InsuranceService(
            ILogger<InsuranceService> logger,
            HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            //HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5002") };
        }

        public async Task<ApiResponseModel<InsuranceDto>> GetProductTypeAsync(InsuranceDto insurance)
        {
            _logger.LogInformation("Getting product type ProductId = {@ProductId} started.", insurance.ProductId);

            var productResponse = await _httpClient.GetAsync(string.Format("/products/{0:G}", insurance.ProductId));
            if (productResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiResponseModel<InsuranceDto>(ApiState.NotFound, "Product could not be found");
            }

            var productJson = await productResponse.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<ProductsDto>(productJson);

            var productTypeResponse = await _httpClient.GetAsync(string.Format("/product_types/{0:G}", product.productTypeId));
            if (productTypeResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiResponseModel<InsuranceDto>(ApiState.NotFound, "Product type could not be found");
            }
            var productTypeJson = await productTypeResponse.Content.ReadAsStringAsync();
            var productType = JsonSerializer.Deserialize<ProductTypesDto>(productTypeJson);

            insurance.ProductTypeName = productType.name;
            insurance.ProductTypeHasInsurance = productType.canBeInsured;

            _logger.LogInformation("Getting product type ProductId = {@ProductId} completed.", insurance.ProductId);

            return new ApiResponseModel<InsuranceDto>(ApiState.Success, insurance);
        }

        public async Task AddSurchargeAsync(InsuranceDto insurance)
        {
            await Task.Run(() =>
            {
                var surchargeRates = ReadSurchargeFile();
                surchargeRates = UpdateLine(surchargeRates, insurance);
                WriteSurchargeFile(surchargeRates);
            });
        }

        public ApiResponseModel<InsuranceDto> CalculateInsuranceValue(InsuranceDto insurance, IList<string> typeList)
        {
            insurance.InsuranceValue += CheckSalesPrice(insurance.SalesPrice);
            insurance.InsuranceValue += CheckProductType(insurance.ProductTypeName, typeList);
            insurance.InsuranceValue *= CheckSurchargeRate(insurance.ProductTypeName);

            return new ApiResponseModel<InsuranceDto>(ApiState.Success, insurance);
        }

        public async Task<ApiResponseModel<InsuranceDto>> GetSalesPriceAsync(InsuranceDto insurance)
        {
            //Log.Information($"Getting product sales price {{ProductId = {insurance.ProductId}}} started.");
            //HttpClient client = new HttpClient { BaseAddress = new Uri(baseAddress) };

            var salesPriceResponse = await _httpClient.GetAsync(string.Format("/products/{0:G}", insurance.ProductId));
            if (salesPriceResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiResponseModel<InsuranceDto>(ApiState.NotFound, "Sales price could not be found");
            }
            var productJson = await salesPriceResponse.Content.ReadAsStringAsync();

            var product = JsonSerializer.Deserialize<ProductsDto>(productJson);

            insurance.SalesPrice = product.salesPrice;
            _logger.LogInformation($"Getting product sales price {{ProductId = {insurance.ProductId}}} completed.");

            //if (0 < 1)
            //{
            //    return new ApiResponseModel<InsuranceDto>(ApiState.Error, "");
            //}

            return new ApiResponseModel<InsuranceDto>(ApiState.Success, insurance);
        }

        private static float CheckSurchargeRate(string ProductTypeName)
        {
            var surchargeRates = ReadSurchargeFile();
            float surchargeRate = 0f;
            if (surchargeRates.Any(x => x.ProductTypeName.Equals(ProductTypeName)))
                surchargeRate = surchargeRates.Find(x => x.ProductTypeName.Equals(ProductTypeName)).SurchargeRate;
            return 1 + (surchargeRate / 100);
        }

        private static List<InsuranceDto> ReadSurchargeFile()
        {
            var surchargeRates = new List<InsuranceDto>();
            if (!System.IO.File.Exists("SurchargeRates.csv"))
            {
                WriteSurchargeFile(surchargeRates);
            }
            using (var reader = new StreamReader("SurchargeRates.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    var surchargeRate = new InsuranceDto
                    {
                        ProductId = Int32.Parse(csv.GetField("ProductId")),
                        SurchargeRate = float.Parse(csv.GetField("SurchargeRate")),
                        ProductTypeName = csv.GetField("ProductTypeName")
                    };
                    surchargeRates.Add(surchargeRate);
                }
            }
            return surchargeRates;
        }
        private static void WriteSurchargeFile(List<InsuranceDto> surchargeRates)
        {
            using (var writer = new StreamWriter("SurchargeRates.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(surchargeRates);
            }
        }
        private static List<InsuranceDto> UpdateLine(List<InsuranceDto> surchargeRates, InsuranceDto toInsure)
        {
            if (surchargeRates.Any(x => x.ProductTypeName.Equals(toInsure.ProductTypeName)))
                surchargeRates.Remove(surchargeRates.Find(x => x.ProductTypeName.Equals(toInsure.ProductTypeName)));
            surchargeRates.Add(toInsure);
            return surchargeRates;
        }

        private static float CheckSalesPrice(float salesPrice)
        {
            if (salesPrice >= StaticDataProvider.SalesPriceFirstThreshold &&
                salesPrice < StaticDataProvider.SalesPriceSecondThreshold)
            {
                return StaticDataProvider.FirstInsuranceValue;
            }
            else if (salesPrice >= StaticDataProvider.SalesPriceSecondThreshold)
            {
                return StaticDataProvider.SecondInsuranceValue;
            }

            return 0;
        }

        private static float CheckProductType(string type, IList<string> typeList)
        {
            if (typeList.Any(x => x.Equals(type)))
            {
                if (type == StaticDataProvider.Laptops)
                    return StaticDataProvider.LaptopsAdditionalInsuranceValue;

                if (type == StaticDataProvider.SmartPhones)
                    return StaticDataProvider.SmartPhonesAdditionalInsuranceValue;

                if (type == StaticDataProvider.DigitalCameras)
                    return StaticDataProvider.DigitalCamerasAdditionalInsuranceValue;
            }

            return 0;
        }
    }
}
