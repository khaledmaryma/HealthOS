-- =====================================================
-- Queries to Check for Blocking Issues
-- Run these in a separate SSMS window while SP is running
-- =====================================================

-- 1. Check for blocking sessions
SELECT 
    s.session_id,
    r.blocking_session_id,
    r.wait_type,
    r.wait_time,
    r.wait_resource,
    r.status,
    r.command,
    t.text AS sql_text,
    DB_NAME(r.database_id) AS database_name
FROM sys.dm_exec_requests r
INNER JOIN sys.dm_exec_sessions s ON r.session_id = s.session_id
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.blocking_session_id > 0
   OR r.wait_type IS NOT NULL
ORDER BY r.wait_time DESC;

-- 2. Check for locks on AdmissionCounter table (used by GenerateAdmissionNumber_V1)
SELECT 
    l.request_session_id,
    l.resource_database_id,
    l.resource_associated_entity_id,
    l.resource_type,
    l.resource_description,
    l.request_mode,
    l.request_type,
    l.request_status,
    OBJECT_NAME(p.object_id) AS object_name
FROM sys.dm_tran_locks l
LEFT JOIN sys.partitions p ON l.resource_associated_entity_id = p.partition_id
WHERE OBJECT_NAME(p.object_id) = 'AdmissionCounter'
   OR l.resource_type = 'KEY'
ORDER BY l.request_session_id;

-- 3. Check active transactions
SELECT 
    t.transaction_id,
    t.name AS transaction_name,
    t.transaction_begin_time,
    DATEDIFF(SECOND, t.transaction_begin_time, GETDATE()) AS transaction_duration_seconds,
    t.transaction_type,
    t.transaction_state,
    t.transaction_status,
    s.session_id,
    s.login_name,
    s.program_name,
    s.host_name
FROM sys.dm_tran_active_transactions t
INNER JOIN sys.dm_tran_session_transactions st ON t.transaction_id = st.transaction_id
INNER JOIN sys.dm_exec_sessions s ON st.session_id = s.session_id
WHERE t.transaction_begin_time < DATEADD(MINUTE, -5, GETDATE()) -- Transactions older than 5 minutes
ORDER BY t.transaction_begin_time;

-- 4. Check what GenerateAdmissionNumber_V1 is doing
SELECT 
    r.session_id,
    r.status,
    r.command,
    r.blocking_session_id,
    r.wait_type,
    r.wait_time,
    r.cpu_time,
    r.total_elapsed_time,
    t.text AS sql_text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE t.text LIKE '%GenerateAdmissionNumber%'
   OR t.text LIKE '%AdmissionCounter%';

-- 5. Check for deadlocks (if any occurred)
SELECT 
    CAST(event_data AS XML) AS DeadlockGraph
FROM sys.fn_xe_file_target_read_file('system_health*.xel', NULL, NULL, NULL)
WHERE CAST(event_data AS XML).value('(event/@name)[1]', 'varchar(50)') = 'xml_deadlock_report';

-- 6. Check current activity on AdmissionCounter table
SELECT 
    OBJECT_NAME(object_id) AS table_name,
    index_id,
    partition_number,
    row_count,
    reserved_page_count,
    lob_reserved_page_count
FROM sys.dm_db_partition_stats
WHERE object_id = OBJECT_ID('AdmissionCounter');

-- 7. Check if there are triggers on Admission table that might be slow
SELECT 
    t.name AS trigger_name,
    OBJECT_NAME(t.parent_id) AS table_name,
    t.is_disabled,
    t.is_instead_of_trigger,
    t.create_date,
    t.modify_date
FROM sys.triggers t
WHERE OBJECT_NAME(t.parent_id) = 'Admission'
ORDER BY t.name;

-- 8. Check for long-running queries
SELECT 
    r.session_id,
    r.status,
    r.command,
    r.blocking_session_id,
    r.wait_type,
    r.wait_time,
    r.cpu_time,
    r.total_elapsed_time / 1000 AS elapsed_seconds,
    r.reads,
    r.writes,
    t.text AS sql_text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.total_elapsed_time > 30000 -- More than 30 seconds
ORDER BY r.total_elapsed_time DESC;
