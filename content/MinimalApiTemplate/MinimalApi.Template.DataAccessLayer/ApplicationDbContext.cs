using Microsoft.EntityFrameworkCore;

namespace MinimalApi.Template.DataAccessLayer;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{ }