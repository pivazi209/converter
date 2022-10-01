namespace service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<RabbitConsumer>();
                })
                .Build();

            host.Run();
        }
    }
}