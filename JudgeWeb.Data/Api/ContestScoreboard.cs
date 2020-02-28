using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JudgeWeb.Data.Api
{
    public class ContestScoreboard
    {
        public string event_id { get; set; }
        public DateTimeOffset time { get; set; }
        public TimeSpan contest_time { get; set; }
        public ContestTime state { get; set; }
        public IEnumerable<Row> rows { get; set; }

        public class Score
        {
            public int num_solved { get; set; }
            public int total_time { get; set; }

            public Score(int a, int b)
            {
                num_solved = a;
                total_time = b;
            }
        }

        [JsonConverter(typeof(ProblemConverter))]
        public class Problem
        {
            public string label { get; set; }
            public string problem_id { get; set; }
            public int num_judged { get; set; }
            public int num_pending { get; set; }
            public bool solved { get; set; }
            public bool first_to_solve { get; set; }
            public int time { get; set; }
        }

        private class ProblemConverter : JsonConverter<Problem>
        {
            public override Problem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => throw new InvalidOperationException();

            public override void Write(Utf8JsonWriter writer, Problem value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString(nameof(value.label), value.label);
                writer.WriteString(nameof(value.problem_id), value.problem_id);
                writer.WriteNumber(nameof(value.num_judged), value.num_judged);
                writer.WriteNumber(nameof(value.num_pending), value.num_pending);
                writer.WriteBoolean(nameof(value.solved), value.solved);

                if (value.solved)
                {
                    writer.WriteBoolean(nameof(value.first_to_solve), value.first_to_solve);
                    writer.WriteNumber(nameof(value.time), value.time);
                }

                writer.WriteEndObject();
            }
        }

        public class Row
        {
            public int rank { get; set; }
            public string team_id { get; set; }
            public Score score { get; set; }
            public IEnumerable<Problem> problems { get; set; }
        }
    }
}
