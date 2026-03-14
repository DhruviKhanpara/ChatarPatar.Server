/*
==================================================================================
This script creates:
- DB, DB user / roles, Grant privileges
==================================================================================
*/

-- ============================================================================
-- SECTION: DATABASE CREATION (Optional - comment out if database exists)
-- ============================================================================

USE master;
GO

IF DB_ID('ChatarPatar') IS NOT NULL
BEGIN
    ALTER DATABASE ChatarPatar SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ChatarPatar;
END
GO

CREATE DATABASE ChatarPatar;
GO
