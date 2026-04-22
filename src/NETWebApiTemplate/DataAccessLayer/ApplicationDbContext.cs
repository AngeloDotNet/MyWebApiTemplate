using Microsoft.EntityFrameworkCore;

namespace NETWebApiTemplate.DataAccessLayer;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{ }