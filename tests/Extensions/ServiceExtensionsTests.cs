using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Quartz;
using WeatherApp.Data;
using WeatherApp.Extensions;
using WeatherApp.Services;
using WeatherApp.Services.AI;
using WeatherApp.Services.Background;
using WeatherApp.Services.Wrappers;
using Xunit;

namespace WeatherApp.Tests.Extensions;

public class ServiceExtensionsTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly ServiceCollection _services;

    public ServiceExtensionsTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _services = new ServiceCollection();
    }

    [Fact]
    public void AddDatabaseServices_ShouldRegisterDbContext()
    {
        // Arrange
        _mockConfig.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("Host=localhost;Database=test;");

        // Act
        _services.AddDatabaseServices(_mockConfig.Object);
        var provider = _services.BuildServiceProvider();

        // Assert
        var dbContext = provider.GetService<WeatherDbContext>();
        Assert.NotNull(dbContext);
    }

    [Fact]
    public void AddCoreServices_ShouldRegisterAllCoreServices()
    {
        // Act
        _services.AddCoreServices();
        var provider = _services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IWeatherService>());
        Assert.NotNull(provider.GetService<IEmailService>());
        Assert.NotNull(provider.GetService<ISmtpClientWrapper>());
        Assert.NotNull(provider.GetService<IPromptService>());
    }

    [Fact]
    public void AddCoreServices_ShouldRegisterWeatherServiceAsScoped()
    {
        // Act
        _services.AddCoreServices();

        // Assert
        var serviceDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(IWeatherService));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor!.Lifetime);
    }

    [Fact]
    public void AddCoreServices_ShouldRegisterEmailServiceAsTransient()
    {
        // Act
        _services.AddCoreServices();

        // Assert
        var serviceDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(IEmailService));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Transient, serviceDescriptor!.Lifetime);
    }

    [Fact]
    public void AddCoreServices_ShouldRegisterPromptServiceAsSingleton()
    {
        // Act
        _services.AddCoreServices();

        // Assert
        var serviceDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(IPromptService));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor!.Lifetime);
    }

    [Fact]
    public void AddAIProviders_ShouldRegisterAllAIProviders()
    {
        // Act
        _services.AddAIProviders();

        // Assert
        var aiProviders = _services.Where(s => s.ServiceType == typeof(IAIProvider)).ToList();
        Assert.Equal(5, aiProviders.Count); // Cerebras, Groq, DeepSeek, Mistral, Gemini
    }

    [Fact]
    public void AddAIProviders_ShouldRegisterAIService()
    {
        // Act
        _services.AddAIProviders();
        var provider = _services.BuildServiceProvider();

        // Assert
        var aiService = provider.GetService<IAIService>();
        Assert.NotNull(aiService);
    }

    [Fact]
    public void AddAIProviders_ShouldRegisterHttpClientsForProviders()
    {
        // Act
        _services.AddAIProviders();

        // Assert
        // HttpClient registrations are done via AddHttpClient<T>
        // We can verify the service collection contains these registrations
        var serviceDescriptors = _services.Where(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(IHttpClientFactory)).ToList();
        
        // AddHttpClient adds IHttpClientFactory registration
        Assert.NotEmpty(serviceDescriptors);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldRegisterQuartzServices()
    {
        // Act
        _services.AddBackgroundJobs();

        // Assert
        var schedulerFactory = _services.FirstOrDefault(s => s.ServiceType == typeof(ISchedulerFactory));
        Assert.NotNull(schedulerFactory);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldRegisterDailyWeatherJob()
    {
        // Act
        _services.AddBackgroundJobs();
        var provider = _services.BuildServiceProvider();

        // Assert - Job should be resolvable
        var job = provider.GetService<DailyWeatherJob>();
        Assert.NotNull(job);
    }

    [Fact]
    public void AddCorsPolicy_ShouldRegisterCorsPolicy()
    {
        // Act
        _services.AddCorsPolicy();
        var provider = _services.BuildServiceProvider();

        // Assert
        var corsService = provider.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsService>();
        Assert.NotNull(corsService);
    }

    [Fact]
    public void AddJwtAuthentication_ShouldRegisterAuthenticationServices()
    {
        // Arrange
        _mockConfig.Setup(c => c["Jwt:Key"]).Returns("test_secret_key_12345");

        // Act
        _services.AddJwtAuthentication(_mockConfig.Object);

        // Assert
        var authSchemeProvider = _services.FirstOrDefault(s => 
            s.ServiceType.Name.Contains("IAuthenticationSchemeProvider"));
        Assert.NotNull(authSchemeProvider);
    }

    [Fact]
    public void AddJwtAuthentication_ShouldUseDefaultKey_WhenConfigKeyIsNull()
    {
        // Arrange
        _mockConfig.Setup(c => c["Jwt:Key"]).Returns((string?)null);

        // Act
        _services.AddJwtAuthentication(_mockConfig.Object);
        var provider = _services.BuildServiceProvider();

        // Assert - Should not throw, uses default key
        var authSchemeProvider = provider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        Assert.NotNull(authSchemeProvider);
    }

    [Fact]
    public async Task AddJwtAuthentication_ShouldSetDefaultAuthenticationScheme()
    {
        // Arrange
        _mockConfig.Setup(c => c["Jwt:Key"]).Returns("test_key");

        // Act
        _services.AddJwtAuthentication(_mockConfig.Object);
        var provider = _services.BuildServiceProvider();

        // Assert
        var schemeProvider = provider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        Assert.NotNull(schemeProvider);
        
        var defaultScheme = await schemeProvider!.GetDefaultAuthenticateSchemeAsync();
        Assert.NotNull(defaultScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, defaultScheme!.Name);
    }

    [Fact]
    public void ExtensionMethods_ShouldReturnServiceCollection_ForChaining()
    {
        // Arrange
        _mockConfig.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("Host=localhost;");
        _mockConfig.Setup(c => c["Jwt:Key"]).Returns("test_key");

        // Act & Assert - Should allow method chaining
        var result = _services
            .AddCoreServices()
            .AddAIProviders()
            .AddCorsPolicy();

        Assert.Same(_services, result);
    }
}
