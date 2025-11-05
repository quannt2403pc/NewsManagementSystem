using Microsoft.AspNetCore.SignalR;

namespace Backend2.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotificationToAll(string message, string? articleUrl = null)
        {
            // " một đối tượng chứa message và url (nếu có).
            await Clients.All.SendAsync("ReceiveNotification", new { message, articleUrl });
        }

        // (Optional) Xử lý khi client kết nối/ngắt kết nối
        public override async Task OnConnectedAsync()
        {
            // Có thể thêm logic ở đây nếu cần, ví dụ: log
            await base.OnConnectedAsync();
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Có thể thêm logic ở đây nếu cần
            await base.OnDisconnectedAsync(exception);
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        }
    }
}
