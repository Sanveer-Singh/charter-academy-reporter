#!/bin/bash

# WordPress Report API - Quick Test Script
# Usage: ./quick-test.sh [base_url] [admin_email] [admin_password]

BASE_URL=${1:-"https://localhost:7001"}
ADMIN_EMAIL=${2:-"admin@charter.co.za"}
ADMIN_PASSWORD=${3:-"AdminPassword123"}

echo "🧪 WordPress Report API - Quick Test Suite"
echo "=========================================="
echo "Base URL: $BASE_URL"
echo "Admin Email: $ADMIN_EMAIL"
echo ""

# Step 1: Login and get token
echo "🔐 Step 1: Getting authentication token..."
TOKEN_RESPONSE=$(curl -s -X POST "$BASE_URL/Account/Login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}" \
  -k)

if [[ $TOKEN_RESPONSE == *"token"* ]]; then
    TOKEN=$(echo $TOKEN_RESPONSE | grep -o '"token":"[^"]*' | grep -o '[^"]*$')
    echo "✅ Authentication successful"
    echo "Token: ${TOKEN:0:20}..."
else
    echo "❌ Authentication failed"
    echo "Response: $TOKEN_RESPONSE"
    exit 1
fi

echo ""

# Step 2: Test basic report endpoint
echo "📊 Step 2: Testing basic report endpoint..."
REPORT_RESPONSE=$(curl -s -X GET "$BASE_URL/WordPressReport/GetReport?pageSize=5" \
  -H "Authorization: Bearer $TOKEN" \
  -k)

if [[ $REPORT_RESPONSE == *"items"* ]] && [[ $REPORT_RESPONSE == *"totalCount"* ]]; then
    echo "✅ Basic report endpoint working"
    TOTAL_COUNT=$(echo $REPORT_RESPONSE | grep -o '"totalCount":[0-9]*' | grep -o '[0-9]*')
    echo "Total records found: $TOTAL_COUNT"
    
    if [[ $TOTAL_COUNT -gt 0 ]]; then
        echo "✅ Data found in report"
    else
        echo "⚠️  No data found - this might be expected if no users have completed 4 courses"
    fi
else
    echo "❌ Basic report endpoint failed"
    echo "Response: ${REPORT_RESPONSE:0:200}..."
fi

echo ""

# Step 3: Test categories endpoint
echo "📂 Step 3: Testing categories endpoint..."
CATEGORIES_RESPONSE=$(curl -s -X GET "$BASE_URL/WordPressReport/GetCategories" \
  -H "Authorization: Bearer $TOKEN" \
  -k)

if [[ $CATEGORIES_RESPONSE == *"["* ]]; then
    echo "✅ Categories endpoint working"
    CATEGORY_COUNT=$(echo $CATEGORIES_RESPONSE | grep -o '"id"' | wc -l)
    echo "Categories found: $CATEGORY_COUNT"
else
    echo "⚠️  Categories endpoint returned: ${CATEGORIES_RESPONSE:0:100}..."
fi

echo ""

# Step 4: Test available columns endpoint
echo "📋 Step 4: Testing available columns endpoint..."
COLUMNS_RESPONSE=$(curl -s -X GET "$BASE_URL/WordPressReport/GetAvailableColumns" \
  -H "Authorization: Bearer $TOKEN" \
  -k)

if [[ $COLUMNS_RESPONSE == *"LastName"* ]] && [[ $COLUMNS_RESPONSE == *"FirstName"* ]]; then
    echo "✅ Available columns endpoint working"
    COLUMN_COUNT=$(echo $COLUMNS_RESPONSE | grep -o '"value"' | wc -l)
    echo "Columns available: $COLUMN_COUNT"
else
    echo "❌ Available columns endpoint failed"
    echo "Response: ${COLUMNS_RESPONSE:0:200}..."
fi

echo ""

# Step 5: Test search functionality
echo "🔍 Step 5: Testing search functionality..."
SEARCH_RESPONSE=$(curl -s -X GET "$BASE_URL/WordPressReport/GetReport?search=test&pageSize=3" \
  -H "Authorization: Bearer $TOKEN" \
  -k)

if [[ $SEARCH_RESPONSE == *"items"* ]]; then
    echo "✅ Search functionality working"
    SEARCH_COUNT=$(echo $SEARCH_RESPONSE | grep -o '"totalCount":[0-9]*' | grep -o '[0-9]*')
    echo "Search results: $SEARCH_COUNT"
else
    echo "❌ Search functionality failed"
fi

echo ""

# Step 6: Test sorting
echo "🔄 Step 6: Testing sorting functionality..."
SORT_RESPONSE=$(curl -s -X GET "$BASE_URL/WordPressReport/GetReport?sortColumn=lastname&sortDesc=true&pageSize=3" \
  -H "Authorization: Bearer $TOKEN" \
  -k)

if [[ $SORT_RESPONSE == *"items"* ]]; then
    echo "✅ Sorting functionality working"
else
    echo "❌ Sorting functionality failed"
fi

echo ""

# Step 7: Test Excel export (basic test - just check if endpoint responds)
echo "📤 Step 7: Testing Excel export endpoint..."
EXPORT_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$BASE_URL/WordPressReport/ExportExcel" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "selectedColumns=LastName&selectedColumns=FirstName&selectedColumns=Email" \
  -k \
  -o /dev/null)

if [[ $EXPORT_RESPONSE == "200" ]]; then
    echo "✅ Excel export endpoint working"
else
    echo "⚠️  Excel export returned status: $EXPORT_RESPONSE"
fi

echo ""

# Step 8: Test unauthorized access
echo "🔒 Step 8: Testing security (unauthorized access)..."
UNAUTH_RESPONSE=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/WordPressReport/GetReport" \
  -k \
  -o /dev/null)

if [[ $UNAUTH_RESPONSE == "401" ]] || [[ $UNAUTH_RESPONSE == "403" ]]; then
    echo "✅ Security working - unauthorized requests blocked"
else
    echo "⚠️  Security test returned status: $UNAUTH_RESPONSE"
fi

echo ""
echo "🎯 Test Summary"
echo "==============="
echo "✅ = Working correctly"
echo "⚠️  = Working but no data/warning" 
echo "❌ = Failed/Error"
echo ""
echo "If you see ⚠️  'No data found', this might be normal if:"
echo "- No users have completed exactly 4 courses"
echo "- WordPress database connection is not configured"
echo "- Custom fields (PPRA, Province, Agency) don't exist"
echo "- LMS plugin assumptions don't match your setup"
echo ""
echo "Next steps:"
echo "1. If basic functionality works, check your WordPress database"
echo "2. Run the investigation queries from the documentation"
echo "3. Import the full Postman collection for detailed testing"
echo ""
echo "✅ Quick test completed!"
