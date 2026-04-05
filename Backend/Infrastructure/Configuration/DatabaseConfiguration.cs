using System.ComponentModel.DataAnnotations;

namespace Backend.Infrastructure.Configuration;

public class DatabaseConfiguration
{
    [Required]
    public required string ConnectionString { get; set; }
}