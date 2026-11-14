using FluentValidation;
using FluentValidation.AspNetCore;
using ManualMate.Application.Interfaces;
using ManualMate.Application.Interfaces.Services;
using ManualMate.Application.Mapping;
using ManualMate.Application.Services;
using ManualMate.Application.Validators;
using ManualMate.Infrastructure.Messaging;
using ManualMate.Infrastructure.Presistence;
using ManualMate.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ManualMate
{
    public static class ServicesConfigurations
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ManualMateDbContext>(optionsAction =>
            {
                optionsAction.UseNpgsql(
                    configuration.GetConnectionString("ManualMateDbContext"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.UseVector();
                    });
            });
            // services.AddScoped<IApplicationDbContext, ManualMateDbContext>();
            services.AddScoped<IApplicationDbContext>(provider => 
                provider.GetRequiredService<ManualMateDbContext>());

            services.AddSingleton<IConnectionMultiplexer>(options => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<CreateItemDtoValidator>();

            services.AddScoped<IItemService, ItemService>();
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IEmbeddingService, VoyageAIEmbeddingService>();
            services.AddScoped<IFileProcessingService, FileProcessingService>();
            services.AddScoped<ILlmService, GeminiLlmService>();
            services.AddScoped<IQaService, QaService>();
            services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
            services.AddScoped<ITextChunker, TextChunker>();

            services.AddRabbitMqMessaging(configuration);

            services.AddAutoMapper(cfg => {}, typeof(MappingProfile));

            return services;
        }
    }
}
   