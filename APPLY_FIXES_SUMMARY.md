# How to Apply the Performance Fixes

## The Main Problem

Your stored procedure is taking 1+ hour because of this line:
```sql
EXISTS (SELECT 1 FROM OPENJSON(@saveData, '$.invoice') WHERE CAST(JSON_VALUE(value, '$.denomination') AS INT) > 0)
```

This scans the entire JSON array and is extremely slow!

## Quick Fix Steps

1. **Remove the EXISTS check with OPENJSON** - Replace it with a simple string check
2. **Fix the transaction** - Change `rollback transaction` to `COMMIT TRANSACTION`
3. **Fix @saveOptions parsing** - Extract nested saveOptions properly
4. **Fix Department type** - Change from INT to NVARCHAR(50)
5. **Re-enable SET NOCOUNT ON** - Uncomment it
6. **Remove TestAdmissionInsert** - Remove the test table insert

## Use the Fixed Version

I've created a fixed version in `sp_SaveQuickAdmission_PERFORMANCE_FIXED.sql`. 

**Key Changes:**
- ✅ Removed EXISTS with OPENJSON (fast string check instead)
- ✅ Fixed transaction commit (was rolling back everything!)
- ✅ Fixed @saveOptions parsing (extracts nested saveOptions correctly)
- ✅ Fixed Department to NVARCHAR (was INT)
- ✅ Re-enabled SET NOCOUNT ON
- ✅ Single OPENJSON call per operation (reuses @invoiceJson variable)
- ✅ Includes your GenerateAdmissionNumber_V1 call
- ✅ Includes your ResidentPatient sync logic

## Next Steps

1. Review `sp_SaveQuickAdmission_PERFORMANCE_FIXED.sql`
2. Apply it to your database
3. Test with your data
4. Execution should be seconds, not hours!


