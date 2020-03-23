using JudgeWeb.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public interface IJudgementFacade
    {
        ILogger<IJudgementFacade> Logger { get; }

        IInternalErrorStore InternalErrorStore { get; }
        IJudgehostStore JudgehostStore { get; }
        IJudgingStore JudgingStore { get; }
        ISubmissionStore SubmissionStore { get; }
        IRejudgingStore RejudgingStore { get; }

        public IInternalErrorStore InternalErrors => InternalErrorStore;
        public IJudgehostStore Judgehosts => JudgehostStore;
        public IJudgingStore Judgings => JudgingStore;
        public ISubmissionStore Submissions => SubmissionStore;
        public IRejudgingStore Rejudgings => RejudgingStore;

        IConfigurationRegistry Configurations { get; }

        Task<List<ServerStatus>> GetJudgeQueueAsync(int? cid = null);

        Task<(int judgehosts, int internal_error)> GetJudgeStatusAsync();
    }
}
