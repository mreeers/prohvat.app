using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.WebAPI.Hubs;

namespace ProhvatApp.WebAPI.Services;

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<ProhvatHub> _hubContext;

    public RealtimeNotificationService(IHubContext<ProhvatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(Guid userId, string title, string message, string? url = null)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
        {
            Title = title,
            Message = message,
            Url = url,
            CreatedAt = DateTime.UtcNow
        });
    }
}
