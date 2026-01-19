using PayrollIntelligence.Core;

namespace PayrollIntelligence.Tests.Helpers;

/// <summary>
/// Helper class for creating test payroll data
/// </summary>
public static class TestDataHelper
{
    public static PayrollData CreateSamplePayrollData(
        int employeeCount = 3,
        decimal grossPay = 10000m,
        decimal netPay = 8000m,
        decimal employerCost = 12000m)
    {
        var employees = new List<EmployeePayroll>();
        
        for (int i = 1; i <= employeeCount; i++)
        {
            employees.Add(new EmployeePayroll
            {
                EmployeeId = $"EMP{i:000}",
                EmployeeName = $"Employee {i}",
                EmployeeNumber = $"E{i:000}",
                StatutoryContribution = new StatutoryContribution
                {
                    Gross = grossPay / employeeCount,
                    Net = netPay / employeeCount,
                    EmployeeMtd = 500m,
                    EmployerEpf = 1200m,
                    EmployerSocso = 50m,
                    EmployerEis = 20m
                },
                LeavePayPayrollItem = new LeavePayPayrollItem { Amount = 0 },
                UnpaidLeavePayrollItems = new List<UnpaidLeavePayrollItem>()
            });
        }

        return new PayrollData
        {
            EmployeePayrolls = employees,
            Totals = new PayrollTotals
            {
                Gross = grossPay,
                Net = netPay,
                Cost = employerCost
            }
        };
    }

    public static PayrollData CreatePayrollWithLeaveAdjustments(int employeeCount = 2)
    {
        var data = CreateSamplePayrollData(employeeCount);
        
        // Add leave adjustments to first employee
        if (data.EmployeePayrolls?.Count > 0)
        {
            data.EmployeePayrolls[0].LeavePayPayrollItem = new LeavePayPayrollItem { Amount = 500m };
            data.EmployeePayrolls[0].UnpaidLeavePayrollItems = new List<UnpaidLeavePayrollItem>
            {
                new UnpaidLeavePayrollItem { Amount = 200m }
            };
        }

        return data;
    }

    public static PayrollData CreatePayrollWithNewEmployees(int existingCount = 2, int newCount = 1)
    {
        var data = CreateSamplePayrollData(existingCount);
        
        // Add new employees
        for (int i = 1; i <= newCount; i++)
        {
            data.EmployeePayrolls?.Add(new EmployeePayroll
            {
                EmployeeId = $"NEW{i:000}",
                EmployeeName = $"New Employee {i}",
                EmployeeNumber = $"N{i:000}",
                StatutoryContribution = new StatutoryContribution
                {
                    Gross = 5000m,
                    Net = 4000m,
                    EmployeeMtd = 250m,
                    EmployerEpf = 600m,
                    EmployerSocso = 25m,
                    EmployerEis = 10m
                },
                LeavePayPayrollItem = new LeavePayPayrollItem { Amount = 0 },
                UnpaidLeavePayrollItems = new List<UnpaidLeavePayrollItem>()
            });
        }

        // Update totals
        if (data.Totals != null)
        {
            data.Totals.Gross += newCount * 5000m;
            data.Totals.Net += newCount * 4000m;
            data.Totals.Cost += newCount * 6000m;
        }

        return data;
    }

    public static PayrollData CreatePayrollWithZeroPayEmployee()
    {
        var data = CreateSamplePayrollData(2);
        
        // Set first employee to zero pay
        if (data.EmployeePayrolls?.Count > 0)
        {
            data.EmployeePayrolls[0].StatutoryContribution = new StatutoryContribution
            {
                Gross = 0m,
                Net = 0m,
                EmployeeMtd = 0m,
                EmployerEpf = 0m,
                EmployerSocso = 0m,
                EmployerEis = 0m
            };
        }

        return data;
    }

    public static PayrollData CreatePayrollWithErrors()
    {
        var data = CreateSamplePayrollData(2);
        
        // Add error to first employee
        if (data.EmployeePayrolls?.Count > 0)
        {
            data.EmployeePayrolls[0].Error = new PayrollError
            {
                Message = "Error: Calculation warning detected"
            };
        }

        return data;
    }
}
