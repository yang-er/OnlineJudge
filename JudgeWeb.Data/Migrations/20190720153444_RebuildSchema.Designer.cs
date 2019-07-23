﻿// <auto-generated />
using System;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JudgeWeb.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20190720153444_RebuildSchema")]
    partial class RebuildSchema
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("JudgeWeb.Data.AuditLog", b =>
                {
                    b.Property<int>("LogId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Comment")
                        .IsRequired();

                    b.Property<int>("ContestId");

                    b.Property<int>("EntityId");

                    b.Property<bool>("Resolved");

                    b.Property<DateTime>("Time");

                    b.Property<int>("Type");

                    b.Property<string>("UserName")
                        .IsRequired();

                    b.HasKey("LogId");

                    b.HasIndex("ContestId");

                    b.HasIndex("EntityId");

                    b.HasIndex("Resolved");

                    b.HasIndex("Type");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("JudgeWeb.Data.Clarification", b =>
                {
                    b.Property<int>("ClarificationId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("Answered");

                    b.Property<string>("Body")
                        .IsRequired();

                    b.Property<int>("Category");

                    b.Property<int>("ContestId");

                    b.Property<string>("JuryMember");

                    b.Property<int?>("ProblemId");

                    b.Property<int?>("Recipient");

                    b.Property<int?>("ResponseToId");

                    b.Property<int?>("Sender");

                    b.Property<DateTime>("SubmitTime");

                    b.HasKey("ClarificationId");

                    b.HasIndex("ContestId");

                    b.HasIndex("Recipient");

                    b.HasIndex("ResponseToId");

                    b.HasIndex("Sender");

                    b.ToTable("Clarifications");
                });

            modelBuilder.Entity("JudgeWeb.Data.Configure", b =>
                {
                    b.Property<int>("ConfigId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Category");

                    b.Property<string>("Description");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(128)
                        .IsUnicode(false);

                    b.Property<int>("Public");

                    b.Property<string>("Type");

                    b.Property<string>("Value")
                        .IsRequired()
                        .IsUnicode(false);

                    b.HasKey("ConfigId");

                    b.HasIndex("Name");

                    b.ToTable("Configures");
                });

            modelBuilder.Entity("JudgeWeb.Data.Contest", b =>
                {
                    b.Property<int>("ContestId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("BronzeMedal");

                    b.Property<DateTime?>("EndTime");

                    b.Property<DateTime?>("FreezeTime");

                    b.Property<int>("GoldMedal");

                    b.Property<bool>("IsPublic");

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<int>("RankingStrategy");

                    b.Property<int>("RegisterDefaultCategory");

                    b.Property<string>("ShortName")
                        .IsRequired();

                    b.Property<int>("SilverMedal");

                    b.Property<DateTime?>("StartTime");

                    b.Property<DateTime?>("UnfreezeTime");

                    b.HasKey("ContestId");

                    b.ToTable("Contests");
                });

            modelBuilder.Entity("JudgeWeb.Data.ContestProblem", b =>
                {
                    b.Property<int>("ContestId");

                    b.Property<int>("ProblemId");

                    b.Property<bool>("AllowJudge");

                    b.Property<bool>("AllowSubmit");

                    b.Property<string>("Color")
                        .IsRequired();

                    b.Property<int>("Rank");

                    b.Property<string>("ShortName")
                        .IsRequired()
                        .HasMaxLength(10)
                        .IsUnicode(false);

                    b.HasKey("ContestId", "ProblemId");

                    b.HasIndex("ProblemId");

                    b.HasIndex("Rank");

                    b.ToTable("ContestProblem");
                });

            modelBuilder.Entity("JudgeWeb.Data.Detail", b =>
                {
                    b.Property<int>("TestId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("ExecuteMemory");

                    b.Property<int>("ExecuteTime");

                    b.Property<int>("JudgingId");

                    b.Property<string>("OutputDiff")
                        .HasMaxLength(131072)
                        .IsUnicode(false);

                    b.Property<string>("OutputSystem")
                        .HasMaxLength(131072)
                        .IsUnicode(false);

                    b.Property<int>("Status");

                    b.Property<int>("TestcaseId");

                    b.HasKey("TestId");

                    b.HasIndex("JudgingId");

                    b.HasIndex("TestcaseId");

                    b.ToTable("Details");
                });

            modelBuilder.Entity("JudgeWeb.Data.Executable", b =>
                {
                    b.Property<string>("ExecId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(64)
                        .IsUnicode(false);

                    b.Property<string>("Description");

                    b.Property<string>("Md5sum")
                        .IsRequired()
                        .HasMaxLength(32)
                        .IsUnicode(false);

                    b.Property<string>("Type");

                    b.Property<byte[]>("ZipFile")
                        .IsRequired();

                    b.Property<int>("ZipSize");

                    b.HasKey("ExecId");

                    b.ToTable("Executable");
                });

            modelBuilder.Entity("JudgeWeb.Data.InternalError", b =>
                {
                    b.Property<int>("ErrorId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("ContestId");

                    b.Property<string>("Description")
                        .IsRequired();

                    b.Property<string>("Disabled")
                        .IsRequired();

                    b.Property<string>("JudgehostLog")
                        .IsRequired();

                    b.Property<int?>("JudgingId");

                    b.Property<int>("Status");

                    b.Property<DateTimeOffset>("Time");

                    b.HasKey("ErrorId");

                    b.HasIndex("Status");

                    b.ToTable("InternalErrors");
                });

            modelBuilder.Entity("JudgeWeb.Data.JudgeHost", b =>
                {
                    b.Property<int>("ServerId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("Active");

                    b.Property<DateTimeOffset>("PollTime");

                    b.Property<string>("ServerName")
                        .IsRequired()
                        .HasMaxLength(64)
                        .IsUnicode(false);

                    b.HasKey("ServerId");

                    b.HasIndex("ServerName");

                    b.ToTable("JudgeHosts");
                });

            modelBuilder.Entity("JudgeWeb.Data.Judging", b =>
                {
                    b.Property<int>("JudgingId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("Active");

                    b.Property<string>("CompileError")
                        .HasMaxLength(131072)
                        .IsUnicode(false);

                    b.Property<int>("ExecuteMemory");

                    b.Property<int>("ExecuteTime");

                    b.Property<bool>("FullTest");

                    b.Property<int>("RejudgeId");

                    b.Property<int>("ServerId");

                    b.Property<DateTimeOffset?>("StartTime");

                    b.Property<int>("Status");

                    b.Property<DateTimeOffset?>("StopTime");

                    b.Property<int>("SubmissionId");

                    b.HasKey("JudgingId");

                    b.HasIndex("RejudgeId");

                    b.HasIndex("ServerId");

                    b.HasIndex("Status");

                    b.HasIndex("SubmissionId");

                    b.ToTable("Judgings");
                });

            modelBuilder.Entity("JudgeWeb.Data.Language", b =>
                {
                    b.Property<int>("LangId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("AllowJudge");

                    b.Property<bool>("AllowSubmit");

                    b.Property<string>("CompileScript")
                        .IsRequired()
                        .HasMaxLength(64)
                        .IsUnicode(false);

                    b.Property<string>("ExternalId")
                        .IsRequired()
                        .HasMaxLength(16)
                        .IsUnicode(false);

                    b.Property<string>("FileExtension");

                    b.Property<string>("Name")
                        .IsRequired()
                        .IsUnicode(false);

                    b.Property<double>("TimeFactor");

                    b.HasKey("LangId");

                    b.HasIndex("CompileScript");

                    b.HasIndex("ExternalId");

                    b.ToTable("Languages");
                });

            modelBuilder.Entity("JudgeWeb.Data.News", b =>
                {
                    b.Property<int>("NewsId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("Active");

                    b.Property<byte[]>("Content");

                    b.Property<DateTime>("LastUpdate");

                    b.Property<byte[]>("Source");

                    b.Property<string>("Title");

                    b.Property<byte[]>("Tree");

                    b.HasKey("NewsId");

                    b.ToTable("News");
                });

            modelBuilder.Entity("JudgeWeb.Data.Problem", b =>
                {
                    b.Property<int>("ProblemId");

                    b.Property<string>("ComapreArguments")
                        .HasMaxLength(128)
                        .IsUnicode(false);

                    b.Property<bool>("CombinedRunCompare");

                    b.Property<string>("CompareScript")
                        .IsRequired()
                        .HasMaxLength(64)
                        .IsUnicode(false);

                    b.Property<int>("Flag");

                    b.Property<int>("MemoryLimit");

                    b.Property<string>("RunScript")
                        .IsRequired()
                        .HasMaxLength(64)
                        .IsUnicode(false);

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(256);

                    b.Property<int>("TimeLimit");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(128);

                    b.HasKey("ProblemId");

                    b.HasIndex("CompareScript");

                    b.HasIndex("RunScript");

                    b.ToTable("Problems");
                });

            modelBuilder.Entity("JudgeWeb.Data.RankCache", b =>
                {
                    b.Property<int>("ContestId");

                    b.Property<int>("TeamId");

                    b.Property<int>("PointsPublic");

                    b.Property<int>("PointsRestricted");

                    b.Property<int>("TotalTimePublic");

                    b.Property<int>("TotalTimeRestricted");

                    b.HasKey("ContestId", "TeamId");

                    b.ToTable("RankCache");
                });

            modelBuilder.Entity("JudgeWeb.Data.Rejudge", b =>
                {
                    b.Property<int>("RejudgeId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool?>("Applied");

                    b.Property<int>("ContestId");

                    b.Property<string>("Reason")
                        .IsRequired();

                    b.HasKey("RejudgeId");

                    b.HasIndex("ContestId");

                    b.ToTable("Rejudges");
                });

            modelBuilder.Entity("JudgeWeb.Data.ScoreCache", b =>
                {
                    b.Property<int>("ContestId");

                    b.Property<int>("TeamId");

                    b.Property<int>("ProblemId");

                    b.Property<bool>("FirstToSolve");

                    b.Property<bool>("IsCorrectPublic");

                    b.Property<bool>("IsCorrectRestricted");

                    b.Property<int>("PendingPublic");

                    b.Property<int>("PendingRestricted");

                    b.Property<double>("SolveTimePublic");

                    b.Property<double>("SolveTimeRestricted");

                    b.Property<int>("SubmissionPublic");

                    b.Property<int>("SubmissionRestricted");

                    b.HasKey("ContestId", "TeamId", "ProblemId");

                    b.ToTable("ScoreCache");
                });

            modelBuilder.Entity("JudgeWeb.Data.Submission", b =>
                {
                    b.Property<int>("SubmissionId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("Author");

                    b.Property<int>("CodeLength");

                    b.Property<int>("ContestId");

                    b.Property<string>("Ip")
                        .IsRequired()
                        .HasMaxLength(128)
                        .IsUnicode(false);

                    b.Property<int>("Language");

                    b.Property<int>("ProblemId");

                    b.Property<string>("SourceCode")
                        .IsRequired()
                        .HasMaxLength(131072)
                        .IsUnicode(false);

                    b.Property<DateTimeOffset>("Time");

                    b.HasKey("SubmissionId");

                    b.HasIndex("Author");

                    b.HasIndex("ContestId");

                    b.HasIndex("Language");

                    b.HasIndex("ProblemId");

                    b.ToTable("Submissions");
                });

            modelBuilder.Entity("JudgeWeb.Data.Team", b =>
                {
                    b.Property<int>("ContestId");

                    b.Property<int>("TeamId");

                    b.Property<int>("AffiliationId");

                    b.Property<int>("CategoryId");

                    b.Property<DateTime?>("RegisterTime");

                    b.Property<int>("Status");

                    b.Property<string>("TeamName")
                        .IsRequired()
                        .HasMaxLength(128);

                    b.Property<int>("UserId");

                    b.HasKey("ContestId", "TeamId");

                    b.HasIndex("AffiliationId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("Status");

                    b.HasIndex("UserId");

                    b.ToTable("Teams");
                });

            modelBuilder.Entity("JudgeWeb.Data.TeamAffiliation", b =>
                {
                    b.Property<int>("AffiliationId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ExternalId");

                    b.Property<string>("FormalName");

                    b.HasKey("AffiliationId");

                    b.HasIndex("ExternalId");

                    b.ToTable("TeamAffiliations");
                });

            modelBuilder.Entity("JudgeWeb.Data.TeamCategory", b =>
                {
                    b.Property<int>("CategoryId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Color")
                        .IsRequired();

                    b.Property<bool>("IsPublic");

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<int>("SortOrder");

                    b.HasKey("CategoryId");

                    b.HasIndex("IsPublic");

                    b.HasIndex("SortOrder");

                    b.ToTable("TeamCategories");
                });

            modelBuilder.Entity("JudgeWeb.Data.Testcase", b =>
                {
                    b.Property<int>("TestcaseId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(512);

                    b.Property<byte[]>("Input")
                        .IsRequired()
                        .HasMaxLength(33554432);

                    b.Property<int>("InputLength");

                    b.Property<bool>("IsSecret");

                    b.Property<string>("Md5sumInput")
                        .IsRequired()
                        .HasMaxLength(32)
                        .IsUnicode(false);

                    b.Property<string>("Md5sumOutput")
                        .IsRequired()
                        .HasMaxLength(32)
                        .IsUnicode(false);

                    b.Property<byte[]>("Output")
                        .IsRequired()
                        .HasMaxLength(4194304);

                    b.Property<int>("OutputLength");

                    b.Property<int>("Point");

                    b.Property<int>("ProblemId");

                    b.Property<int>("Rank");

                    b.HasKey("TestcaseId");

                    b.HasIndex("ProblemId");

                    b.ToTable("Testcases");
                });

            modelBuilder.Entity("JudgeWeb.Data.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NickName");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<int>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<int>("UserId");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("JudgeWeb.Data.Clarification", b =>
                {
                    b.HasOne("JudgeWeb.Data.Contest")
                        .WithMany()
                        .HasForeignKey("ContestId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("JudgeWeb.Data.ContestProblem", b =>
                {
                    b.HasOne("JudgeWeb.Data.Contest")
                        .WithMany()
                        .HasForeignKey("ContestId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("JudgeWeb.Data.Problem")
                        .WithMany()
                        .HasForeignKey("ProblemId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("JudgeWeb.Data.Detail", b =>
                {
                    b.HasOne("JudgeWeb.Data.Judging")
                        .WithMany()
                        .HasForeignKey("JudgingId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("JudgeWeb.Data.Testcase")
                        .WithMany()
                        .HasForeignKey("TestcaseId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("JudgeWeb.Data.Judging", b =>
                {
                    b.HasOne("JudgeWeb.Data.Submission")
                        .WithMany()
                        .HasForeignKey("SubmissionId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("JudgeWeb.Data.Language", b =>
                {
                    b.HasOne("JudgeWeb.Data.Executable")
                        .WithMany()
                        .HasForeignKey("CompileScript")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("JudgeWeb.Data.Problem", b =>
                {
                    b.HasOne("JudgeWeb.Data.Executable")
                        .WithMany()
                        .HasForeignKey("CompareScript")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("JudgeWeb.Data.Executable")
                        .WithMany()
                        .HasForeignKey("RunScript")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("JudgeWeb.Data.Submission", b =>
                {
                    b.HasOne("JudgeWeb.Data.Language")
                        .WithMany()
                        .HasForeignKey("Language")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("JudgeWeb.Data.Problem")
                        .WithMany()
                        .HasForeignKey("ProblemId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("JudgeWeb.Data.Team", b =>
                {
                    b.HasOne("JudgeWeb.Data.TeamAffiliation")
                        .WithMany()
                        .HasForeignKey("AffiliationId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("JudgeWeb.Data.TeamCategory")
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("JudgeWeb.Data.Contest")
                        .WithMany()
                        .HasForeignKey("ContestId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("JudgeWeb.Data.Testcase", b =>
                {
                    b.HasOne("JudgeWeb.Data.Problem")
                        .WithMany()
                        .HasForeignKey("ProblemId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<int>")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.HasOne("JudgeWeb.Data.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.HasOne("JudgeWeb.Data.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<int>")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("JudgeWeb.Data.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.HasOne("JudgeWeb.Data.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
