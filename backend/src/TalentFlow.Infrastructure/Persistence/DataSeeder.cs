using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TalentFlow.Domain.Entities;

namespace TalentFlow.Infrastructure.Persistence;

/// <summary>
/// Seeds initial data: roles and a default admin user.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
            // Apply pending migrations automatically
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying pending database migrations...");
                await dbContext.Database.MigrateAsync();
            }

            await SeedRolesAsync(serviceProvider, logger);
            await SeedAdminUserAsync(serviceProvider, logger);
            await SeedSampleDataAsync(serviceProvider, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    private static async Task SeedRolesAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var roles = new[] { "SystemAdmin", "Recruiter", "HiringManager", "Candidate", "Employee" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string adminEmail = "admin@talentflow.com";
        const string adminPassword = "Admin@123456";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            Email = adminEmail,
            UserName = adminEmail,
            FirstName = "System",
            LastName = "Admin",
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "SystemAdmin");
            logger.LogInformation("Created default admin user: {Email}", adminEmail);
        }
        else
        {
            logger.LogWarning("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedSampleDataAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed Sample Company & Departments
        if (!dbContext.Companies.Any())
        {
            var company = new Company
            {
                Name = "TechNova Solutions",
                Industry = "Software Development",
                Address = "San Francisco, CA",
                IsActive = true
            };
            dbContext.Companies.Add(company);
            await dbContext.SaveChangesAsync();

            var engineeringDept = new Department { Name = "Engineering", CompanyId = company.Id };
            var hrDept = new Department { Name = "Human Resources", CompanyId = company.Id };
            dbContext.Departments.AddRange(engineeringDept, hrDept);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Seeded company and departments.");

            // Seed Sample Jobs
            var job1 = new Job
            {
                CompanyId = company.Id,
                DepartmentId = engineeringDept.Id,
                Title = "Senior Backend Engineer",
                Description = "Looking for a seasoned backend engineer with C# and .NET Core experience.",
                EmploymentType = "Full Time",
                Location = "Remote",
                MinimumExperience = 5,
                VacancyCount = 2,
                ApplicationDeadline = DateTime.UtcNow.AddDays(30),
                Status = TalentFlow.Domain.Enums.JobStatus.Published,
                Requirements = new List<JobRequirement>
                {
                    new JobRequirement { Description = "Skill: c#", IsMandatory = true, Weight = 20 },
                    new JobRequirement { Description = "Skill: .net core", IsMandatory = true, Weight = 20 },
                    new JobRequirement { Description = "Minimum Education: Bachelor's Degree", IsMandatory = true, Weight = 10 },
                    new JobRequirement { Description = "Willing to work PST hours", IsMandatory = false, Weight = 5 }
                }
            };
            var job2 = new Job
            {
                CompanyId = company.Id,
                DepartmentId = hrDept.Id,
                Title = "Technical Recruiter",
                Description = "Join our HR team to help source top engineering talent.",
                EmploymentType = "Full Time",
                Location = "San Francisco, CA",
                MinimumExperience = 2,
                VacancyCount = 1,
                ApplicationDeadline = DateTime.UtcNow.AddDays(15),
                Status = TalentFlow.Domain.Enums.JobStatus.Draft
            };
            dbContext.Jobs.AddRange(job1, job2);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded sample jobs.");
            
            // Seed Sample Users (Recruiter & Candidate)
            if (await userManager.FindByEmailAsync("recruiter@technova.com") == null)
            {
                var recruiter = new ApplicationUser
                {
                    Email = "recruiter@technova.com",
                    UserName = "recruiter@technova.com",
                    FirstName = "Alice",
                    LastName = "Recruiter",
                    IsActive = true,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(recruiter, "Recruiter@123");
                await userManager.AddToRoleAsync(recruiter, "Recruiter");
                
                // Add Company Membership
                dbContext.CompanyMemberships.Add(new CompanyMembership 
                { 
                    CompanyId = company.Id, 
                    UserId = recruiter.Id, 
                    Role = "Recruiter" 
                });
                await dbContext.SaveChangesAsync();
            }

            if (await userManager.FindByEmailAsync("candidate@example.com") == null)
            {
                var candidate = new ApplicationUser
                {
                    Email = "candidate@example.com",
                    UserName = "candidate@example.com",
                    FirstName = "Bob",
                    LastName = "Applicant",
                    IsActive = true,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(candidate, "Candidate@123");
                await userManager.AddToRoleAsync(candidate, "Candidate");
                
                // Add Candidate Profile with explicit Skills and Experience
                var profile = new CandidateProfile
                {
                    UserId = candidate.Id,
                    Summary = "Experienced software engineer looking for new challenges.",
                    Phone = "+1 555-0100"
                };
                dbContext.CandidateProfiles.Add(profile);
                await dbContext.SaveChangesAsync();

                var csharpSkill = new Skill { Name = "c#" };
                var netCoreSkill = new Skill { Name = ".net core" };
                dbContext.Skills.AddRange(csharpSkill, netCoreSkill);
                await dbContext.SaveChangesAsync();

                dbContext.CandidateSkills.AddRange(
                    new CandidateSkill { CandidateProfileId = profile.Id, SkillId = csharpSkill.Id, YearsOfExperience = 5, ProficiencyLevel = "Expert" },
                    new CandidateSkill { CandidateProfileId = profile.Id, SkillId = netCoreSkill.Id, YearsOfExperience = 4, ProficiencyLevel = "Advanced" }
                );

                dbContext.CandidateExperience.Add(new CandidateExperience
                {
                    CandidateProfileId = profile.Id,
                    JobTitle = "Backend Engineer",
                    CompanyName = "Tech Corp",
                    StartDate = DateTime.UtcNow.AddYears(-5),
                    IsCurrent = true,
                    Description = "Built microservices using C# and .NET Core."
                });

                dbContext.CandidateEducation.Add(new CandidateEducation
                {
                    CandidateProfileId = profile.Id,
                    Institution = "State University",
                    Degree = "Bachelor's Degree",
                    FieldOfStudy = "Computer Science",
                    StartDate = DateTime.UtcNow.AddYears(-9),
                    EndDate = DateTime.UtcNow.AddYears(-5)
                });
                await dbContext.SaveChangesAsync();

                // Seed Application
                var application = new TalentFlow.Domain.Entities.Application
                {
                    JobId = job1.Id,
                    CandidateProfileId = profile.Id,
                    Status = TalentFlow.Domain.Enums.ApplicationStatus.Screening,
                    CoverLetter = "I am a senior backend engineer with 5 years of C# experience.",
                    SubmittedAt = DateTime.UtcNow
                };
                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync();
            }
            logger.LogInformation("Seeded sample recruiter, candidate, and application.");
        }
    }
}
