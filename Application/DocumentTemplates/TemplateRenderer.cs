namespace Application.DocumentTemplates
{
    // Plain {{Token}} substitution -- no loop/conditional syntax in the template language.
    // Repeating sections (fee item rows, salary component rows, tax breakdown rows, ...) are
    // pre-rendered by the caller as HTML row fragments and passed in as a single token's value.
    public static class TemplateRenderer
    {
        public static string Render(string htmlTemplate, Dictionary<string, string> values)
        {
            var renderedHtml = htmlTemplate;

            foreach (var value in values)
            {
                var token = "{{" + value.Key + "}}";
                renderedHtml = renderedHtml.Replace(token, value.Value ?? string.Empty);
            }

            return renderedHtml;
        }
    }
}
