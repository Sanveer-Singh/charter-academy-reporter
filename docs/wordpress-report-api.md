# WordPress Report API Documentation

## Overview

This API provides comprehensive reporting on WordPress user course completions, specifically targeting users who have completed all 4 required courses. The endpoints support server-side filtering, sorting, searching, and Excel export functionality.

## Endpoints

### 1. Get WordPress Report Data

**GET** `/WordPressReport/GetReport`

Retrieves paginated WordPress user completion data with filtering, sorting, and searching capabilities.

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `preset` | string | No | Date range preset: "last-month", "last-3-months", "last-6-months", "last-year", "all-time" |
| `from` | DateTime | No | Start date filter (UTC) |
| `to` | DateTime | No | End date filter (UTC) |
| `categoryId` | long | No | Course category ID filter |
| `search` | string | No | Search term (searches across name, email, course, category, PPRA number, ID) |
| `sortColumn` | string | No | Column to sort by: "firstname", "lastname", "email", "phonenumber", "pprano", "idno", "province", "agency", "coursename", "category", "enrolmentdate", "completiondate", "fourthcompletiondate" |
| `sortDesc` | boolean | No | Sort direction (true = descending, false = ascending) |
| `showOnlyFourthCompletion` | boolean | No | Show only 4th completion (true) or all completions (false, default) |
| `page` | int | No | Page number (default: 1) |
| `pageSize` | int | No | Items per page (default: 25, max: 200) |

#### Response Format

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

#### Example Requests

```bash
# Get all users with 4 course completions (default view)
GET /WordPressReport/GetReport

# Search by name
GET /WordPressReport/GetReport?search=John%20Doe

# Filter by date range and province (via search)
GET /WordPressReport/GetReport?from=2024-01-01&to=2024-12-31&search=Western%20Cape

# Sort by last name, descending
GET /WordPressReport/GetReport?sortColumn=lastname&sortDesc=true

# Show only 4th completions
GET /WordPressReport/GetReport?showOnlyFourthCompletion=true

# Paginated results
GET /WordPressReport/GetReport?page=2&pageSize=50
```

### 2. Get Course Categories

**GET** `/WordPressReport/GetCategories`

Retrieves available course categories for filtering.

### 3. Get Available Columns

**GET** `/WordPressReport/GetAvailableColumns`

Retrieves available columns for export functionality.

### 4. Export to Excel

**POST** `/WordPressReport/ExportExcel`

Exports WordPress report data to Excel format with WooCommerce purchase validation.

## Security & Authorization

- **Required Role**: Charter Admin or Rebosa Admin
- **Policy**: `RequireAnyAdmin`
- All database queries use read-only transactions
- SQL injection protection via parameterized queries
- Export limited to 50,000 rows maximum

## Data Validation

The API validates that users have both:
1. **Purchased courses** through WooCommerce (order status: completed/processing)
2. **Completed 4 courses** in the LMS system

## Performance Features

- Server-side pagination
- Optimized SQL queries with proper indexing
- Read-only database transactions
- Connection pooling via MariaDB connection factory
- Efficient sorting via database-level ORDER BY
