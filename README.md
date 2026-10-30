# DBOM

**Database Object Model for .NET**

DBOM provides a database object model for inspecting and working with relational database schemas.

The object model is independent of the application consuming it. Connect using a connection string and provider name; DBOM handles the database context and exposes the resulting schema model.

## Getting Started

Connect to a SQL Server database:

```csharp
var conn = @"data source=;user id=;password=;initial catalog=AdventureWorks;persist security info=True;TrustServerCertificate=True";
var prov = @"System.Data.SqlClient";

var ctx = await DatabaseContextFactory.GetContext(conn, prov);
```

`ctx` is your database context and entry point to the object model.

## Providers

DBOM uses the supplied provider name to establish the database connection. SQL Server is a working implementation.

The object model is kept separate from the provider-specific connection details, allowing consumers to work with the schema through a consistent model.

## Design

- Standalone .NET library
- Provider-driven database connections
- Database schema represented as an object model
- No dependency on the JEO3 application

## License

MIT

## Examples

Usage:

<img width="870" height="736" alt="image" src="https://github.com/user-attachments/assets/5de20cd9-5b74-448f-8f60-c1447e084db2" />


Loading:

<img width="719" height="407" alt="image" src="https://github.com/user-attachments/assets/94128d92-acf0-4f7b-b64d-7b31dfa62715" />


