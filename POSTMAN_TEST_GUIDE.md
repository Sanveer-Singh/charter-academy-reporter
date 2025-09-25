# 🧪 WordPress Report API - Postman Test Guide

## 🚀 Quick Setup

### 1. Import Postman Collection
- Open Postman
- Click **Import** → **Upload Files**
- Select `tests/WordPress_Report_API_Tests.postman_collection.json`

### 2. Set Environment Variables
Create a new environment with these variables:

| Variable | Value | Description |
|----------|-------|-------------|
| `base_url` | `https://localhost:7001` | Your API base URL |
| `auth_token` | `your_jwt_token_here` | JWT token (obtained from login) |

### 3. Get Authentication Token
1. Run the **"Login - Get Auth Token"** request first
2. Update the email/password in the request body
3. The token will be automatically saved to `auth_token` variable

## 📋 Test Scenarios

### ✅ **Basic Functionality Tests**

#### 1. **Get Report - Basic Test**
```
GET /WordPressReport/GetReport
```
**Tests:**
- ✅ Status code 200
- ✅ Response structure (items, totalCount, page, pageSize)
- ✅ Items contain all required fields
- ✅ Response time under 5 seconds

#### 2. **Pagination Test**
```
GET /WordPressReport/GetReport?page=2&pageSize=10
```
**Tests:**
- ✅ Pagination parameters respected
- ✅ Items count ≤ pageSize
- ✅ Page number matches request

#### 3. **Search Functionality**
```
GET /WordPressReport/GetReport?search=John
```
**Tests:**
- ✅ Search works across name/email fields
- ✅ Results contain search term

#### 4. **Sorting Test**
```
GET /WordPressReport/GetReport?sortColumn=lastname&sortDesc=true
```
**Tests:**
- ✅ Results sorted correctly
- ✅ Sort direction respected

#### 5. **Date Filtering**
```
GET /WordPressReport/GetReport?from=2024-01-01T00:00:00Z&to=2024-12-31T23:59:59Z
```
**Tests:**
- ✅ Only results within date range
- ✅ Fourth completion dates match filter

#### 6. **Fourth Completion Only**
```
GET /WordPressReport/GetReport?showOnlyFourthCompletion=true
```
**Tests:**
- ✅ Each user appears only once
- ✅ Flag reflected in response

### 📊 **Supporting Endpoints**

#### 7. **Get Categories**
```
GET /WordPressReport/GetCategories
```
**Tests:**
- ✅ Returns array of categories
- ✅ Each category has id and name
- ✅ Charter Admin access required

#### 8. **Get Available Columns**
```
GET /WordPressReport/GetAvailableColumns
```
**Tests:**
- ✅ Returns column definitions
- ✅ Required columns present
- ✅ Proper value/label structure

#### 9. **Excel Export**
```
POST /WordPressReport/ExportExcel
Body: selectedColumns[], fromUtc, toUtc
```
**Tests:**
- ✅ Returns Excel file (.xlsx)
- ✅ Proper Content-Disposition header
- ✅ File size reasonable (1KB - 50MB)

### 🔒 **Security Tests**

#### 10. **Unauthorized Access**
```
GET /WordPressReport/GetReport
Authorization: None
```
**Expected:** 401 or 403 status

### ⚠️ **Error Handling Tests**

#### 11. **Invalid Sort Column**
```
GET /WordPressReport/GetReport?sortColumn=invalid_column
```
**Expected:** Graceful handling (200 with default sort OR 400)

#### 12. **Invalid Date Format**
```
GET /WordPressReport/GetReport?from=invalid-date
```
**Expected:** Graceful handling

## 🔧 **Manual Test Commands**

If you prefer testing with cURL:

### Get Auth Token
```bash
curl -X POST "https://localhost:7001/Account/Login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@charter.co.za","password":"AdminPassword123"}' \
  -k
```

### Basic Report Test
```bash
curl -X GET "https://localhost:7001/WordPressReport/GetReport" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -k
```

### Search Test
```bash
curl -X GET "https://localhost:7001/WordPressReport/GetReport?search=John&page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -k
```

### Excel Export Test
```bash
curl -X POST "https://localhost:7001/WordPressReport/ExportExcel" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "selectedColumns=LastName&selectedColumns=FirstName&selectedColumns=Email" \
  --output report.xlsx \
  -k
```

## 📈 **Expected Results**

### **Successful Response Example:**
```json
{
  "items": [
    {
      "userId": 123,
      "firstName": "John",
      "lastName": "Doe", 
      "ppraNo": "PPRA12345",
      "idNo": "ID67890",
      "province": "Western Cape",
      "agency": "Department of Health",
      "email": "john.doe@example.com",
      "phoneNumber": "+27123456789",
      "courseName": "Advanced Leadership",
      "category": "Management", 
      "enrolmentDate": "2024-01-15T08:00:00Z",
      "completionDate": "2024-03-15T16:30:00Z",
      "fourthCompletionDate": "2024-06-15T14:20:00Z"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 25,
  "showOnlyFourthCompletion": false
}
```

### **Empty Results (Valid):**
```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 25,
  "showOnlyFourthCompletion": false
}
```

### **Error Response:**
```json
{
  "error": "Failed to retrieve WordPress report: Database connection failed"
}
```

## 🚨 **Troubleshooting**

### **Empty Results?**
1. **Check Database Connection**: Verify "Woo" connection string points to WordPress DB
2. **Verify Data**: Run investigation queries from `CRITICAL_IMPLEMENTATION_FIXES.md`
3. **Check LMS Plugin**: Ensure correct LMS plugin assumptions
4. **Validate Custom Fields**: Confirm user meta fields exist

### **Authentication Issues?**
1. **Check Roles**: Ensure user has Charter Admin or Rebosa Admin role
2. **Token Expiry**: Get fresh token from login endpoint
3. **HTTPS**: Use `-k` flag with curl for self-signed certificates

### **Performance Issues?**
1. **Add Database Indexes**: On completion dates, user IDs
2. **Limit Page Size**: Use pageSize ≤ 200 for regular queries
3. **Date Filters**: Always use date ranges for large datasets

### **No Categories/Empty Dropdowns?**
- Check taxonomy name in `GetWordPressCategoriesAsync`
- Verify course categories exist in WordPress
- Ensure categories have `count > 0`

## ✅ **Success Checklist**

- [ ] Authentication works (Login returns token)
- [ ] Basic report returns data structure
- [ ] Pagination works correctly
- [ ] Search finds relevant results
- [ ] Sorting changes order
- [ ] Date filtering limits results
- [ ] Categories endpoint returns data
- [ ] Excel export downloads file
- [ ] Unauthorized requests are blocked
- [ ] Invalid parameters handled gracefully

## 🎯 **Performance Benchmarks**

| Endpoint | Expected Response Time | Max Records |
|----------|----------------------|-------------|
| GetReport (25 items) | < 2 seconds | 50,000 |
| GetReport (200 items) | < 5 seconds | 50,000 |
| GetCategories | < 1 second | N/A |
| ExportExcel | < 30 seconds | 50,000 |

**Ready to test!** 🚀 Import the collection and start with the authentication request.
