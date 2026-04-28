using Engine.Infrastructure;
using Engine.Infrastructure.Persistence.Mongo;
using Engine.Worker;
using Engine.Worker.Jobs;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

// MongoDB
var mongoConnStr = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoDbName = builder.Configuration["Mongo:DatabaseName"] ?? "groupwaredb";
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnStr));
builder.Services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDbName));
builder.Services.AddSingleton<GroupwareDbContext>();

// Add Infrastructure (Notification, Audit, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Jobs
builder.Services.AddScoped<ApprovalReminderJob>();
builder.Services.AddScoped<NotificationCleanupJob>();
builder.Services.AddScoped<AttendanceAutoCloseJob>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
