namespace System.Text.Json.Serialization
{
    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            string op = "-";
            if (value < TimeSpan.Zero) value = -value;
            else op = "";
            writer.WriteStringValue($"{op}{value.Days * 24 + value.Hours}:{value.Minutes:d2}:{value.Seconds:d2}.{value.Milliseconds:d3}");
        }
    }
}
