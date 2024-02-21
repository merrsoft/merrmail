using System.ComponentModel.DataAnnotations;
using Merrsoft.MerrMail.Application.Contracts;
using Merrsoft.MerrMail.Application.Services;
using Merrsoft.MerrMail.Domain.Options;
using Merrsoft.MerrMail.Infrastructure.Services;
using Serilog;
using Serilog.Events;

try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] - {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    Log.Information("Welcome to Merr Mail!");
    Log.Information("Configuring Services...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();
    builder.Services.AddHttpClient();

    builder.Services.AddHostedService<MerrMailWorker>();

    builder.Services.AddSingleton<IEmailApiService, GmailApiService>();

    #region Application Options
    
    builder.Services
        .AddOptions<EmailApiOptions>()
        .BindConfiguration($"{nameof(EmailApiOptions)}")
        .Validate(options => File.Exists(options.OAuthClientCredentialsFilePath),
            $"{nameof(EmailApiOptions.OAuthClientCredentialsFilePath)} does not exists")
        .Validate(options => Directory.Exists(options.AccessTokenDirectoryPath),
            $"{nameof(EmailApiOptions.AccessTokenDirectoryPath)} does not exists")
        .Validate(options =>
                new EmailAddressAttribute().IsValid(options.HostAddress),
            $"{nameof(EmailApiOptions.HostAddress)} is not a valid email")
        .ValidateOnStart();

    // TODO: Change [Required] to validating if data actually exists
    builder.Services
        .AddOptions<DataStorageOptions>()
        .BindConfiguration($"{nameof(DataStorageOptions)}")
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services
        .AddOptions<TensorFlowBindingOptions>()
        .BindConfiguration($"{nameof(TensorFlowBindingOptions)}")
        .Validate(options => File.Exists(options.PythonDllFilePath),
            $"{nameof(TensorFlowBindingOptions.PythonDllFilePath)} does not exists")
        .Validate(options => Directory.Exists(options.UniversalSentenceEncoderDirectoryPath),
            $"{nameof(TensorFlowBindingOptions.UniversalSentenceEncoderDirectoryPath)} does not exists")
        .ValidateOnStart();

    #endregion

    var host = builder.Build();
    
    Log.Information("Services Configured!");

    host.Run(); // Go to Application.Services.MerrMailWorker to see the background service that holds everything together
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(ex.ToString());
}
finally
{
    Log.Information("Stopping Merr Mail");
    Log.CloseAndFlush();
}