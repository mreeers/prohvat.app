using System;
using System.Collections.Generic;
using System.IO;
using MediatR;
using ProhvatApp.Application.Rides.DTOs;

namespace ProhvatApp.Application.Rides.Commands.UploadRideGpx;

public record UploadRideGpxResult(string GpxTrackPath, double TotalDistanceKm, List<GpxPointDto> Points);

public record UploadRideGpxCommand(
    Guid RideId,
    Guid UserId,
    Stream FileStream,
    string FileName,
    string ContentType
) : IRequest<UploadRideGpxResult>;
