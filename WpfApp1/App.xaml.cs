﻿using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WpfApp1.Data;
using WpfApp1.Shared.DataAccess;
using WpfApp1.Shared.Interfaces;

namespace WpfApp1;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // Configure DbContext with SQLite
        services.AddDbContext<SnackDbContext>(options =>
            options.UseSqlite("Data Source=snacks.db"));

        // Register Services
        services.AddScoped<ISnackService, SnackService>();

        ServiceProvider = services.BuildServiceProvider();


        // Initialize and seed the database
        using (var scope = ServiceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SnackDbContext>();
            context.Database.EnsureCreated();
            DatabaseInitializer.SeedData(context);
        }
    }
}