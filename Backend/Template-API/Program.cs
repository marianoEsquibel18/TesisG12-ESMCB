namespace API
{
    public class Program
    {
        protected Program()
        {
        }
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            if (args.Contains("--seed"))
            {
                using var scope = host.Services.CreateScope();
                var seedController = ActivatorUtilities.CreateInstance<Controllers.SeedController>(scope.ServiceProvider);
                var result = seedController.SeedCompleto().GetAwaiter().GetResult();
                Console.WriteLine("[SEED CLI] Seed de 45 dias ejecutado exitosamente.");
                return;
            }

            host.Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(config => { config.UseStartup<Startup>(); });
    }
}