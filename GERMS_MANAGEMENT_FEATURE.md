# Germs Management Feature

## Overview
A complete CRUD (Create, Read, Update, Delete) management system for the Germs table in the LIS database. This feature provides a user-friendly interface to define and modify germ records.

## Features

### Backend (API)
- **GermsController**: Full REST API with CRUD operations
- **Endpoints**:
  - `GET /api/germs` - Get all germs
  - `GET /api/germs/{id}` - Get specific germ by ID
  - `POST /api/germs` - Create new germ
  - `PUT /api/germs/{id}` - Update existing germ
  - `DELETE /api/germs/{id}` - Soft delete germ

### Frontend (Angular)
- **GermsComponent**: Main management interface
- **GermsService**: API service for data operations
- **Features**:
  - Data grid with sorting and pagination
  - Search functionality across all fields
  - Add/Edit modal form
  - Delete confirmation
  - Responsive design

## Database Schema
The feature works with the existing `Germs` table structure:

```sql
CREATE TABLE Germs (
    ID int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(50),
    Description nvarchar(255),
    Identifier nvarchar(50),
    DisplayOrder nvarchar(50),
    CreatedBy int,
    CreatedDate datetime,
    ModifiedBy int,
    ModifiedDate datetime,
    IsDeleted bit
);
```

## Usage

### Accessing the Feature
1. Navigate to the LIS application
2. Click on "Germs" in the main navigation menu
3. The germs management page will load

### Managing Germs

#### Adding a New Germ
1. Click the "Add Germ" button
2. Fill in the required fields:
   - **Code**: Unique identifier (required)
   - **Description**: Full description (required)
   - **Identifier**: Optional additional identifier
   - **Display Order**: Order for display purposes
3. Click "Create" to save

#### Editing an Existing Germ
1. Select a germ from the table by clicking on it
2. Click the "Edit" button
3. Modify the fields as needed
4. Click "Update" to save changes

#### Deleting a Germ
1. Select a germ from the table
2. Click the "Delete" button
3. Confirm the deletion in the dialog
4. The germ will be soft deleted (marked as deleted but not removed from database)

#### Searching and Filtering
- Use the search box to filter germs by code, description, or identifier
- Click column headers to sort by that field
- Use pagination controls to navigate through large datasets
- Adjust page size using the dropdown

## Technical Details

### API Validation
- Code and Description are required fields
- Code must be unique (not already exists)
- Maximum length validation for all text fields
- Soft delete implementation (sets IsDeleted flag)

### Frontend Features
- Real-time search filtering
- Sortable columns with visual indicators
- Pagination with configurable page sizes
- Responsive design for mobile devices
- Form validation with user feedback
- Loading states and error handling

### Security Considerations
- All operations require proper authentication
- Input validation on both client and server
- SQL injection protection through Entity Framework
- XSS protection through Angular's built-in sanitization

## File Structure
```
LIS.Api/
├── Controllers/
│   └── GermsController.cs
└── Models/
    └── Germs.cs (existing)

LIS.Web/src/app/
├── germs/
│   ├── germs.component.ts
│   ├── germs.component.html
│   └── germs.component.scss
├── services/
│   └── germs.service.ts
├── app.routes.ts (updated)
└── app.html (updated)
```

## Dependencies
- Angular 17+ with standalone components
- Bootstrap 5 for styling
- Bootstrap Icons for UI elements
- RxJS for reactive programming
- Entity Framework Core for data access

## Future Enhancements
- Bulk operations (import/export)
- Advanced filtering options
- Audit trail for changes
- Integration with other bacteriology features
- Data validation rules
- Custom field support














