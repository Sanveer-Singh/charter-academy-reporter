# WordPress Report API - Quick Test Script (PowerShell)
# Usage: .\quick-test.ps1 [-BaseUrl "https://localhost:7001"] [-AdminEmail "admin@charter.co.za"] [-AdminPassword "AdminPassword123"]

param(
    [string]$BaseUrl = "https://localhost:7001",
    [string]$AdminEmail = "admin@charter.co.za", 
    [string]$AdminPassword = "AdminPassword123"
)

Write-Host "🧪 WordPress Report API - Quick Test Suite" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host "Admin Email: $AdminEmail"
Write-Host ""

# Disable SSL certificate validation for testing
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
Add-Type -AssemblyName System.Net.Http

$httpClient = New-Object System.Net.Http.HttpClient

try {
    # Step 1: Login and get token
    Write-Host "🔐 Step 1: Getting authentication token..." -ForegroundColor Yellow
    
    $loginBody = @{
        email = $AdminEmail
        password = $AdminPassword
    } | ConvertTo-Json
    
    $loginContent = New-Object System.Net.Http.StringContent($loginBody, [System.Text.Encoding]::UTF8, "application/json")
    $loginResponse = $httpClient.PostAsync("$BaseUrl/Account/Login", $loginContent).Result
    $loginResult = $loginResponse.Content.ReadAsStringAsync().Result
    
    if ($loginResult -like "*token*") {
        $tokenObj = $loginResult | ConvertFrom-Json
        $token = $tokenObj.token
        Write-Host "✅ Authentication successful" -ForegroundColor Green
        Write-Host "Token: $($token.Substring(0, 20))..."
        
        # Set authorization header
        $httpClient.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
    }
    else {
        Write-Host "❌ Authentication failed" -ForegroundColor Red
        Write-Host "Response: $loginResult"
        exit 1
    }
    
    Write-Host ""
    
    # Step 2: Test basic report endpoint
    Write-Host "📊 Step 2: Testing basic report endpoint..." -ForegroundColor Yellow
    
    $reportResponse = $httpClient.GetAsync("$BaseUrl/WordPressReport/GetReport?pageSize=5").Result
    $reportResult = $reportResponse.Content.ReadAsStringAsync().Result
    
    if ($reportResult -like "*items*" -and $reportResult -like "*totalCount*") {
        Write-Host "✅ Basic report endpoint working" -ForegroundColor Green
        $reportObj = $reportResult | ConvertFrom-Json
        $totalCount = $reportObj.totalCount
        Write-Host "Total records found: $totalCount"
        
        if ($totalCount -gt 0) {
            Write-Host "✅ Data found in report" -ForegroundColor Green
        }
        else {
            Write-Host "⚠️  No data found - this might be expected if no users have completed 4 courses" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "❌ Basic report endpoint failed" -ForegroundColor Red
        Write-Host "Response: $($reportResult.Substring(0, [Math]::Min(200, $reportResult.Length)))..."
    }
    
    Write-Host ""
    
    # Step 3: Test categories endpoint
    Write-Host "📂 Step 3: Testing categories endpoint..." -ForegroundColor Yellow
    
    $categoriesResponse = $httpClient.GetAsync("$BaseUrl/WordPressReport/GetCategories").Result
    $categoriesResult = $categoriesResponse.Content.ReadAsStringAsync().Result
    
    if ($categoriesResult -like "*[*") {
        Write-Host "✅ Categories endpoint working" -ForegroundColor Green
        $categoriesObj = $categoriesResult | ConvertFrom-Json
        $categoryCount = $categoriesObj.Length
        Write-Host "Categories found: $categoryCount"
    }
    else {
        Write-Host "⚠️  Categories endpoint returned: $($categoriesResult.Substring(0, [Math]::Min(100, $categoriesResult.Length)))..." -ForegroundColor Yellow
    }
    
    Write-Host ""
    
    # Step 4: Test available columns endpoint  
    Write-Host "📋 Step 4: Testing available columns endpoint..." -ForegroundColor Yellow
    
    $columnsResponse = $httpClient.GetAsync("$BaseUrl/WordPressReport/GetAvailableColumns").Result
    $columnsResult = $columnsResponse.Content.ReadAsStringAsync().Result
    
    if ($columnsResult -like "*LastName*" -and $columnsResult -like "*FirstName*") {
        Write-Host "✅ Available columns endpoint working" -ForegroundColor Green
        $columnsObj = $columnsResult | ConvertFrom-Json
        $columnCount = $columnsObj.Length
        Write-Host "Columns available: $columnCount"
    }
    else {
        Write-Host "❌ Available columns endpoint failed" -ForegroundColor Red
        Write-Host "Response: $($columnsResult.Substring(0, [Math]::Min(200, $columnsResult.Length)))..."
    }
    
    Write-Host ""
    
    # Step 5: Test search functionality
    Write-Host "🔍 Step 5: Testing search functionality..." -ForegroundColor Yellow
    
    $searchResponse = $httpClient.GetAsync("$BaseUrl/WordPressReport/GetReport?search=test&pageSize=3").Result
    $searchResult = $searchResponse.Content.ReadAsStringAsync().Result
    
    if ($searchResult -like "*items*") {
        Write-Host "✅ Search functionality working" -ForegroundColor Green
        $searchObj = $searchResult | ConvertFrom-Json
        $searchCount = $searchObj.totalCount
        Write-Host "Search results: $searchCount"
    }
    else {
        Write-Host "❌ Search functionality failed" -ForegroundColor Red
    }
    
    Write-Host ""
    
    # Step 6: Test sorting
    Write-Host "🔄 Step 6: Testing sorting functionality..." -ForegroundColor Yellow
    
    $sortResponse = $httpClient.GetAsync("$BaseUrl/WordPressReport/GetReport?sortColumn=lastname&sortDesc=true&pageSize=3").Result
    $sortResult = $sortResponse.Content.ReadAsStringAsync().Result
    
    if ($sortResult -like "*items*") {
        Write-Host "✅ Sorting functionality working" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Sorting functionality failed" -ForegroundColor Red
    }
    
    Write-Host ""
    
    # Step 7: Test Excel export
    Write-Host "📤 Step 7: Testing Excel export endpoint..." -ForegroundColor Yellow
    
    $exportContent = New-Object System.Net.Http.FormUrlEncodedContent(@{
        "selectedColumns" = @("LastName", "FirstName", "Email")
    })
    
    $exportResponse = $httpClient.PostAsync("$BaseUrl/WordPressReport/ExportExcel", $exportContent).Result
    
    if ($exportResponse.StatusCode -eq "OK") {
        Write-Host "✅ Excel export endpoint working" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Excel export returned status: $($exportResponse.StatusCode)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    
    # Step 8: Test unauthorized access
    Write-Host "🔒 Step 8: Testing security (unauthorized access)..." -ForegroundColor Yellow
    
    $unauthClient = New-Object System.Net.Http.HttpClient
    $unauthResponse = $unauthClient.GetAsync("$BaseUrl/WordPressReport/GetReport").Result
    
    if ($unauthResponse.StatusCode -eq "Unauthorized" -or $unauthResponse.StatusCode -eq "Forbidden") {
        Write-Host "✅ Security working - unauthorized requests blocked" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Security test returned status: $($unauthResponse.StatusCode)" -ForegroundColor Yellow
    }
    
    $unauthClient.Dispose()
    
    Write-Host ""
    Write-Host "🎯 Test Summary" -ForegroundColor Cyan
    Write-Host "===============" -ForegroundColor Cyan
    Write-Host "✅ = Working correctly" -ForegroundColor Green
    Write-Host "⚠️  = Working but no data/warning" -ForegroundColor Yellow
    Write-Host "❌ = Failed/Error" -ForegroundColor Red
    Write-Host ""
    Write-Host "If you see ⚠️  'No data found', this might be normal if:" -ForegroundColor Yellow
    Write-Host "- No users have completed exactly 4 courses"
    Write-Host "- WordPress database connection is not configured" 
    Write-Host "- Custom fields (PPRA, Province, Agency) don't exist"
    Write-Host "- LMS plugin assumptions don't match your setup"
    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "1. If basic functionality works, check your WordPress database"
    Write-Host "2. Run the investigation queries from the documentation"
    Write-Host "3. Import the full Postman collection for detailed testing"
    Write-Host ""
    Write-Host "✅ Quick test completed!" -ForegroundColor Green

}
finally {
    $httpClient.Dispose()
}
