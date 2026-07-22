using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Location.Queries;

public class CityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
}

public record GetCitiesQuery(string? SearchTerm) : IRequest<List<CityDto>>;

public class GetCitiesQueryHandler : IRequestHandler<GetCitiesQuery, List<CityDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCitiesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CityDto>> Handle(GetCitiesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Cities
            .Include(c => c.Region)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }

        return await query
            .OrderByDescending(c => c.Population) // Return biggest cities first
            .Take(50) // Limit to 50 results
            .Select(c => new CityDto
            {
                Id = c.Id,
                Name = c.Name,
                RegionName = c.Region.Name,
                CenterLat = c.CenterLat,
                CenterLng = c.CenterLng
            })
            .ToListAsync(cancellationToken);
    }
}
