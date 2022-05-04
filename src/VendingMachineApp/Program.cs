// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VendingMachineApp;
using VendingMachineApp.OrderManagementAPIClient;

var host = CreateHostBuilder(args)
    .Build();

var sodaMachineService = host?.Services?.GetService<SodaMachine>();
if (sodaMachineService != null)
{
    await sodaMachineService.StartAsync();
}

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, builder) =>
        {
            builder.SetBasePath(Directory.GetCurrentDirectory());
        })
        .ConfigureServices((context, services) =>
        {
            var orderAPIBaseUrl = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build()
            .GetValue<string>("OrderAPIBaseUrl");

            //add your service registrations
            services.AddSingleton(typeof(SodaMachine));
            services.AddTransient<IOrderAPIClient>(o => new OrderAPIClient(orderAPIBaseUrl));
            services.AddLogging(configure => configure.AddConsole());
        });