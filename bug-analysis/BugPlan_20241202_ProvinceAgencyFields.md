# Bug Fix Plan: Province and Agency Fields Data Population

**Version**: 1.0.0
**Created**: 2024-12-02
**Bug ID**: TASK_20241202_ProvinceAgencyFields
**Estimated Time**: 1-2 hours

## Fix Strategy

### Option 1: Dynamic Field Discovery (RECOMMENDED)
Update the query to dynamically discover the correct field shortnames by pattern matching.

### Option 2: Configuration-Based Approach
Add configurable field mappings in `appsettings.json` to allow customization.

### Option 3: Multiple Shortname Attempt
Try common variations of shortnames until data is found.

## Implementation Plan

### Phase 1: Field Discovery Enhancement
1. **Create field discovery query** to identify actual shortnames:
   ```sql
   SELECT shortname, name FROM {prefix}user_info_field 
   WHERE name LIKE '%agency%' OR name LIKE '%province%' OR shortname LIKE '%agency%' OR shortname LIKE '%province%'
   ```

2. **Update CustomFields CTE** to use discovered shortnames or common variations:
   ```sql
   MAX(CASE WHEN uif.shortname IN ('province', 'user_province', 'employerprovince') THEN uid.data END) as province,
   MAX(CASE WHEN uif.shortname IN ('agency', 'agencyname', 'agency_name', 'employeragency') THEN uid.data END) as agency
   ```

### Phase 2: Enhanced Error Handling
1. Add logging for field discovery results
2. Add fallback mechanisms for missing fields
3. Ensure graceful degradation when fields aren't found

### Phase 3: Testing & Validation
1. Test with actual Moodle database
2. Verify field population in reports
3. Validate sorting and search functionality

## Technical Requirements

### Code Changes Required
- **File**: `src/Infrastructure/Services/Dashboard/MariaDbDashboardService.cs`
- **Method**: `GetMoodleReportAsync` (CustomFields CTE)
- **Lines**: 146-147, potentially 150

### Database Constraints
- **READ ONLY**: Cannot modify Moodle database structure
- **NO DATA CHANGES**: Cannot alter existing user data
- **TRANSACTION SAFETY**: All queries must remain read-only

### Compatibility Requirements
- Maintain backward compatibility with existing queries
- Preserve performance characteristics
- Keep existing security safeguards

## Risk Assessment

### Low Risk
- Query optimization only, no structural changes
- Read-only operations maintain data integrity
- Existing working fields (PPRA, ID) remain unchanged

### Mitigation Strategies
- Test with copy of production data first
- Add comprehensive logging for field resolution
- Implement graceful fallbacks for missing fields

## Testing Strategy

### Test Cases
1. **Fields Found**: Verify Province and Agency populate correctly
2. **Fields Missing**: Ensure graceful handling without errors
3. **Partial Data**: Test when only one field is available
4. **Performance**: Ensure no query degradation
5. **Sorting**: Verify Province/Agency sorting works

### Validation Criteria
- [ ] Province and Agency columns show data from Moodle user profiles
- [ ] No performance regression
- [ ] Existing functionality preserved
- [ ] Error handling works gracefully

## Implementation Steps

1. ✅ Analysis completed
2. ✅ Plan documented  
3. 🔄 Implement field discovery logic
4. ⏳ Update SQL query with multiple shortname attempts
5. ⏳ Add logging and error handling
6. ⏳ Test and validate changes

## Success Criteria
- Province and Agency columns populate with data from Moodle user profiles
- Report maintains current performance
- Search and sorting work correctly for new fields
- No breaking changes to existing functionality
