namespace JudgeWeb.Features
{
    public interface IMarkdownService
    {
        string Render(string source);

        void Render(string source, out string html, out string tree);
    }
}
