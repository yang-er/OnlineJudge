using JudgeWeb.Features.Storage;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JudgeWeb.Data
{
    public class TestcaseManager
    {
        protected AppDbContext DbContext { get; }

        protected UserManager UserManager { get; }

        protected IFileRepository IoContext { get; }

        public TestcaseManager(AppDbContext adbc, UserManager um, IFileRepository io)
        {
            DbContext = adbc;
            UserManager = um;
            IoContext = io;
            io.SetContext("Problems");
        }

        public async Task<int> CreateAsync(int pid,
            (byte[], string) input, (byte[], string) output,
            bool isSecret, string description, ClaimsPrincipal user)
        {
            int rank = await DbContext.Testcases.CountAsync(a => a.ProblemId == pid);

            var tc = DbContext.Testcases.Add(new Testcase
            {
                ProblemId = pid,
                Rank = rank + 1,
                Description = description,
                IsSecret = isSecret,
                Md5sumInput = input.Item2,
                Md5sumOutput = output.Item2,
                InputLength = input.Item1.Length,
                OutputLength = output.Item1.Length,
            });

            await DbContext.SaveChangesAsync();

            int tcid = tc.Entity.TestcaseId;
            await IoContext.WriteBinaryAsync($"p{pid}", $"t{tcid}.in", input.Item1);
            await IoContext.WriteBinaryAsync($"p{pid}", $"t{tcid}.out", output.Item1);

            var logUserName = UserManager.GetUserName(user);
            if (logUserName != null)
            {
                DbContext.AuditLogs.Add(new AuditLog
                {
                    UserName = logUserName,
                    ContestId = 0,
                    Resolved = true,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Testcase,
                    Comment = $"added for p{pid} with rank {rank}",
                    EntityId = tcid,
                });

                await DbContext.SaveChangesAsync();
            }

            return tcid;
        }

        public Task<Testcase> GetAsync(int pid, int tcid)
        {
            return DbContext.Testcases
                .Where(t => t.TestcaseId == tcid && t.ProblemId == pid)
                .FirstOrDefaultAsync();
        }

        public async Task EditAsync(Testcase last,
            (byte[], string)? input, (byte[], string)? output,
            bool isSecret, string description, ClaimsPrincipal user)
        {
            int pid = last.ProblemId, tid = last.TestcaseId;

            if (input.HasValue)
            {
                last.Md5sumInput = input.Value.Item2;
                last.InputLength = input.Value.Item1.Length;
                await IoContext.WriteBinaryAsync($"p{pid}", $"t{tid}.in", input.Value.Item1);
            }

            if (output.HasValue)
            {
                last.Md5sumOutput = output.Value.Item2;
                last.OutputLength = output.Value.Item1.Length;
                await IoContext.WriteBinaryAsync($"p{pid}", $"t{tid}.out", output.Value.Item1);
            }

            last.Description = description ?? last.Description;
            last.IsSecret = isSecret;
            DbContext.Testcases.Update(last);

            DbContext.AuditLogs.Add(new AuditLog
            {
                UserName = UserManager.GetUserName(user),
                ContestId = 0,
                Resolved = true,
                Time = DateTimeOffset.Now,
                Type = AuditLog.TargetType.Testcase,
                Comment = "modified",
                EntityId = last.TestcaseId,
            });

            await DbContext.SaveChangesAsync();
        }

        public string Download(int pid, int tid, string type)
        {
            if (IoContext.ExistPart($"p{pid}", $"t{tid}.{type}"))
                return $"Problems/p{pid}/t{tid}.{type}";
            return null;
        }

        public Task<List<Testcase>> EnumerateAsync(int pid)
        {
            return DbContext.Testcases
                .Where(t => t.ProblemId == pid)
                .OrderBy(t => t.Rank)
                .ToListAsync();
        }
    }
}
