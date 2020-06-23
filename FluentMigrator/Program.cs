using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Mono.Options;
using Polly;

namespace FluentMigrator
{
    /// <summary>
    ///     Represents the entry point for the application.
    /// </summary>
    public static class Program
    {
        private static OptionSet Options { get; } = new OptionSet
        {
            { "h|?|help", "Prints this message.", v => ShowHelp() },
            { "c|conn|connectionString=", v => ConnectionString = v }
        };

        private static string ConnectionString { get; set; }

        /// <summary>
        ///     Builds and runs a database migration.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args)
        {
            try
            {
                Options.Parse(args);

                if (string.IsNullOrWhiteSpace(ConnectionString))
                {
                    throw new OptionException("Connection string is required.", "connectionString");
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine();
                ShowHelp();

                Environment.Exit(-1);
            }

            var serviceProvider = CreateServices();

            RunMigrationWithRetries(serviceProvider);

            Console.WriteLine("Migrations completed successfully. If there were no migrations to run, then there will be no other output.");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage: *.exe [OPTIONS]+");
            Console.WriteLine($"Example: {Assembly.GetEntryAssembly()?.GetName().Name}.exe --connectionString \"Data Source=test.db\"");
            Console.WriteLine();

            Options.WriteOptionDescriptions(Console.Out);
        }

        private static IServiceProvider CreateServices()
        {
            return new ServiceCollection()
                   .AddFluentMigratorCore()
                   .ConfigureRunner(
                       rb => rb.AddSqlServer()
                               .WithGlobalConnectionString(ConnectionString)
                               .ScanIn(typeof(Program).Assembly)
                               .For.EmbeddedResources()
                               .For.Migrations())
                   .AddLogging(lb => lb.AddFluentMigratorConsole())
                   .BuildServiceProvider(false);
        }

        private static void RunMigrationWithRetries(IServiceProvider serviceProvider)
        {
            const int BackoffAttemptInMinutes1 = 2;
            const int BackoffAttemptInMinutes2 = 5;
            const int BackoffAttemptInMinutes3 = 10;
            const string ServerPausedExceptionMessageSubstring = "Data Provider error 6";

            Policy.Handle<SqlException>(
                      ex => new SqlDatabaseTransientErrorDetectionStrategy().IsTransient(ex))
                  .Or<InvalidOperationException>(
                      ex => ex.Message.IndexOf(ServerPausedExceptionMessageSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                  .WaitAndRetry(
                      new List<TimeSpan>
                      {
                          TimeSpan.FromMinutes(BackoffAttemptInMinutes1),
                          TimeSpan.FromMinutes(BackoffAttemptInMinutes2),
                          TimeSpan.FromMinutes(BackoffAttemptInMinutes3)
                      },
                      (exception, timeSpan, retryAttempt, context) =>
                      {
                          Console.WriteLine($"Failed to migrate database on attempt {retryAttempt}.");
                          Console.WriteLine($"Migration will be re-attempted in {timeSpan.TotalMinutes} minutes at {DateTime.UtcNow.Add(timeSpan)} UTC.");
                          Console.WriteLine();
                          Console.WriteLine($"Exception:\r\n{exception}.");
                      })
                  .Execute(
                      () =>
                      {
                          using (var scope = serviceProvider.CreateScope())
                          {
                              RunMigration(scope.ServiceProvider);
                          }
                      });
        }

        private static void RunMigration(IServiceProvider serviceProvider)
        {
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp();
        }
    }
}
