﻿// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VendingMachineApp;
using VendingMachineApp.OrderManagementAPIClient;

var host = CreateHostBuilder(args).Build();
host.Services.GetService<SodaMachine>()?.Start();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, builder) =>
        {
            builder.SetBasePath(Directory.GetCurrentDirectory());
        })
        .ConfigureServices((context, services) =>
        {
            //add your service registrations
            services.AddSingleton(typeof(SodaMachine));
            services.AddTransient<IOrderAPIClient>(o => new OrderAPIClient("https://localhost:7167")); // TODO: get base url from app settings
            services.AddLogging(configure => configure.AddConsole());
        });