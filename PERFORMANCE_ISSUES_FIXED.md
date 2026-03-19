# Performance Issues Fixed in sp_SaveQuickAdmission

## Critical Issues Found

### 1. **CRITICAL PERFORMANCE KILLER: EXISTS with OPENJSON**
**Problem:**
```sql
EXISTS (SELECT 1 FROM OPENJSON(@saveData, '$.invoice') WHERE CAST(JSON_VALUE(value, '$.denomination') AS INT) > 0)
```
This scans the entire JSON array and is EXTREMELY slow (causes 1+ hour execution time).

**Fix:**
```sql
-- FAST CHECK: Just check if JSON exists (no OPENJSON scan!)
DECLARE @invoiceJson NVARCHAR(MAX) = JSON_QUERY(@saveData, '$.invoice');
IF @invoiceJson IS NOT NULL AND @invoiceJson != 'null' AND @invoiceJson != '' AND @invoiceJson != '[]'
```

### 2. **Transaction Always Rolled Back**
**Problem:**
```sql
IF @transactionCount = 0
    rollback transaction --COMMIT TRANSACTION;
```
This means ALL changes are rolled back - nothing is saved!

**Fix:**
```sql
IF @transactionCount = 0
    COMMIT TRANSACTION;
```

### 3. **SET NOCOUNT ON Commented Out**
**Problem:**
```sql
--SET NOCOUNT ON;
```
This causes extra network overhead.

**Fix:**
```sql
SET NOCOUNT ON;
```

### 4. **Wrong @saveOptions Parsing**
**Problem:**
```sql
JSON_VALUE(@saveOptions, '$.saveMedicalFile')
```
But `@saveOptions` contains the full JSON structure, not just options.

**Fix:**
```sql
-- Extract nested saveOptions first
DECLARE @optionsJson NVARCHAR(MAX) = JSON_QUERY(@saveOptions, '$.saveOptions');
-- Then parse from @optionsJson
SET @saveMedicalFile = CAST(ISNULL(JSON_VALUE(@optionsJson, '$.saveMedicalFile'), '1') AS BIT);
```

### 5. **Department Data Type Mismatch**
**Problem:**
```sql
DECLARE @department INT;
SET @department = CAST(JSON_VALUE(@saveData, '$.admission.Department'), '0') AS INT);
```
But in your test data, Department is "Lab" (a string), not an INT.

**Fix:**
```sql
DECLARE @department NVARCHAR(50);
SET @department = JSON_VALUE(@saveData, '$.admission.Department');
```

### 6. **Test Table Insert (Should Be Removed)**
**Problem:**
```sql
insert into TestAdmissionInsert([existingPatientId], [saveData], [saveOptions])
values(@existingPatientId,@saveData, @saveData)
```
This shouldn't be in production code.

**Fix:** Remove this insert statement.

### 7. **Multiple OPENJSON Calls**
**Problem:** Calling OPENJSON multiple times on the same data is inefficient.

**Fix:** Extract JSON once, reuse the variable:
```sql
DECLARE @invoiceJson NVARCHAR(MAX) = JSON_QUERY(@saveData, '$.invoice');
-- Use @invoiceJson in all OPENJSON calls
```

## Summary

The main performance issue is the `EXISTS` clause with OPENJSON which scans the entire JSON array. This is what's causing the 1+ hour execution time. The fix is to:
1. Remove the EXISTS check with OPENJSON
2. Use a simple string check instead
3. Only call OPENJSON when actually inserting data
4. Fix the transaction commit/rollback
5. Fix the @saveOptions parsing
6. Fix Department data type
7. Remove test table insert
8. Re-enable SET NOCOUNT ON


