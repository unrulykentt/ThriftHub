using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ThriftHub.Hubs
{
    public sealed class ThriftHubUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(
            HubConnectionContext connection)
        {
            return connection.User?
                .FindFirstValue(
                    ClaimTypes.NameIdentifier);
        }
    }
}