using System;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Common.Interfaces;

public interface IRealtimeNotificationService
{
    Task SendNotificationAsync(Guid userId, string title, string message, string? url = null);
}
