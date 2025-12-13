# Department Management System

## Overview
The Department Management System allows System Admins to create departments and assign any person (HR Admins, System Admins, Employees, or Line Managers) to departments. People working in the same department can see all other members of their department.

## Features

### 1. Create Departments (System Admin Only)
- **Location:** Dashboard → "Department Management" → "Create New Department"
- **Access:** System Admins only
- **Features:**
  - Create new departments with a name
  - Optional: Add department purpose/description
  - Optional: Assign a department head
  - Validation: Prevents duplicate department names

### 2. Assign People to Departments (System Admin Only)
- **Location:** Dashboard → "Department Management" → "Assign People to Departments"
- **Access:** System Admins only
- **Features:**
  - Assign any active person (Employee, HR Admin, System Admin, Line Manager) to any department
  - View current department assignments
  - See all roles for each person
  - Change department assignments

### 3. View Department Members (All Department Members)
- **Location:** Dashboard → "My Department" → "View Department Members"
- **Access:** 
  - Anyone in the same department can view all members
  - System Admins and HR Admins can view any department
- **Features:**
  - View all people in your department (regardless of role)
  - See names, emails, positions, and roles
  - Click to view individual profiles
  - See department head information

### 4. Department Management Dashboard (System Admin Only)
- **Location:** Dashboard → "Department Management" → "Manage Departments"
- **Access:** System Admins only
- **Features:**
  - List all departments
  - View department statistics (total members, department head)
  - View members of each department
  - Assign people to departments
  - Delete departments (only if no active employees)

## Navigation

### For System Admins:
1. **Dashboard** → "Department Management" card
   - "Manage Departments" - View all departments
   - "Create New Department" - Create a new department
   - "Assign People to Departments" - Assign people to departments

2. **Dashboard** → "My Department" card (if assigned to a department)
   - "View Department Members" - See all people in your department

### For All Users (if assigned to a department):
1. **Dashboard** → "My Department" card
   - "View Department Members" - See all people in your department

## Access Control

### System Admins:
- ✅ Can create departments
- ✅ Can assign any person to any department
- ✅ Can view all departments
- ✅ Can view members of any department
- ✅ Can delete departments (if no active employees)

### HR Admins:
- ✅ Can view members of any department
- ❌ Cannot create departments
- ❌ Cannot assign people to departments

### Line Managers:
- ✅ Can view members of their own department only
- ❌ Cannot create departments
- ❌ Cannot assign people to departments

### Employees:
- ✅ Can view members of their own department only
- ❌ Cannot create departments
- ❌ Cannot assign people to departments

## Files Created

1. **HRMS/Controllers/DepartmentController.cs**
   - `Index()` - List all departments (System Admin)
   - `Create()` - Create new department (GET/POST) (System Admin)
   - `AssignToDepartment()` - Assign people to departments (GET/POST) (System Admin)
   - `ViewMembers()` - View department members (All department members)
   - `Delete()` - Delete department (System Admin)

2. **HRMS/Views/Department/**
   - `Index.cshtml` - Department management dashboard
   - `Create.cshtml` - Create department form
   - `AssignToDepartment.cshtml` - Assign people to departments form
   - `ViewMembers.cshtml` - View department members list

3. **HRMS/Views/Home/Index.cshtml**
   - Added "Department Management" card for System Admins
   - Added "My Department" card for all users (if assigned to a department)

## Database Schema

The system uses the existing `Department` and `Employee` tables:

- **Department** table:
  - `department_id` (Primary Key)
  - `department_name` (Required)
  - `purpose` (Optional)
  - `department_head_id` (Foreign Key to Employee, Optional)

- **Employee** table:
  - `employee_id` (Primary Key)
  - `department_id` (Foreign Key to Department, Optional)
  - Other employee fields...

## Usage Examples

### Creating a Department:
1. Log in as System Admin
2. Go to Dashboard
3. Click "Department Management" → "Create New Department"
4. Enter department name (e.g., "Engineering")
5. Optionally add purpose and select department head
6. Click "Create Department"

### Assigning People to Departments:
1. Log in as System Admin
2. Go to Dashboard
3. Click "Department Management" → "Assign People to Departments"
4. Select a person from the dropdown
5. Select a department
6. Click "Assign to Department"

### Viewing Department Members:
1. Log in with any account
2. If assigned to a department, go to Dashboard
3. Click "My Department" → "View Department Members"
4. See all people in your department with their roles

## Notes

- All people (Employees, HR Admins, System Admins, Line Managers) can be assigned to departments
- People in the same department can see all other members regardless of role
- System Admins can manage all departments
- Departments cannot be deleted if they have active employees (must reassign first)
- Department assignments are visible in employee profiles

