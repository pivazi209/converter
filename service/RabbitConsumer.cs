using PuppeteerSharp;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace service
{
    public class RabbitConsumer : BackgroundService
    {
        private readonly ILogger<RabbitConsumer> logger;
        private readonly string queueName;
        private readonly string uploadedPath;
        private readonly string convertedPath;

        private IConnection connection;
        private BrowserFetcher browserFetcher;
        private Browser browser;

        public RabbitConsumer(IConfiguration config, ILogger<RabbitConsumer> logger)
        {
            this.logger = logger;

            var rabbitHost = config.GetValue<string>("rabbitHost");
            var queueName = config.GetValue<string>("queueName");
            uploadedPath = config.GetValue<string>("uploadedPath");
            convertedPath = config.GetValue<string>("convertedPath");

            var factory = new ConnectionFactory() { HostName = rabbitHost };
            this.connection = factory.CreateConnection();
            this.queueName = queueName;

            browserFetcher = new BrowserFetcher();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await browserFetcher.DownloadAsync();
            browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });

            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += Consumer_Received;
            channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
        }

        private async void Consumer_Received(object? sender, BasicDeliverEventArgs e)
        {
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);

            var filePath = Path.Combine(uploadedPath, message);

            if (!File.Exists(filePath))
                return;

            using (var page = await browser.NewPageAsync())
            {
                await page.SetContentAsync(File.ReadAllText(filePath));
                var result = await page.GetContentAsync();
                await page.PdfAsync(Path.Combine(convertedPath, message + ".pdf"));
            }
        }

        public override void Dispose()
        {
            this.connection.Dispose();
            this.browser.Dispose();
            base.Dispose();
        }
    }
}