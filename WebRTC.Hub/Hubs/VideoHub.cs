using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace WebRTC.Hubs.Hubs;

public class VideoHub : Hub
{
    private static readonly ConcurrentDictionary<string, (string Role, string CameraId)> ConnectedClients = new(); private const string FixedCenterDeviceId = "center-device-fixed-id"; 

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectedClients.TryRemove(Context.ConnectionId, out var client))
        {
            if (client.Role == "camera")
            {
                Console.WriteLine($"Camera {Context.ConnectionId} with ID {client.CameraId} disconnected");
                await Clients.Group("CenterDevice").SendAsync("ClientDisconnected", Context.ConnectionId, client.CameraId);
            }
            else if (client.Role == "center")
            {
                Console.WriteLine($"Center Device {Context.ConnectionId} disconnected");
                await Clients.Group("CameraGroup").SendAsync("CenterDevicesUpdated", new[] { FixedCenterDeviceId });
            }
        }
        await base.OnDisconnectedAsync(exception);
    }
    
    public async Task RegisterAsCamera(string cameraId)
    {
        ConnectedClients[Context.ConnectionId] = ("camera", cameraId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "CameraGroup");
        Console.WriteLine($"Camera {Context.ConnectionId} registered with ID: {cameraId}");

        var cameras = ConnectedClients
            .Where(c => c.Value.Role == "camera")
            .Select(c => new { ConnectionId = c.Key, CameraId = c.Value.CameraId })
            .ToList();
        Console.WriteLine($"Sending CameraListUpdated to CenterDevice: {string.Join(", ", cameras.Select(c => c.CameraId))}");
        await Clients.Group("CenterDevice").SendAsync("CameraListUpdated", cameras);

        var centerDevices = ConnectedClients
            .Where(c => c.Value.Role == "center")
            .Select(c => c.Key)
            .ToList();
        if (centerDevices.Count == 0)
        {
            centerDevices = new List<string> { FixedCenterDeviceId };
            Console.WriteLine($"No real Center Device. Sending fixed center device to camera {Context.ConnectionId}: {FixedCenterDeviceId}");
        }
        else
        {
            Console.WriteLine($"Sending real center devices to camera {Context.ConnectionId}: {string.Join(", ", centerDevices)}");
        }
        await Clients.Client(Context.ConnectionId).SendAsync("CenterDevicesUpdated", centerDevices, Context.ConnectionId);
    }

    public async Task RegisterAsCenterDevice()
    {
        if (ConnectedClients.Any(x => x.Value.Role == "center"))
        {
            throw new HubException("A Center Device is already registered");
        }

        ConnectedClients[Context.ConnectionId] = ("center", string.Empty);
        await Groups.AddToGroupAsync(Context.ConnectionId, "CenterDevice");
        Console.WriteLine($"Center Device {Context.ConnectionId} registered");

        var cameras = ConnectedClients
            .Where(c => c.Value.Role == "camera")
            .Select(c => c.Key)
            .ToList();
        await Clients.Caller.SendAsync("Registered", Context.ConnectionId, cameras);

        var centerDevices = new List<string> { Context.ConnectionId };
        Console.WriteLine($"Sending real center device to CameraGroup: {Context.ConnectionId}");
        await Clients.Group("CameraGroup").SendAsync("CenterDevicesUpdated", centerDevices);
    }

    public async Task GetCenterDevices()
    {
        var centerDevices = ConnectedClients
            .Where(c => c.Value.Role == "center")
            .Select(c => c.Key)
            .ToList();
        if (centerDevices.Count == 0)
        {
            centerDevices = new List<string> { FixedCenterDeviceId };
            Console.WriteLine($"No real Center Device. Sending fixed center device to {Context.ConnectionId}: {FixedCenterDeviceId}");
        }
        else
        {
            Console.WriteLine($"Sending real center devices to {Context.ConnectionId}: {string.Join(", ", centerDevices)}");
        }
        await Clients.Caller.SendAsync("CenterDevicesUpdated", centerDevices);
    }

    public async Task SendSdp(string clientId, string sdp, string targetDeviceId)
    {
        if (!ConnectedClients.TryGetValue(Context.ConnectionId, out var client))
        {
            throw new HubException("Client not registered");
        }
        string role = client.Role;

        if (role == "camera" && targetDeviceId != FixedCenterDeviceId && !await IsCenterDevice(targetDeviceId))
        {
            throw new HubException("Invalid target device: not a Center Device");
        }
        if (role == "center" && !await IsCamera(targetDeviceId))
        {
            throw new HubException("Invalid target device: not a Camera");
        }

        Console.WriteLine($"Sending SDP from {clientId} to {targetDeviceId}: {sdp}");
        await Clients.Client(targetDeviceId).SendAsync("ReceiveSdp", clientId, sdp);
    }

    public async Task SendIceCandidate(string clientId, string candidate, string targetDeviceId)
    {
        if (!ConnectedClients.TryGetValue(Context.ConnectionId, out var client))
        {
            throw new HubException("Client not registered");
        }
        string role = client.Role;

        if (role == "camera" && targetDeviceId != FixedCenterDeviceId && !await IsCenterDevice(targetDeviceId))
        {
            throw new HubException("Invalid target device: not a Center Device");
        }
        if (role == "center" && !await IsCamera(targetDeviceId))
        {
            throw new HubException("Invalid target device: not a Camera");
        }

        Console.WriteLine($"Sending ICE candidate from {clientId} to {targetDeviceId}: {candidate}");
        await Clients.Client(targetDeviceId).SendAsync("ReceiveIceCandidate", clientId, candidate);
    }
    private async Task<bool> IsCenterDevice(string connectionId)
    {
        return ConnectedClients.TryGetValue(connectionId, out var client) && client.Role == "center";
    }

    private async Task<bool> IsCamera(string connectionId)
    {
        return ConnectedClients.TryGetValue(connectionId, out var client) && client.Role == "camera";
    }

    public async Task SetCodecPreference(string codec)
    {
        await Clients.Group("CameraGroup").SendAsync("UpdateCodec", codec);
    }
}