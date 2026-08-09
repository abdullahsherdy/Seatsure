using Microsoft.EntityFrameworkCore;
using Seatsure.DAL;

// familiarize
// entry point 
/*

void main(){

    return 0;
}
*/
// understand only 0 and 1 
// parsing, parse Tree (root) -> main  
namespace Seatsure
{
    public class Program
    {
        // args -> cli 
        // dotnet run --args 
        public static void Main(string[] args)
        {
            // webapplication builder -> create a web application 
            // define, announce compiler for this project type 
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddControllers();


            //// Register repositories
            //builder.Services.AddScoped<IUserRepository, UserRepository>();
            //builder.Services.AddScoped<IEventRepository, EventRepository>();
            //builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
            //builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

            // JWT tokens (defined durning imp)

            builder.Services.AddControllers(); 
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            // register JWT
            // Barear "key" 
            


            app.MapControllers();

            app.Run();
        }
    }
}
