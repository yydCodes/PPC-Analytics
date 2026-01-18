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
                employeeId = $"EMP{i:000}",
                employeeName = $"Employee {i}",
                employeeNumber = $"E{i:000}",
                statutoryContribution = new StatutoryContribution
                {
                    gross = grossPay / employeeCount,
                    net = netPay / employeeCount,
                    employeeMtd = 500m,
                    employerEpf = 1200m,
                    employerSocso = 50m,
                    employerEis = 20m
                },
                leavePayPayrollItem = new LeavePayPayrollItem { amount = 0 },
                unpaidLeavePayrollItems = new List<UnpaidLeavePayrollItem>()
            });
        }

        return new PayrollData
        {
            employeePayrolls = employees,
            totals = new PayrollTotals
            {
                gross = grossPay,
                net = netPay,
                cost = employerCost
            }
        };
    }

    public static PayrollData CreatePayrollWithLeaveAdjustments(int employeeCount = 2)
    {
        var data = CreateSamplePayrollData(employeeCount);
        
        // Add leave adjustments to first employee
        if (data.employeePayrolls?.Count > 0)
        {
            data.employeePayrolls[0].leavePayPayrollItem = new LeavePayPayrollItem { amount = 500m };
            data.employeePayrolls[0].unpaidLeavePayrollItems = new List<UnpaidLeavePayrollItem>
            {
                new UnpaidLeavePayrollItem { amount = 200m }
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
            data.employeePayrolls?.Add(new EmployeePayroll
            {
                employeeId = $"NEW{i:000}",
                employeeName = $"New Employee {i}",
                employeeNumber = $"N{i:000}",
                statutoryContribution = new StatutoryContribution
                {
                    gross = 5000m,
                    net = 4000m,
                    employeeMtd = 250m,
                    employerEpf = 600m,
                    employerSocso = 25m,
                    employerEis = 10m
                },
                leavePayPayrollItem = new LeavePayPayrollItem { amount = 0 },
                unpaidLeavePayrollItems = new List<UnpaidLeavePayrollItem>()
            });
        }

        // Update totals
        if (data.totals != null)
        {
            data.totals.gross += newCount * 5000m;
            data.totals.net += newCount * 4000m;
            data.totals.cost += newCount * 6000m;
        }

        return data;
    }

    public static PayrollData CreatePayrollWithZeroPayEmployee()
    {
        var data = CreateSamplePayrollData(2);
        
        // Set first employee to zero pay
        if (data.employeePayrolls?.Count > 0)
        {
            data.employeePayrolls[0].statutoryContribution = new StatutoryContribution
            {
                gross = 0m,
                net = 0m,
                employeeMtd = 0m,
                employerEpf = 0m,
                employerSocso = 0m,
                employerEis = 0m
            };
        }

        return data;
    }

    public static PayrollData CreatePayrollWithErrors()
    {
        var data = CreateSamplePayrollData(2);
        
        // Add error to first employee
        if (data.employeePayrolls?.Count > 0)
        {
            data.employeePayrolls[0].error = new PayrollError
            {
                message = "Error: Calculation warning detected"
            };
        }

        return data;
    }
}
