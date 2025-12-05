using ChatServer;
using ChatServer.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ChatContext>(options =>
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging()); 

// 2. Thêm dịch vụ khác
builder.Services.AddControllers();

// Cấu hình giới hạn upload file lớn (ví dụ: 500MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000; // 500 MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000; // 500 MB
});

// Tăng Timeout cho SignalR để tránh disconnect sớm
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.EnableDetailedErrors = true;
    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(15); // Ping mỗi 15s
    hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(2); // Chờ 2 phút mới disconnect
    hubOptions.MaximumReceiveMessageSize = 1024 * 1024; // 1MB cho tin nhắn signalR (text), file upload qua API riêng
});

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(origin => true);
    });
});

var app = builder.Build();

// 3. Tự động tạo Database và Cập nhật Schema
using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatContext>();
    dbContext.Database.EnsureCreated(); 
    
    // FIX: Tự động thêm cột mới vào bảng Messages nếu chưa có
    // (Vì EnsureCreated không tự update bảng cũ đã tồn tại)
    try 
    {
        dbContext.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Messages]') AND name = 'Type')
            BEGIN
                ALTER TABLE [Messages] ADD [Type] int NOT NULL DEFAULT 0;
            END
            
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Messages]') AND name = 'AttachmentName')
            BEGIN
                ALTER TABLE [Messages] ADD [AttachmentName] nvarchar(max) NULL;
            END
        ");
        Console.WriteLine("✅ Đã cập nhật Schema Database thành công.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("⚠️ Lỗi cập nhật schema (có thể bỏ qua nếu đã đủ cột): " + ex.Message);
    }
}

// 4. Cấu hình pipeline
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run("http://0.0.0.0:5000");
