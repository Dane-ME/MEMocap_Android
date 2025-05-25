using WebRTC.Hubs.Hubs;
using System.Net;
using System.Net.Sockets;
using TCPping;
using WebRTC.Cores;

var builder = WebApplication.CreateBuilder(args);
//Replace or add the following code to fix the issue
string localIP = IPControl.GetDesktopIPAddress();
int port = 5000;
builder.WebHost.UseUrls($"http://{localIP}:{port}");
builder.Services.AddSignalR();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder
            .SetIsOriginAllowed(origin => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

app.UseRouting();
app.UseCors("CorsPolicy");
app.UseStaticFiles();
app.UseDefaultFiles();
app.MapHub<VideoHub>("/videoHub");
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Đã xảy ra lỗi server.");
    });
});
new Ping();
app.Run();

