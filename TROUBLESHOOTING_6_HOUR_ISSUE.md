# Troubleshooting: 6+ Hour Execution Time

## Most Likely Causes

### 1. **GenerateAdmissionNumber_V1 is BLOCKING** ⚠️ **MOST LIKELY**
The `GenerateAdmissionNumber_V1` procedure likely uses `UPDLOCK`/`HOLDLOCK` on the `AdmissionCounter` table. If another session is holding a lock on this table, your SP will wait indefinitely.

**Check:**
```sql
-- Run this while SP is executing
SELECT * FROM sys.dm_tran_locks 
WHERE resource_associated_entity_id = OBJECT_ID('AdmissionCounter');
```

**Solution:**
- Check if `GenerateAdmissionNumber_V1` uses `WITH (UPDLOCK, HOLDLOCK)`
- If yes, consider using `WITH (UPDLOCK, ROWLOCK)` instead
- Or use a different locking strategy

### 2. **Triggers on Tables**
There might be triggers on `Admission`, `Patient`, or `InvoiceHeader` tables that are slow or blocking.

**Check:**
```sql
SELECT * FROM sys.triggers WHERE parent_id = OBJECT_ID('Admission');
SELECT * FROM sys.triggers WHERE parent_id = OBJECT_ID('Patient');
```

**Solution:**
- Temporarily disable triggers to test
- Optimize trigger logic
- Move trigger logic to stored procedure

### 3. **Cross-Database Queries**
Queries to `HospitalDefinition` and `Billing` databases might be slow due to:
- Network latency
- Missing indexes
- Blocking on remote databases

**Check:**
- Run `sp_who2` or `sys.dm_exec_requests` to see where it's waiting
- Check wait types: `PAGEIOLATCH_SH`, `LCK_M_X`, etc.

### 4. **Transaction Blocking**
Long transaction holding locks on multiple tables.

**Check:**
```sql
SELECT * FROM sys.dm_tran_active_transactions;
SELECT * FROM sys.dm_tran_session_transactions;
```

**Solution:**
- Reduce transaction scope
- Commit after each major step (Patient, Admission, Invoice)

### 5. **Missing WHERE Clause in OPENJSON**
If OPENJSON is processing invalid/null items, it might be slow.

**Current code:**
```sql
FROM OPENJSON(@invoiceJson)
WHERE CAST(ISNULL(JSON_VALUE(value, '$.denomination'), '0') AS INT) > 0;
```

This is correct, but make sure it's actually filtering.

## Diagnostic Steps

### Step 1: Use Diagnostic Version
I've created `sp_SaveQuickAdmission_DIAGNOSTIC.sql` which adds timing/logging. This will show you exactly where it hangs.

### Step 2: Check for Blocking
While the SP is running, open another SSMS window and run:
```sql
-- See what's blocking
SELECT * FROM sys.dm_exec_requests WHERE blocking_session_id > 0;

-- Check locks on AdmissionCounter
SELECT * FROM sys.dm_tran_locks 
WHERE resource_associated_entity_id = OBJECT_ID('AdmissionCounter');
```

### Step 3: Check GenerateAdmissionNumber_V1
```sql
-- View the procedure definition
SELECT OBJECT_DEFINITION(OBJECT_ID('GenerateAdmissionNumber_V1'));
```

Look for:
- `WITH (UPDLOCK, HOLDLOCK)` - This causes blocking!
- Long-running queries
- Nested transactions

### Step 4: Test Without GenerateAdmissionNumber_V1
Temporarily comment out the call to `GenerateAdmissionNumber_V1` and see if it completes faster:

```sql
-- EXEC [dbo].[GenerateAdmissionNumber_V1] @checkInDate, @type, @admissionNumber OUTPUT;
SET @admissionNumber = 'TEST-' + CAST(@admissionId AS VARCHAR(20));
```

If it completes quickly, the problem is in `GenerateAdmissionNumber_V1`.

## Quick Fixes to Try

### Fix 1: Reduce Transaction Scope
Instead of one big transaction, commit after each step:

```sql
-- After patient insert
IF @transactionCount = 0
    COMMIT TRANSACTION;
BEGIN TRANSACTION;

-- After admission insert
IF @transactionCount = 0
    COMMIT TRANSACTION;
BEGIN TRANSACTION;
```

### Fix 2: Use NOLOCK (for testing only!)
```sql
SELECT @CurrentCounter = ...
FROM AdmissionCounter WITH (NOLOCK) -- Only for testing!
```

### Fix 3: Add Timeout
```sql
SET LOCK_TIMEOUT 30000; -- 30 seconds
```

### Fix 4: Check for Deadlocks
```sql
SELECT * FROM sys.dm_exec_requests 
WHERE blocking_session_id > 0 
   OR wait_type LIKE '%DEADLOCK%';
```

## Expected Behavior

With the diagnostic version, you should see output like:
```
=== SP STARTED: 2026-01-10 10:00:00.000
=== STEP 1: Parsing save options...
=== STEP 1 COMPLETE: 0 seconds
=== STEP 2: Saving patient...
=== STEP 2 COMPLETE: 1 seconds
=== STEP 3: Saving admission...
=== CALLING GenerateAdmissionNumber_V1 - THIS MAY BLOCK...
[HANGS HERE IF BLOCKING]
```

This will tell you exactly where it's hanging!
