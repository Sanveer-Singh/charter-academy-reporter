# 🧪 WordPress Report API - Ready to Test!

## 🚀 **3 Ways to Test Your API**

### **Option 1: Quick Script Test (Fastest)**
Run the automated test script:

**Windows (PowerShell):**
```powershell
.\tests\quick-test.ps1 -BaseUrl "https://localhost:7001" -AdminEmail "admin@charter.co.za" -AdminPassword "YourPassword"
```

**Linux/Mac (Bash):**
```bash
./tests/quick-test.sh https://localhost:7001 admin@charter.co.za YourPassword
```

**What it tests:**
- ✅ Authentication & token generation
- ✅ Basic report functionality
- ✅ Search & sorting
- ✅ Categories endpoint
- ✅ Excel export
- ✅ Security (unauthorized access blocked)

### **Option 2: Postman Collection (Most Detailed)**
1. Import `tests/WordPress_Report_API_Tests.postman_collection.json`
2. Set environment variables (`base_url`, `auth_token`)
3. Run the **"Login - Get Auth Token"** request first
4. Run all other tests

**Tests included:**
- 🔐 Authentication flow
- 📊 Report data with all parameters
- 🔍 Search functionality
- 📋 Sorting & pagination
- 📅 Date filtering
- 📂 Categories & columns
- 📤 Excel export
- 🔒 Security & error handling

### **Option 3: Manual cURL Commands**

#### Get Auth Token:
```bash
curl -X POST "https://localhost:7001/Account/Login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@charter.co.za","password":"YourPassword"}' \
  -k
```

#### Basic Report:
```bash
curl -X GET "https://localhost:7001/WordPressReport/GetReport" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -k
```

#### With Search:
```bash
curl -X GET "https://localhost:7001/WordPressReport/GetReport?search=John&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -k
```

#### Excel Export:
```bash
curl -X POST "https://localhost:7001/WordPressReport/ExportExcel" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "selectedColumns=LastName&selectedColumns=FirstName&selectedColumns=Email" \
  --output report.xlsx \
  -k
```

## 🎯 **Expected Results**

### **✅ Success Response:**
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

### **⚠️ Empty Results (May be normal):**
```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 25,
  "showOnlyFourthCompletion": false
}
```

## 🔧 **Before Testing - Configuration Checklist**

Make sure you have:

- [ ] **Database Connection**: "Woo" connection string points to WordPress database
- [ ] **Admin Account**: Valid Charter Admin or Rebosa Admin user
- [ ] **HTTPS/SSL**: Using `https://` URL (or `-k` flag for self-signed certificates)
- [ ] **Port**: Correct port number (usually 7001 for development)
- [ ] **Application Running**: Your ASP.NET Core app is started

## 🚨 **Common Test Scenarios**

### **Scenario 1: Empty Results**
**Symptoms:** `totalCount: 0`, empty items array
**Likely Causes:**
- No users have completed exactly 4 courses
- WordPress database not connected
- LMS plugin mismatch (using LearnDash but code expects STM LMS)
- Custom fields don't exist

**Solution:** Run database investigation queries

### **Scenario 2: Authentication Fails** 
**Symptoms:** 401/403 errors, "Unauthorized"
**Likely Causes:**
- Wrong email/password
- User doesn't have Charter Admin or Rebosa Admin role
- JWT token expired

**Solution:** Check user roles in database

### **Scenario 3: Categories Empty**
**Symptoms:** Categories endpoint returns `[]`
**Likely Causes:**
- Course taxonomy name mismatch
- No course categories in WordPress
- Categories have zero count

**Solution:** Check WordPress taxonomy tables

## 📊 **Test Priority Order**

1. **🔐 Authentication** - Must work first
2. **📊 Basic Report** - Core functionality  
3. **🔍 Search** - User experience feature
4. **📂 Categories** - Filtering support
5. **📤 Excel Export** - Business requirement
6. **🔒 Security** - Access control

## 🎉 **Success Criteria**

Your API is working correctly if:

- ✅ Authentication returns valid JWT token
- ✅ Report endpoint returns data structure (even if empty)
- ✅ Categories/columns endpoints return arrays
- ✅ Excel export downloads .xlsx file
- ✅ Unauthorized requests are blocked
- ✅ Search/sort parameters are accepted

## 📁 **Test Files Created**

```
tests/
├── WordPress_Report_API_Tests.postman_collection.json  # Postman collection
├── quick-test.sh                                       # Linux/Mac test script  
└── quick-test.ps1                                     # Windows test script

POSTMAN_TEST_GUIDE.md                                  # Detailed testing guide
TEST_SUMMARY.md                                        # This file
docs/wordpress-report-api.md                           # API documentation
```

## 🚀 **Ready to Go!**

**Start with:** Run the quick test script for immediate feedback
**Then:** Import Postman collection for detailed testing  
**Finally:** Use manual cURL commands for specific scenarios

Your WordPress report API is ready for testing! 🎯
