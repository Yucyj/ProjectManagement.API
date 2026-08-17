using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.API.Models;

namespace ProjectManagement.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<ProjectProgram> Programs { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<ProjectTask> Tasks { get; set; }
        public DbSet<Milestone> Milestones { get; set; }
        public DbSet<ChangeRequest> ChangeRequests { get; set; }
        public DbSet<ProjectMeeting> Meetings { get; set; }
        public DbSet<ChangeRequestComment> ChangeRequestComments { get; set; }

        public DbSet<Plan> Plans { get; set; }
        public DbSet<PlanMilestone> PlanMilestones { get; set; }
        public DbSet<PlanDeliverable> PlanDeliverables { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ProjectMember Composite Key
            modelBuilder.Entity<ProjectMember>()
                .HasKey(pm => new { pm.ProjectId, pm.UserId });

            modelBuilder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Portfolio Owner relationship
            modelBuilder.Entity<Portfolio>()
                .HasOne(p => p.Owner)
                .WithMany(u => u.Portfolios)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Program Manager relationship
            modelBuilder.Entity<ProjectProgram>()
                .HasOne(p => p.Manager)
                .WithMany()
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Project Relationships
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Manager)
                .WithMany()
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Portfolio)
                .WithMany(pt => pt.Projects)
                .HasForeignKey(p => p.PortfolioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Program)
                .WithMany(pr => pr.Projects)
                .HasForeignKey(p => p.ProgramId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Task Assignee relationship
            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.Assignee)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ChangeRequest relationships
            modelBuilder.Entity<ChangeRequest>()
                .HasOne(cr => cr.RequestedBy)
                .WithMany(u => u.RequestedChangeRequests)
                .HasForeignKey(cr => cr.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChangeRequest>()
                .HasOne(cr => cr.ApprovedBy)
                .WithMany(u => u.ApprovedChangeRequests)
                .HasForeignKey(cr => cr.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChangeRequest>()
                .HasOne(cr => cr.Project)
                .WithMany(p => p.ChangeRequests)
                .HasForeignKey(cr => cr.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectMeeting>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Meetings)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

          
            modelBuilder.Entity<Plan>()
                .HasMany(p => p.Milestones)
                .WithOne()
                .HasForeignKey(m => m.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Plan>()
                .HasMany(p => p.Deliverables)
                .WithOne()
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChangeRequestComment>()
                .HasOne(c => c.ChangeRequest)
                .WithMany()
                .HasForeignKey(c => c.ChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.ReplyToMessage)
                .WithMany()
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}