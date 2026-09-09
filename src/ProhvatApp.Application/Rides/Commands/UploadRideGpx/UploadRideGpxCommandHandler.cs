using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Helpers;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Rides.Commands.UploadRideGpx;

public class UploadRideGpxCommandHandler : IRequestHandler<UploadRideGpxCommand, UploadRideGpxResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileService _fileService;

    public UploadRideGpxCommandHandler(IApplicationDbContext context, IFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    public async Task<UploadRideGpxResult> Handle(UploadRideGpxCommand request, CancellationToken cancellationToken)
    {
        var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken);
        if (ride == null)
        {
            throw new KeyNotFoundException($"Ride with id {request.RideId} not found");
        }

        if (ride.OrganizerId != request.UserId)
        {
            throw new UnauthorizedAccessException("Only the ride organizer can upload GPX tracks");
        }

        using var memoryStream = new MemoryStream();
        await request.FileStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var gpxXml = Encoding.UTF8.GetString(bytes);
        var (points, distanceKm) = GpxHelper.ParseGpx(gpxXml);

        using var uploadStream = new MemoryStream(bytes);
        var fileUrl = await _fileService.UploadFileAsync(uploadStream, request.FileName, request.ContentType);

        ride.GpxTrackPath = fileUrl;
        await _context.SaveChangesAsync(cancellationToken);

        return new UploadRideGpxResult(fileUrl, distanceKm, points);
    }
}
