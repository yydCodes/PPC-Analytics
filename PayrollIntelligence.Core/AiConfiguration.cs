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

Currency handling rules:
- Use only the currency provided in the input data.
- Do NOT infer, convert, normalize, or substitute currencies.
- Do NOT introduce currency symbols or names not present in the input.
- When mentioning amounts, reference them exactly as given.
- If currency is not explicitly provided, refer to amounts with ""RM"" as the currency.

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
5. Explain why each change group matters to payroll admins and finance reviewers.

Guidelines:
- Ignore employees with no meaningful changes.
- Focus on patterns, not individual calculations.
- Do not repeat summary-level insights.
- ""Why it matters"" should explain operational or informational relevance, not risk or error.

Grouping constraint:
- Prefer a small number of meaningful change groups.
- Aim for 3–6 groups maximum.
- If more patterns exist, merge similar ones into broader groups.

Group employees based on the primary driver of change,
not minor secondary differences.

Primary drivers include:
- Salary structure changes
- Leave-related adjustments
- Deduction or statutory changes
- New or removed payroll inclusion
- Minor or mixed adjustments

If two groups differ only in magnitude or secondary components,
merge them into a single broader group.

Output JSON only:
{
  ""change_groups"": [
    {
      ""title"": string,
      ""description"": string,
      ""affected_employees"": [string],
      ""why_it_matters"": string,
      ""confidence_level"": ""high"" | ""medium"" | ""low""
    }
  ]
}";

    public string PayrollAnomalyPrompt { get; set; } = @"Given payroll risk signals:

1. Identify items that may deserve human review.
2. Assign a review severity based on potential impact.
3. Explain why each item stands out.
4. Suggest what should be reviewed to clarify the situation.

Anomaly classification rules:
You must classify each review item into exactly ONE of the following categories:
- Pay Consistency
- Leave
- Payroll Items
- Statutory
- Payroll Total
- Headcount

Do not create new categories.
Do not merge categories.

Severity assignment guidelines:

- HIGH:
  Changes that could significantly affect employee pay understanding,
  payroll accuracy perception, or compliance if incorrect.

- MEDIUM:
  Changes that are unusual but commonly explainable,
  or affect a limited scope.

- LOW:
  Changes that are expected, informational, or have minimal impact.

When uncertain between two levels, choose the LOWER severity.

Grouping rules:
- Group similar anomalies when the underlying signal pattern is the same.
- Do not split items solely due to different employees.
- Prefer fewer, clearer groups over many small ones.

Review suggestion guidelines:
- Suggestions must be optional and neutral.
- Do not recommend corrections or decisions.
- Phrase suggestions as verification or confirmation steps.
- Avoid urgency or alarmist language.

Output JSON only:
{
  ""review_items"": [
    {
      ""scope"": ""employee"" | ""payroll"",
      ""reference"": string,
      ""severity"": ""high"" | ""medium"" | ""low"",
      ""title"": string,
      ""explanation"": string,
      ""review_suggestion"": string,
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