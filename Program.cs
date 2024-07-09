using last.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;




var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});


builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{

    //var configuration = ConfigurationOptions.Parse("rediss://red-cpqqd0tumphs73b0iorg:0sXuc3AKY38HIqpKhRKJk63oNE8lbRW5@oregon-redis.render.com:6379");
   //var configuration = ConfigurationOptions.Parse("localhost:6379");
     var configuration = ConfigurationOptions.Parse("red-cpqqd0tumphs73b0iorg:6379");
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddTransient<LawService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy"); 
app.UseAuthorization();
app.MapControllers();
app.Run();
