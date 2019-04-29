using Microsoft.Extensions.Configuration;

namespace WebixKanban
{
    /// <summary>
    /// Dependancy injection container using .Net Core provider
    /// </summary>
    public class IoC
    {
        public static IConfiguration Configuration { get; set; }
    }
}
