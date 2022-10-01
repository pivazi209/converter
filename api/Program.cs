using api.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var maxFileSize = builder.Configuration.GetValue<int>("maxFileSize");

            builder.Services.AddControllers();
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = maxFileSize;
            });

            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = maxFileSize;
            });

            builder.Services.AddSingleton<IMessageSender, RabbitPublisherService>();

            var app = builder.Build();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}