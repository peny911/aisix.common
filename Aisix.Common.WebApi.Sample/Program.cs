using Aisix.Common.Db;
using Aisix.Common.Db.Repository;
using Aisix.Common.Db.Service;
using Aisix.Common.Utils;
using Aisix.Common.WebApi.Sample.Data;
using Aisix.Common.WebApi.Sample.Services;
using SqlSugar;
using SqlSugar.IOC;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 配置 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Aisix.Common.WebApi.Sample", Version = "v1" });
});

// 配置数据库连接
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("数据库连接字符串未配置");
}

// 注册 SqlSugar 服务
builder.Services.AddSqlSugarIocSetup(builder.Configuration);

// 手动注册 SqlSugar 核心服务
//builder.Services.AddScoped<ISqlSugarClient>(sp => DbScoped.SugarScope);
//builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
//builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));

// 注册应用服务
builder.Services.AddScoped<AppDbContext>();
builder.Services.AddScoped<ITestService, TestService>();

var app = builder.Build();

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.InitializeDatabase();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aisix.Common.WebApi.Sample v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
