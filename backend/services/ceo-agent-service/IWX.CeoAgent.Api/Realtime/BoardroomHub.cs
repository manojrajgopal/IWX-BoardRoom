using Microsoft.AspNetCore.SignalR;

namespace IWX.CeoAgent.Realtime;

public sealed class BoardroomHub : Hub
{
    public Task Subscribe(string department) => Groups.AddToGroupAsync(Context.ConnectionId, $"dept:{department}");
}
