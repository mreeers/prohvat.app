using System;
using System.Collections.Generic;

namespace ProhvatApp.Domain.Entities;

public class Region
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    
    public List<City> Cities { get; set; } = new();
}
