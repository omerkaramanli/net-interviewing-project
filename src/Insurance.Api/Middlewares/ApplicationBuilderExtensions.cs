namespace Insurance.Api.Middlewares
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseApiServiceExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(applicationBuilder =>
            {
                applicationBuilder.Run(async httpContext =>
                {
                    var logger = applicationBuilder.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(UseApiServiceExceptionHandler));

                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    httpContext.Response.ContentType = MediaTypeNames.Application.Json;

                    IExceptionHandlerFeature exceptionHandlerFeature = httpContext.Features.Get<IExceptionHandlerFeature>();
                    if (logger is not null)
                    {
                        logger.LogError(exceptionHandlerFeature?.Error, "-- HTTP 500 Error!! -- {@message}", exceptionHandlerFeature?.Error?.Message);
                    }

                    if (exceptionHandlerFeature is not null)
                    {
                        ApiResponseModel<object> apiResponseModel = new(ApiState.Error, exceptionHandlerFeature.Error.Message);
                        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(apiResponseModel));
                    }
                });
            });

            return app;
        }
    }
}
