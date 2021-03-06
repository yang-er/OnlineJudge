﻿using System;

namespace JudgeWeb.Domains.Contests
{
    public class ContestListModel : IComparable<ContestListModel>
    {
        public int ContestId { get; set; }

        public string Name { get; set; }

        public DateTimeOffset? StartTime { get; set; }

        public DateTimeOffset? EndTime { get; set; }

        public int RankingStrategy { get; set; }

        public bool IsPublic { get; set; }

        public int TeamCount { get; set; }

        public bool IsRegistered { get; set; }

        public bool OpenRegister { get; set; }

        public bool Gym { get; set; }

        private int stat = 0;

        public int GetState()
        {
            if (stat == 0)
            {
                if (!StartTime.HasValue)
                    stat = 1; // Not Scheduled
                else if (!EndTime.HasValue || EndTime.Value >= DateTimeOffset.Now)
                    stat = 2; // Running or Waiting
                else
                    stat = 3; // Ended
            }

            return stat;
        }

        public int CompareTo(ContestListModel other)
        {
            if (Gym != other.Gym)
            {
                // this is not ok!!
                return ContestId.CompareTo(other.ContestId);
            }
            else if (Gym)
            {
                if (!StartTime.HasValue && !other.StartTime.HasValue)
                    return ContestId.CompareTo(other.ContestId);
                else if (StartTime.HasValue && other.StartTime.HasValue)
                    return StartTime.Value.CompareTo(other.StartTime.Value);
                else
                    return StartTime.HasValue ? 1 : -1;
            }
            else
            {
                int t1 = GetState(), t2 = other.GetState();
                if (t1 != t2) return t1.CompareTo(t2);
                if (t1 == 1) return ContestId.CompareTo(other.ContestId);
                if (t1 == 2) return StartTime.Value.CompareTo(other.StartTime.Value);
                return other.StartTime.Value.CompareTo(StartTime.Value);
            }
        }
    }
}
