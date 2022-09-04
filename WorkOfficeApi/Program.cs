using Hellang.Middleware.ProblemDetails;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;
using TinyHelpers.Json.Serialization;
using WorkOfficeApi.DataAccessLayer;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((hostingContext, logger) =>
{
    logger.ReadFrom.Configuration(hostingContext.Configuration);
});

builder.Services.AddMemoryCache();
builder.Services.AddMapperProfiles();
builder.Services.AddValidators();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen()
    .AddFluentValidationRulesToSwagger(options =>
    {
        options.SetNotNullableIfMinLengthGreaterThenZero = true;
    });

string connectionString = builder.Configuration.GetConnectionString("SqlConnection");
builder.Services.AddSqlServer<DataContext>(connectionString, sqlServerOptionsAction: options =>
{
    int maxRetryCount = 10;
    TimeSpan maxRetryDelay = TimeSpan.FromSeconds(1);
    options.EnableRetryOnFailure(maxRetryCount, maxRetryDelay, null);
});
builder.Services.AddScoped<IReadOnlyDataContext>(services =>
{
    return services.GetRequiredService<DataContext>();
});
builder.Services.AddScoped<IDataContext>(services =>
{
    return services.GetRequiredService<DataContext>();
});

builder.Services.AddProblemDetails(options =>
{
    options.Map<ArgumentException>(ex =>
    {
        if (ex is ArgumentNullException)
        {
            return new StatusCodeProblemDetails(StatusCodes.Status404NotFound)
            {
                Title = "Entity not found"
            };
        }

        return new StatusCodeProblemDetails(StatusCodes.Status400BadRequest)
        {
            Title = "Entity can't be null"
        };
    });

    options.Map<OperationCanceledException>(ex =>
    {
        if (ex is TaskCanceledException)
        {
            return new StatusCodeProblemDetails(StatusCodes.Status408RequestTimeout);
        }

        return new StatusCodeProblemDetails(StatusCodes.Status400BadRequest);
    });

    options.Map<NullReferenceException>(_ => new StatusCodeProblemDetails(StatusCodes.Status400BadRequest));
    options.Map<HttpRequestException>(_ => new StatusCodeProblemDetails(StatusCodes.Status408RequestTimeout));
    options.Map<InvalidOperationException>(_ => new StatusCodeProblemDetails(StatusCodes.Status400BadRequest));
    options.Map<NotImplementedException>(_ => new StatusCodeProblemDetails(StatusCodes.Status503ServiceUnavailable));
    options.Map<DbUpdateException>(_ => new StatusCodeProblemDetails(StatusCodes.Status500InternalServerError));
});

var app = builder.Build();
app.UseHttpsRedirection();
app.UseProblemDetails();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging(options =>
{
    options.IncludeQueryInRequestPath = true;
});
app.UseAuthorization();
app.MapControllers();
app.Run();