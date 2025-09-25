# Bug Analysis: Province and Agency Fields Empty in Reports

**Version**: 1.0.0
**Created**: 2024-12-02
**Bug ID**: TASK_20241202_ProvinceAgencyFields
**Severity**: HIGH
**Type**: Data Population Issue

## Problem Statement

Province and Agency columns in the Moodle Student Completion Report remain empty despite being visible and populated on the user's edit profile page under "Employment and Agency Information".

## Root Cause Analysis

### Issue Identified
The SQL query in `MariaDbDashboardService.cs` (lines 146-147) is looking for Moodle custom user profile fields with hardcoded shortnames:
```sql
MAX(CASE WHEN uif.shortname = 'province' THEN uid.data END) as province,
MAX(CASE WHEN uif.shortname = 'agency' THEN uid.data END) as agency
```

### Why This Fails
1. **Shortname Mismatch**: In Moodle, custom profile fields have a `shortname` (used internally) that differs from the display name
   - Display Name: "Agency Name" → Possible shortnames: "agencyname", "agency_name", "employeragency", etc.
   - Display Name: "Province" → Possible shortnames: "province", "user_province", "employerprovince", etc.

2. **Missing Data Source**: The query joins `user_info_data` and `user_info_field` tables but with incorrect field identifiers

3. **No Fallback Mechanism**: No error handling or logging when fields aren't found

## Technical Details

### Current Query Structure
```sql
CustomFields AS (
    SELECT 
        uid.userid,
        MAX(CASE WHEN uif.shortname = 'province' THEN uid.data END) as province,
        MAX(CASE WHEN uif.shortname = 'agency' THEN uid.data END) as agency
    FROM {prefix}user_info_data uid
    JOIN {prefix}user_info_field uif ON uid.fieldid = uif.id
    WHERE uif.shortname IN ('ppranumber', 'said', 'province', 'agency')
    GROUP BY uid.userid
)
```

### Evidence of Issue
- PPRA No and ID No fields work correctly (using 'ppranumber' and 'said' shortnames)
- Province and Agency return empty strings via `COALESCE(cf.province, '')` and `COALESCE(cf.agency, '')`
- Fields are defined in `MoodleReportRow` model and displayed in UI

## Impact Assessment

### User Impact
- **HIGH**: Users cannot see critical agency and location information in reports
- **Affects**: All users viewing completion reports
- **Business Impact**: Incomplete compliance reporting, inability to track regional performance

### System Impact
- **LOW**: No system instability
- **Performance**: No performance degradation
- **Security**: No security implications

## Next Steps
1. Identify correct Moodle custom field shortnames
2. Update SQL query with correct field references
3. Add error handling and logging for missing fields
4. Test with real data
