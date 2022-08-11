namespace Insurance.Api.Services
{
    public interface IInsuranceService
    {
        Task<ApiResponseModel<InsuranceDto>> GetProductTypeAsync(InsuranceDto insurance);
        Task<ApiResponseModel<InsuranceDto>> GetSalesPriceAsync(InsuranceDto insurance);
        ApiResponseModel<InsuranceDto> CalculateInsuranceValue(InsuranceDto insurance, IList<string> typeList);
        Task AddSurchargeAsync(InsuranceDto insurance);
    }
}
