using System.Text.Json.Serialization;

namespace PayrollIntelligence.Core;

// Payroll data models
public class PayrollTotals
{
    [JsonPropertyName("Net")]
    public decimal Net { get; set; }
    
    [JsonPropertyName("Cost")]
    public decimal Cost { get; set; }
    
    [JsonPropertyName("Gross")]
    public decimal Gross { get; set; }
    
    [JsonPropertyName("EmployeeMtd")]
    public decimal EmployeeMtd { get; set; }
    
    [JsonPropertyName("EmployeeEpf")]
    public decimal EmployeeEpf { get; set; }
    
    [JsonPropertyName("EmployerEpf")]
    public decimal EmployerEpf { get; set; }
    
    [JsonPropertyName("EmployeeEis")]
    public decimal EmployeeEis { get; set; }
    
    [JsonPropertyName("EmployerEis")]
    public decimal EmployerEis { get; set; }
    
    [JsonPropertyName("EmployerHrdf")]
    public decimal EmployerHrdf { get; set; }
    
    [JsonPropertyName("EmployeeSocso")]
    public decimal EmployeeSocso { get; set; }
    
    [JsonPropertyName("EmployerSocso")]
    public decimal EmployerSocso { get; set; }
    
    [JsonPropertyName("Zakat")]
    public decimal Zakat { get; set; }
    
    [JsonPropertyName("Cp38")]
    public decimal Cp38 { get; set; }
}
