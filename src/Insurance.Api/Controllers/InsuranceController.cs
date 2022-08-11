namespace Insurance.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    public class InsuranceController : Controller
    {
        private readonly IInsuranceService _insuranceService;

        public InsuranceController(IInsuranceService insuranceService)
        {
            _insuranceService = insuranceService;
        }

        /// <summary>
        /// Calculate Insurance
        /// </summary>
        /// <returns>insurance</returns>
        [HttpPost]
        public async Task<IActionResult> Product([FromBody] InsuranceDto toInsure)
        {
            var productType = await _insuranceService.GetProductTypeAsync(toInsure);
            if (productType.Status == ApiState.NotFound)
            {
                return NotFound("Product type could not be found");
            }
            toInsure.ProductTypeHasInsurance = productType.Result.ProductTypeHasInsurance;
            toInsure.ProductTypeName = productType.Result.ProductTypeName;

            if (!toInsure.ProductTypeHasInsurance)
                return Ok(toInsure.InsuranceValue);


            var salesPrice = await _insuranceService.GetSalesPriceAsync(toInsure);
            if (salesPrice.Status == ApiState.NotFound)
            {
                return NotFound("Sales price could not be found");
            }
            toInsure.SalesPrice = salesPrice.Result.SalesPrice;

            List<string> typeList = new List<string> {
                    StaticDataProvider.Laptops,
                    StaticDataProvider.DigitalCameras,
                    StaticDataProvider.SmartPhones};

            var insuranceValue = _insuranceService.CalculateInsuranceValue(toInsure, typeList);
            return Ok(insuranceValue.Result.InsuranceValue);
        }

        ///// <summary>
        ///// Calculate Insurance
        ///// </summary>
        ///// <returns>insurance</returns>
        [HttpPost]
        public async Task<IActionResult> ProductOrder([FromBody] List<InsuranceDto> toInsure)
        {

            List<string> typeList = new List<string> {
                    StaticDataProvider.Laptops,
                    StaticDataProvider.SmartPhones};

            float insurance = 0f;
            bool digitalCameraFound = false;

            for (int i = 0; i < toInsure.Count; i++)
            {
                var productType = await _insuranceService.GetProductTypeAsync(toInsure[i]);
                toInsure[i].ProductTypeHasInsurance = productType.Result.ProductTypeHasInsurance;
                toInsure[i].ProductTypeName = productType.Result.ProductTypeName;

                if (toInsure[i].ProductTypeHasInsurance)
                {
                    var salesPrice = await _insuranceService.GetSalesPriceAsync(toInsure[i]);
                    toInsure[i].SalesPrice = salesPrice.Result.SalesPrice;
                    toInsure[i].InsuranceValue = _insuranceService.CalculateInsuranceValue(toInsure[i], typeList).Result.InsuranceValue;
                }

                insurance += toInsure[i].InsuranceValue;
                if (!digitalCameraFound && toInsure[i].ProductTypeName.Equals(StaticDataProvider.DigitalCameras))
                    insurance += StaticDataProvider.DigitalCamerasAdditionalInsuranceValue;
            }

            return Ok(insurance);

        }

        /// <summary>
        /// Add Surcharge
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Surcharge([FromBody] InsuranceDto toInsure)
        {
            var productType = await _insuranceService.GetProductTypeAsync(toInsure);
            if (productType.Status == ApiState.NotFound)
            {
                return NotFound("Product type could not be found");
            }
            toInsure.ProductTypeHasInsurance = productType.Result.ProductTypeHasInsurance;
            toInsure.ProductTypeName = productType.Result.ProductTypeName;

            if (toInsure.ProductTypeName == null)
            {
                return BadRequest("Surcharge rate could not be applied. Please check the ID");
            }

            await _insuranceService.AddSurchargeAsync(toInsure);

            return Ok("Surcharge rate applied");
        }
    }
}