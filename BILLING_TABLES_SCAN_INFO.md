# Billing Database Tables Scan

## Purpose
Scan and analyze the structure and data of InvoiceHeader and InvoiceDetail tables in the Billing database.

## SQL Scripts Created

### 1. `scan_billing_tables.sql` - Comprehensive Scan
This script provides a detailed analysis of both tables:

**For InvoiceHeader:**
- Complete table structure (columns, data types, lengths, nullable)
- Primary key information
- Foreign key relationships
- Sample data (first 10 rows)
- Total row count

**For InvoiceDetail:**
- Complete table structure (columns, data types, lengths, nullable)
- Primary key information
- Foreign key relationships
- Sample data (first 10 rows)
- Total row count

**Relationship Analysis:**
- Identifies common ID columns
- Shows sample join between InvoiceHeader and InvoiceDetail
- Attempts to preview the relationship

### 2. `quick_scan_billing.sql` - Quick Scan
A simplified version that quickly shows:
- Column names and types for both tables
- 5 sample rows from each table
- Row counts
- Potential linking columns

## How to Run

### Option 1: Using SQL Server Management Studio (SSMS)
```sql
1. Open SSMS
2. Connect to your SQL Server
3. Open the script file: scan_billing_tables.sql or quick_scan_billing.sql
4. Execute (F5)
5. View results in Messages and Results tabs
```

### Option 2: Using sqlcmd (Command Line)
```bash
sqlcmd -S localhost -d Billing -i scan_billing_tables.sql -o billing_scan_results.txt
```

### Option 3: Using PowerShell
```powershell
cd C:\d\LHH_Backup
sqlcmd -S localhost -d Billing -E -i scan_billing_tables.sql
```

## What to Look For

### InvoiceHeader Columns to Note:
- **ID** - Primary key
- **InvoiceNumber** - Invoice identifier
- **PatientID** or **MRN** - Link to patient
- **AdmissionNumber** - Link to admission
- **InvoiceDate** - When invoice was created
- **TotalAmount** - Invoice total
- **Status** - Invoice status (paid, pending, etc.)
- **IsDeleted** - Soft delete flag

### InvoiceDetail Columns to Note:
- **ID** - Primary key
- **InvoiceHeaderID** - Foreign key to InvoiceHeader
- **ItemDescription** - Description of service/item
- **Quantity** - Amount of service/item
- **UnitPrice** - Price per unit
- **TotalPrice** - Line total
- **ServiceID** or **ItemID** - Link to service/item
- **LabTestID** - Link to lab tests (if applicable)
- **IsDeleted** - Soft delete flag

### Relationship Pattern:
```
InvoiceHeader (1)
    ↓
InvoiceDetail (Many)

One invoice header can have multiple detail lines
```

## Expected Output Structure

### InvoiceHeader
Likely contains:
- Invoice identification (ID, Number)
- Patient information (PatientID, MRN, Name)
- Admission reference (AdmissionNumber)
- Financial totals (Total, Paid, Balance)
- Dates (InvoiceDate, DueDate, PaidDate)
- Status flags

### InvoiceDetail
Likely contains:
- Line item identification (ID, Sequence)
- Reference to header (InvoiceHeaderID)
- Service/item details (Description, Code)
- Pricing (Quantity, UnitPrice, Total)
- Reference to services (LabTestID, ServiceID)
- Discounts and adjustments

## Next Steps

After running the scan:
1. **Review the structure** - understand what columns exist
2. **Identify relationships** - how tables link together
3. **Check for LIS integration** - look for LabTestID or PatientLabResult references
4. **Analyze data patterns** - understand the business logic
5. **Document findings** - create models or integration plans

## Integration Potential

If these tables link to the LIS system:
- Could display invoice information with lab results
- Could track billing status for lab tests
- Could generate invoices from lab test results
- Could show payment status in patient results

## Files Created
- ✅ `scan_billing_tables.sql` - Comprehensive analysis
- ✅ `quick_scan_billing.sql` - Quick overview
- ✅ `BILLING_TABLES_SCAN_INFO.md` - This documentation

## Note
Make sure you have appropriate permissions to access the Billing database before running these scripts.






















