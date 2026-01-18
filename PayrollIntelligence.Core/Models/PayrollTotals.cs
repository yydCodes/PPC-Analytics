using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// Payroll data models
public class PayrollTotals
{
    [JsonPropertyName("net")]
    public decimal Net { get; set; }
    
    [JsonPropertyName("cost")]
    public decimal Cost { get; set; }
    
    [JsonPropertyName("gross")]
    public decimal Gross { get; set; }
    
    [JsonPropertyName("employeeMtd")]
    public decimal EmployeeMtd { get; set; }
    
    [JsonPropertyName("employeeEpf")]
    public decimal EmployeeEpf { get; set; }
    
    [JsonPropertyName("employerEpf")]
    public decimal EmployerEpf { get; set; }
    
    [JsonPropertyName("employeeEis")]
    public decimal EmployeeEis { get; set; }
    
    [JsonPropertyName("employerEis")]
    public decimal EmployerEis { get; set; }
    
    [JsonPropertyName("employerHrdf")]
    public decimal EmployerHrdf { get; set; }
    
    [JsonPropertyName("employeeSocso")]
    public decimal EmployeeSocso { get; set; }
    
    [JsonPropertyName("employerSocso")]
    public decimal EmployerSocso { get; set; }
    
    [JsonPropertyName("zakat")]
    public decimal Zakat { get; set; }
    
    [JsonPropertyName("cp38")]
    public decimal Cp38 { get; set; }
}
