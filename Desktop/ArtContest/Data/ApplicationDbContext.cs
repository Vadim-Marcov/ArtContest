using Microsoft.EntityFrameworkCore;
using ArtContest.Models;

namespace ArtContest.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Contest> Contests { get; set; }
        public DbSet<ContestCategory> ContestCategories { get; set; }
        public DbSet<Stage> Stages { get; set; }
        public DbSet<Criteria1> Criteria1 { get; set; }
        public DbSet<Criteria2> Criteria2 { get; set; }
        public DbSet<ApplicationPeriod> ApplicationPeriods { get; set; }
        public DbSet<JudgingPeriod> JudgingPeriods { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<ModeratorLog> ModeratorLogs { get; set; }
        public DbSet<JuryAssessment> JuryAssessments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Login).HasColumnName("login");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.BirthDate).HasColumnName("birth_date");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.ProfilePhoto).HasColumnName("profile_photo");
                entity.Property(e => e.IdRole).HasColumnName("id_role");
                entity.Property(e => e.IdRegion).HasColumnName("id_region");

                entity.HasOne(d => d.Role)
                    .WithMany()
                    .HasForeignKey(d => d.IdRole)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Region)
                    .WithMany()
                    .HasForeignKey(d => d.IdRegion)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.RoleName).HasColumnName("role_name");
            });

            modelBuilder.Entity<Region>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.RegionName).HasColumnName("region_name");
            });

            modelBuilder.Entity<Stage>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.StageName).HasColumnName("stage_name");
            });

            modelBuilder.Entity<ContestCategory>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CategoryName).HasColumnName("category_name");
            });

            modelBuilder.Entity<Criteria1>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Criteria1Name).HasColumnName("criteria1_name");
            });

            modelBuilder.Entity<Criteria2>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Criteria2Name).HasColumnName("criteria2_name");
            });

            modelBuilder.Entity<ApplicationPeriod>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AppStartDate).HasColumnName("app_start_date");
                entity.Property(e => e.AppEndDate).HasColumnName("app_end_date");
            });

            modelBuilder.Entity<JudgingPeriod>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.JudStartDate).HasColumnName("jud_start_date");
                entity.Property(e => e.JudEndDate).HasColumnName("jud_end_date");
            });

            modelBuilder.Entity<Submission>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.SubmissionImage).HasColumnName("submission_image");
                entity.Property(e => e.SubmissionDate).HasColumnName("submission_date");
                entity.Property(e => e.AuthorDescription).HasColumnName("author_description");
                entity.Property(e => e.TotalScore).HasColumnName("total_score");
                entity.Property(e => e.IdUser).HasColumnName("id_user");
                entity.Property(e => e.IdContest).HasColumnName("id_contest");
                entity.Property(e => e.IdModLog).HasColumnName("id_mod_log");
            });

            modelBuilder.Entity<ModeratorLog>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.ModComment).HasColumnName("mod_comment");
                entity.Property(e => e.IdUser).HasColumnName("id_user");
                entity.Property(e => e.ResponseDate).HasColumnName("response_date");
            });

            modelBuilder.Entity<JuryAssessment>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Score1).HasColumnName("score_1");
                entity.Property(e => e.Score2).HasColumnName("score_2");
                entity.Property(e => e.JuryComment).HasColumnName("jury_comment");
                entity.Property(e => e.IdUser).HasColumnName("id_user");
                entity.Property(e => e.IdSubmission).HasColumnName("id_submission");
            });

            modelBuilder.Entity<Contest>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Rules).HasColumnName("rules");
                entity.Property(e => e.ContestImage).HasColumnName("contest_image");
                entity.Property(e => e.IdCategory).HasColumnName("id_category");
                entity.Property(e => e.IdStage).HasColumnName("id_stage");
                entity.Property(e => e.IdAppPeriod).HasColumnName("id_app_period");
                entity.Property(e => e.IdJudPeriod).HasColumnName("id_jud_period");
                entity.Property(e => e.IdCriteria1).HasColumnName("id_criteria1");
                entity.Property(e => e.IdCriteria2).HasColumnName("id_criteria2");

                entity.HasOne(d => d.Category)
                    .WithMany()
                    .HasForeignKey(d => d.IdCategory)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Stage)
                    .WithMany()
                    .HasForeignKey(d => d.IdStage)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ApplicationPeriod)
                    .WithMany()
                    .HasForeignKey(d => d.IdAppPeriod)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.JudgingPeriod)
                    .WithMany()
                    .HasForeignKey(d => d.IdJudPeriod)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Criteria1)
                    .WithMany()
                    .HasForeignKey(d => d.IdCriteria1)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Criteria2)
                    .WithMany()
                    .HasForeignKey(d => d.IdCriteria2)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
