using JudgeWeb.Data;
using JudgeWeb.Domains.Problems.Portion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Domains.Problems
{
    public static class EntityTypeConfiguration
    {
        public static void AddProblemDomain<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.AddScoped<IExportProvider, KattisExportProvider>();
            services.AddScoped<KattisImportProvider>();
            services.AddScoped<XmlImportProvider>();

            IImportProvider.ImportServiceKinds = new Dictionary<string, Type>
            {
                ["kattis"] = typeof(KattisImportProvider),
                ["xysxml"] = typeof(XmlImportProvider),
            };

            services.AddScoped<IProblemViewProvider, MarkdownProblemViewProvider>();
            services.AddScoped<IProblemStore, EntityFrameworkCoreProblemStore<TContext>>();
        }

        public static void ApplyProblemDomain(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Executable>(entity =>
            {
                entity.HasKey(e => e.ExecId);

                entity.Property(e => e.ExecId)
                    .IsRequired()
                    .HasMaxLength(64)
                    .IsUnicode(false);

                entity.Property(e => e.Md5sum)
                    .IsRequired()
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.ZipFile)
                    .IsRequired()
                    .HasMaxLength(1 << 20);
            });

            modelBuilder.Entity<Language>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasMaxLength(16);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasMaxLength(32);

                entity.Property(e => e.FileExtension)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasMaxLength(32);

                entity.Property(e => e.CompileScript)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasMaxLength(64);

                entity.HasOne<Executable>()
                    .WithMany()
                    .HasForeignKey(e => e.CompileScript)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Problem>(entity =>
            {
                entity.HasKey(e => e.ProblemId);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(e => e.Source)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.RunScript)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasMaxLength(64);

                entity.HasOne<Executable>()
                    .WithMany()
                    .HasForeignKey(e => e.RunScript)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.CompareScript)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasMaxLength(64);

                entity.HasOne<Executable>()
                    .WithMany()
                    .HasForeignKey(e => e.CompareScript)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.ComapreArguments)
                    .IsUnicode(false)
                    .HasMaxLength(128);

                entity.Ignore(e => e.Archive);
            });

            modelBuilder.Entity<ProblemArchive>(entity =>
            {
                entity.HasKey(e => e.PublicId);

                entity.Property(e => e.PublicId)
                    .ValueGeneratedNever();

                entity.HasIndex(e => e.ProblemId);

                entity.HasOne<Problem>()
                    .WithMany(p => p.ArchiveCollection)
                    .HasForeignKey(e => e.ProblemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.TagName)
                    .IsRequired();

                entity.Ignore(e => e.Title);

                entity.Ignore(e => e.Source);

                entity.Ignore(e => e.Submitted);
            });

            modelBuilder.Entity<Testcase>(entity =>
            {
                entity.HasKey(e => e.TestcaseId);

                entity.HasIndex(e => e.ProblemId);

                entity.HasOne<Problem>()
                    .WithMany()
                    .HasForeignKey(e => e.ProblemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Md5sumInput)
                    .HasMaxLength(32)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(e => e.Md5sumOutput)
                    .HasMaxLength(32)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(e => e.Description)
                    .HasMaxLength(1 << 9)
                    .IsRequired();
            });
        }
    }
}
