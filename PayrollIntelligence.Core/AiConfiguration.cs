namespace PayrollIntelligence.Core;

public class AiConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:11434"; // Ollama default
    public string ModelName { get; set; } = "llama3.2"; // or "mistral", "phi3", etc.
    public bool Enabled { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 60;
    public double Temperature { get; set; } = 0.7;
    public string SystemPrompt { get; set; } = @"You are a payroll comparison assistant. You do NOT calculate payroll. You do NOT validate payroll accuracy. You ONLY analyze and explain differences between two payroll periods.

Your role:
- Interpret payroll differences that are already present
- Group changes in a way that is easy to understand
- Highlight changes that matter to payroll admins and finance

Rules:
- Be factual and neutral
- Do not speculate beyond the data provided
- Do not assume errors
- Do not suggest corrections

Your output must be concise, structured, and business-friendly.";
    
    // Feature-specific user prompts
    public string PayrollComparisonPrompt { get; set; } = @"Given the payroll facts for two periods:

1. Determine the overall payroll direction.
2. Identify the most meaningful changes.
3. Explain the changes in a short narrative suitable for management.

Guidelines:
- Focus on relationships (e.g. gross vs headcount)
- Prioritize clarity over completeness
- Avoid technical payroll language

Output JSON only:
{
  ""direction"": ""increase"" | ""decrease"" | ""stable"",
  ""summary"": string,
  ""key_drivers"": [string],
  ""confidence_level"": ""high"" | ""medium"" | ""low""
}";

    public string PayrollDifferencesPrompt { get; set; } = @"Given employee-level payroll change facts:

1. Group employees by similar change patterns.
2. Give each group a short, descriptive title.
3. Explain what changed for each group in plain language.
4. Highlight which employees were affected.

Guidelines:
- Ignore employees with no meaningful changes.
- Focus on patterns, not individual calculations.
- Do not repeat summary-level insights.

Output JSON only:
{
  ""change_groups"": [
    {
      ""title"": string,
      ""description"": string,
      ""affected_employees"": [string],
      ""confidence_level"": ""high"" | ""medium"" | ""low""
    }
  ]
}";

    public string PayrollAnomalyPrompt { get; set; } = @"Given payroll risk signals:

1. Identify items that deserve human review.
2. Assign a review severity based on potential impact.
3. Explain why each item may require attention.

Guidelines:
- Severity reflects review priority, not error certainty.
- High severity should be rare.
- Avoid repeating explanations from other sections.

Output JSON only:
{
  ""review_items"": [
    {
      ""scope"": ""employee"" | ""payroll"",
      ""reference"": string,
      ""severity"": ""high"" | ""medium"" | ""low"",
      ""title"": string,
      ""explanation"": string,
      ""confidence_level"": ""high"" | ""medium"" | ""low""
    }
  ],
  ""overall_assessment"": string
}";

    public bool IsValid()
    {
        return Enabled && !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ModelName);
    }
}