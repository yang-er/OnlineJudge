using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Data
{
    public class SeedConfiguration :
        IEntityTypeConfiguration<Executable>,
        IEntityTypeConfiguration<Language>
    {
        public void Configure(EntityTypeBuilder<Language> entity)
        {
            entity.HasData(
                new Language { Id = "c", Name = "C", CompileScript = "c", FileExtension = "c", TimeFactor = 1, AllowJudge = true, AllowSubmit = true },
                new Language { Id = "cpp", Name = "C++", CompileScript = "cpp", FileExtension = "cpp", TimeFactor = 1, AllowJudge = true, AllowSubmit = true },
                new Language { Id = "java", Name = "Java", CompileScript = "java_javac_detect", FileExtension = "java", TimeFactor = 1, AllowJudge = true, AllowSubmit = true },
                new Language { Id = "py2", Name = "Python 2", CompileScript = "py2", FileExtension = "py", TimeFactor = 1, AllowJudge = true, AllowSubmit = true },
                new Language { Id = "py3", Name = "Python 3", CompileScript = "py3", FileExtension = "py3", TimeFactor = 1, AllowJudge = true, AllowSubmit = true },
                new Language { Id = "csharp", Name = "C#", CompileScript = "csharp", FileExtension = "cs", TimeFactor = 1, AllowSubmit = false, AllowJudge = true },
                new Language { Id = "f95", Name = "Fortran", CompileScript = "f95", FileExtension = "f95", TimeFactor = 1, AllowSubmit = false, AllowJudge = true },
                new Language { Id = "hs", Name = "Haskell", CompileScript = "hs", FileExtension = "hs", TimeFactor = 1, AllowSubmit = false, AllowJudge = true },
                new Language { Id = "kt", Name = "Kotlin", CompileScript = "kt", FileExtension = "kt", TimeFactor = 1, AllowSubmit = false, AllowJudge = true },
                new Language { Id = "pas", Name = "Pascal", CompileScript = "pas", FileExtension = "pas", TimeFactor = 1, AllowSubmit = false, AllowJudge = true },
                new Language { Id = "pl", Name = "Perl", CompileScript = "pl", FileExtension = "pl", TimeFactor = 1, AllowSubmit = false, AllowJudge = true });
        }

        public void Configure(EntityTypeBuilder<Executable> entity)
        {
            var executables = new List<Executable>();

            const string prefix = "JudgeWeb.Data.Seeds.Executables.";
            var assembly = typeof(SeedConfiguration).Assembly;
            foreach (var fileName in assembly.GetManifestResourceNames())
            {
                if (!fileName.StartsWith(prefix)) continue;
                using var stream = assembly.GetManifestResourceStream(fileName);
                var count = new byte[stream.Length];
                int len2 = stream.Read(count, 0, count.Length);
                if (len2 != count.Length) throw new IndexOutOfRangeException();
                var file = fileName[prefix.Length..(fileName.Length - 4)];
                var type = file;
                var description = $"default {file} script";

                if (file.StartsWith("compile."))
                {
                    type = "compile";
                    file = file[8..];
                    if (file == "java_javac")
                        description = "compiler for java";
                    else if (file == "java_javac_detect")
                        description = "compiler for java with class name detect";
                    else
                        description = "compiler for " + file;
                }

                executables.Add(new Executable
                {
                    Description = description,
                    ExecId = file,
                    Md5sum = count.ToMD5().ToHexDigest(true),
                    ZipSize = count.Length,
                    ZipFile = count,
                    Type = type,
                });
            }

            entity.HasData(executables);
        }
    }
}
