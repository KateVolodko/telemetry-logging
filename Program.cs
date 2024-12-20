using System.Globalization;
using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.File(".logs/start-host-log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();
    
    //throw new Exception();
    
    builder.Host.UseSerilog();
    // Add services to the container.
    builder.Services.AddRazorPages();

    builder.Services.AddSerilog((hostingContext, loggerConfiguration) =>
        loggerConfiguration
            .Enrich.FromLogContext()
            .WriteTo.Elasticsearch([new Uri("http://localhost:9200")], opts =>
            {
                opts.DataStream = new DataStreamName("logs", "telemetry-logging", "demo");
                opts.BootstrapMethod = BootstrapMethod.Failure;
                opts.ConfigureChannel = channelOpts =>
                {
                    channelOpts.BufferOptions = new BufferOptions
                    {
                        ExportMaxConcurrency = 10
                    };
                };
            }, transport =>
            {
                transport.Authentication(new BasicAuthentication("elastic", "changeme")); // Basic Auth
                transport.OnRequestCompleted(d => Console.WriteLine($"es-req: {d.DebugInformation}"));
            })
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName).ReadFrom.Configuration(builder.Configuration));
    
    
    
    

    var app = builder.Build();
    

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapRazorPages();
    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}


