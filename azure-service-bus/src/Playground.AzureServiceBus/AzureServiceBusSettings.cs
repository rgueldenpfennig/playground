using System.ComponentModel.DataAnnotations;

namespace Playground.AzureServiceBus;

public class AzureServiceBusSettings
{
    [Required]
    public string? ConnectionString { get; set; }
}
