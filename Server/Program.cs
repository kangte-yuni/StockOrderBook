using Server.Hubs;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IOrderBookSimulator, OrderBookSimulator>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder
            .AllowAnyOrigin() // For testing purposes, allow any origin
            .AllowAnyMethod()
            .AllowAnyHeader());            
});

var app = builder.Build();

app.MapHub<OrderBookHub>("/orderbook");
app.UseCors("CorsPolicy");

app.Run();
