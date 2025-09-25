using Charter.Reporter.Application.Services.Dashboard;
using Charter.Reporter.Shared.Export;
using OfficeOpenXml;

namespace Charter.Reporter.Infrastructure.Services.Export;

public interface IExcelExportService
{
    Task<byte[]> GenerateExcelAsync(
        IReadOnlyList<MoodleReportRow> data,
        IReadOnlyList<string> selectedColumns,
        string fileName,
        CancellationToken cancellationToken = default);
}

public class ExcelExportService : IExcelExportService
{
    public async Task<byte[]> GenerateExcelAsync(
        IReadOnlyList<MoodleReportRow> data,
        IReadOnlyList<string> selectedColumns,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Set EPPlus license context for non-commercial use
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            // Define column mappings
            var columnMappings = new Dictionary<string, (string DisplayName, Func<MoodleReportRow, object?> ValueSelector)>
            {
                ["LastName"] = ("Last Name", r => r.LastName),
                ["FirstName"] = ("First Name", r => r.FirstName),
                ["Email"] = ("Email", r => r.Email),
                ["PhoneNumber"] = ("Phone Number", r => r.PhoneNumber),
                ["PpraNo"] = ("PPRA No", r => r.PpraNo),
                ["IdNo"] = ("ID No", r => r.IdNo),
                ["Province"] = ("Province", r => r.Province),
                ["Agency"] = ("Agency", r => r.Agency),
                ["CourseName"] = ("Course Name", r => r.CourseName),
                ["Category"] = ("Category", r => r.Category),
                ["EnrolmentDate"] = ("Enrolment Date", r => r.EnrolmentDate),
                ["CompletionDate"] = ("Completion Date", r => r.CompletionDate),
                ["FourthCompletionDate"] = ("4th Completion Date", r => r.FourthCompletionDate)
            };

            // Filter to only selected columns that exist in our mappings
            var validColumns = selectedColumns
                .Where(c => columnMappings.ContainsKey(c))
                .ToList();

            if (!validColumns.Any())
            {
                // If no valid columns selected, include all columns
                validColumns = columnMappings.Keys.ToList();
            }

            // Create Excel package
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Moodle Report");

            // Add headers
            for (int i = 0; i < validColumns.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = columnMappings[validColumns[i]].DisplayName;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Add data rows
            for (int row = 0; row < data.Count; row++)
            {
                var item = data[row];
                for (int col = 0; col < validColumns.Count; col++)
                {
                    var column = validColumns[col];
                    var value = columnMappings[column].ValueSelector(item);
                    
                    // Handle different data types appropriately
                    if (value is DateTime dt)
                    {
                        worksheet.Cells[row + 2, col + 1].Value = dt;
                        worksheet.Cells[row + 2, col + 1].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                    }
                    else
                    {
                        worksheet.Cells[row + 2, col + 1].Value = value?.ToString() ?? "";
                    }
                }
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            // Return the Excel file as byte array
            return await package.GetAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate Excel export: {ex.Message}", ex);
        }
    }
}
