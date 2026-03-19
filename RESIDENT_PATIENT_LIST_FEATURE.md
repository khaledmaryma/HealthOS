# Resident Patient List Feature

## Overview
A new page has been added to the LIS application to display and manage the list of resident patients from the `Admission.dbo.ResidentPatient` table.

## Files Created

### Backend (LIS.Api)
1. **Models/ResidentPatient.cs**
   - Complete model mapping all 87 columns from the ResidentPatient table
   - Includes all patient demographics, admission details, insurance information, and billing data

2. **Controllers/ResidentPatientController.cs**
   - `GET /api/ResidentPatient` - Get paginated list of patients with filtering
   - `GET /api/ResidentPatient/count` - Get total count of patients matching filters
   - `GET /api/ResidentPatient/{id}` - Get single patient by ID
   - Supports query parameters:
     - `page` (default: 1)
     - `pageSize` (default: 50)
     - `search` (filters by PatientName, AdmissionNumber, or MedicalRecordNumber)
     - `isDischarged` (boolean filter for discharge status)

### Frontend (LIS.Web)
1. **models/resident-patient.ts**
   - TypeScript interface matching the API model

2. **services/resident-patient.service.ts**
   - Service for API communication
   - Methods: `getAll()`, `getCount()`, `getById()`

3. **resident-patient-list/resident-patient-list.component.ts**
   - Main component with signals for reactive state management
   - Pagination logic with computed properties
   - Search and filter functionality

4. **resident-patient-list/resident-patient-list.component.html**
   - Responsive table layout with sticky header
   - Search bar and filters (discharge status, page size)
   - Pagination controls
   - Patient information display including:
     - Admission number
     - Patient name (English and Arabic)
     - MRN
     - Gender with icons
     - Age calculation
     - Check-in date
     - Class and insurance
     - Room/Bed assignment
     - Attending physician
     - Status (Active/Discharged)

5. **resident-patient-list/resident-patient-list.component.scss**
   - Modern styling with Bootstrap integration
   - Sticky table header
   - Custom scrollbar
   - Hover effects

### Routing & Navigation
- Route added: `/resident-patients`
- Navigation link added to main menu: "Resident Patients"

## Features

### 1. **Pagination**
- Configurable page sizes (25, 50, 100 per page)
- Navigation controls (Previous, Next, Page numbers)
- Smart page number display (max 7 visible)
- Shows current range and total count

### 2. **Search**
- Real-time search across:
  - Patient Name
  - Admission Number
  - Medical Record Number
- Resets to page 1 on search

### 3. **Filtering**
- Discharge status filter:
  - All Patients
  - Active Only (not discharged)
  - Discharged Only

### 4. **Patient Information Display**
- Color-coded gender icons (blue for male, red for female)
- Auto-calculated age from date of birth
- Formatted dates with tooltips showing full datetime
- Badge-based status indicators
- Room and bed information with icons
- Insurance details (main insurance and class)
- Physician information (attending or referral)

### 5. **Responsive Design**
- Full-height layout with sticky elements
- Scrollable table body
- Mobile-friendly (responsive breakpoints)

### 6. **Performance**
- Lazy loading via Angular routing
- Server-side pagination
- SQL queries with NOLOCK hints
- Efficient signal-based state management

## Database Schema
The feature reads from the `ResidentPatient` table in the `Admission` database, which contains 87 columns including:
- Patient demographics (name, DOB, gender, contact)
- Admission details (admission number, MRN, check-in date, class)
- Insurance information (main and auxiliary)
- Physician assignments (referral and attending)
- Location (room, bed, floor)
- Billing information (advances, invoices)
- Status flags (discharged, pharmacy discharge, nursing discharge)
- Diagnostic information (3 groups)

## Usage
1. Navigate to "Resident Patients" from the main menu
2. Use the search bar to find specific patients
3. Apply filters to view active or discharged patients
4. Adjust page size as needed
5. Navigate through pages using pagination controls

## Future Enhancements (Potential)
- Export to Excel/PDF
- Advanced filtering (by date range, insurance, physician)
- Sorting by columns
- Patient detail view/modal
- Edit patient information
- Quick actions (discharge, transfer)
- Statistics dashboard




















