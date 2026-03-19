# Bacteriology Entities Summary

This document summarizes the bacteriology-related entities that have been added to the LIS API model.

## Created Entities

### 1. **Germs** (`Models/Germs.cs`)
Represents microorganisms/germs found in bacteriology tests.

**Key Fields:**
- `Id` - Primary key
- `Description` - English description
- `ArabicDescription` - Arabic description
- `Code` - Unique code for the germ
- `GermFamily` - Foreign key to germ family
- `DisplayOrder` - Order for display
- Audit fields: `IsDeleted`, `CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`

### 2. **Antibiotic** (`Models/Antibiotic.cs`)
Represents antibiotics used in sensitivity testing.

**Key Fields:**
- `Id` - Primary key
- `Description` - English description
- `ArabicDescription` - Arabic description
- `Code` - Unique code for the antibiotic
- `AntibFamily` - Foreign key to antibiotic family
- `CommercialName` - Commercial/brand name
- `DisplayOrder` - Order for display
- Audit fields: `IsDeleted`, `CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`

### 3. **AntibFamily** (`Models/AntibFamily.cs`)
Represents families/classes of antibiotics (e.g., Penicillins, Cephalosporins).

**Key Fields:**
- `Id` - Primary key
- `Description` - English description
- `ArabicDescription` - Arabic description
- `DisplayOrder` - Order for display
- Audit fields: `IsDeleted`, `CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`

### 4. **GermAntibiotic** (`Models/GermAntibiotic.cs`)
Junction table linking germs to antibiotics for antibiogram configuration.

**Key Fields:**
- `Id` - Primary key
- `GermId` - Foreign key to Germs
- `AntibioticId` - Foreign key to Antibiotic
- `DisplayOrder` - Order for display
- Audit fields: `IsDeleted`, `CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`

**Relationships:**
- Many-to-one with `Germs` (navigation property: `Germ`)
- Many-to-one with `Antibiotic` (navigation property: `Antibiotic`)

### 5. **Bacteria** (`Models/Bacteria.cs`)
Represents bacteria types (likely a more specific classification than Germs).

**Key Fields:**
- `Id` - Primary key
- `Description` - English description
- `ArabicDescription` - Arabic description
- `Code` - Unique code
- `BacteriaType` - Type classification
- `BacteriaFamily` - Family classification
- `DisplayOrder` - Order for display
- Audit fields: `IsDeleted`, `CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`

### 6. **PatientLabBacteriologyHeader** (`Models/PatientLabBacteriologyHeader.cs`)
Header/master record for patient bacteriology tests.

**Key Fields:**
- `Id` - Primary key
- `PatientHeaderId` - Link to patient lab results header
- `LabTestId` - Lab test ID
- `LabTestDescription` - Lab test name
- `SpecimenType` - Type of specimen (e.g., blood, urine)
- `CollectionDate` - When specimen was collected
- `ReceptionDate` - When lab received it
- `ResultDate` - When results were finalized
- `MacroscopicExamination` - Text for macroscopic findings
- `MicroscopicExamination` - Text for microscopic findings
- `CultureResult` - Text for culture results
- `StatusId` - Current status
- `IsNotified` - Whether results were notified
- `Printed` - Whether results were printed
- `Comments` - Additional comments
- Audit fields: `IsDeleted`, `CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`

### 7. **PatientLabBacteriology** (`Models/PatientLabBacteriology.cs`)
Detail records for patient bacteriology results (antibiogram/sensitivity testing).

**Key Fields:**
- `Id` - Primary key
- `BacteriologyHeaderId` - Foreign key to header
- `GermId` - Foreign key to Germs
- `GermDescription` - Germ name
- `AntibioticId` - Foreign key to Antibiotic
- `AntibioticDescription` - Antibiotic name
- `Sensitivity` - Sensitivity result (S/R/I)
- `Result` - Test result
- `Colony` - Colony description
- `DisplayOrder` - Order for display
- `Comments` - Additional comments
- Audit fields: `IsDeleted`, `CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`

**Relationships:**
- Many-to-one with `PatientLabBacteriologyHeader` (navigation property: `BacteriologyHeader`)
- Many-to-one with `Germs` (navigation property: `Germ`)
- Many-to-one with `Antibiotic` (navigation property: `Antibiotic`)

## Database Context Updates

All entities have been added to `LISDbContext.cs`:

```csharp
// DbSet properties
public DbSet<Germs> Germs { get; set; }
public DbSet<GermAntibiotic> GermAntibiotics { get; set; }
public DbSet<Bacteria> Bacterias { get; set; }
public DbSet<Antibiotic> Antibiotics { get; set; }
public DbSet<AntibFamily> AntibFamilies { get; set; }
public DbSet<PatientLabBacteriologyHeader> PatientLabBacteriologyHeaders { get; set; }
public DbSet<PatientLabBacteriology> PatientLabBacteriologies { get; set; }
```

## Entity Relationships

```
AntibFamily
    ↓ (1:N)
Antibiotic ←───────────┐
    ↑                  │
    │                  │
    │ (N:M via        │ (N:1)
    │  GermAntibiotic) │
    │                  │
    ↓                  │
Germs                  │
    ↑                  │
    │ (N:1)           │
    │                  │
PatientLabBacteriology │
    ↓ (N:1)           │
PatientLabBacteriologyHeader
    ↓ (N:1)
PatientLabResultsHeader
```

## Configuration in OnModelCreating

All relationships are configured with:
- Foreign key constraints
- Cascade delete behavior set to `Restrict`
- Navigation properties for easy querying

## Next Steps

To use these entities, you can create controllers for:

1. **GermsController** - Manage germs master data
2. **AntibioticsController** - Manage antibiotics master data
3. **AntibFamiliesController** - Manage antibiotic families
4. **GermAntibioticsController** - Configure antibiogram
5. **BacteriasController** - Manage bacteria classifications
6. **PatientLabBacteriologyHeadersController** - Manage bacteriology test headers
7. **PatientLabBacteriologiesController** - Manage sensitivity results

## Usage Example

```csharp
// Get all germs with their antibiotics
var germsWithAntibiotics = await _context.GermAntibiotics
    .Include(ga => ga.Germ)
    .Include(ga => ga.Antibiotic)
    .Where(ga => !ga.IsDeleted)
    .ToListAsync();

// Get patient bacteriology results
var patientResults = await _context.PatientLabBacteriologies
    .Include(plb => plb.BacteriologyHeader)
    .Include(plb => plb.Germ)
    .Include(plb => plb.Antibiotic)
    .Where(plb => plb.BacteriologyHeaderId == headerId && !plb.IsDeleted)
    .OrderBy(plb => plb.DisplayOrder)
    .ToListAsync();
```

## Files Created

1. `LIS.Api/Models/Germs.cs`
2. `LIS.Api/Models/Antibiotic.cs`
3. `LIS.Api/Models/AntibFamily.cs`
4. `LIS.Api/Models/GermAntibiotic.cs`
5. `LIS.Api/Models/Bacteria.cs`
6. `LIS.Api/Models/PatientLabBacteriologyHeader.cs`
7. `LIS.Api/Models/PatientLabBacteriology.cs`

## Files Modified

1. `LIS.Api/Data/LISDbContext.cs` - Added DbSet properties and relationship configurations

---

**Date Created**: 2025-01-09  
**Version**: 1.0  
**Status**: Complete ✅





























