namespace DoyamoyeeMondir.Web.Models;
public class FinancialSummary
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal Income { get; set; }
    public decimal ApprovedExpenses { get; set; }
    public decimal PendingExpenses { get; set; }
    public decimal PriestShare { get; set; }
    public decimal StaffShare { get; set; }
    public List<SummaryRow> IncomeHeads { get; set; } = [];
    public List<SummaryRow> ExpenseHeads { get; set; } = [];
}
public record SummaryRow(string Name, decimal Amount);
public class DashboardSummary
{
    public decimal TodayIncome { get; set; }
    public decimal TodayExpenses { get; set; }
    public int MemberCount { get; set; }
    public int PendingCount { get; set; }
}
