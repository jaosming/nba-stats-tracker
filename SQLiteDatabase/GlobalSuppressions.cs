// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "SQLite_Database.SQLiteDatabase.#GetDataTable(System.String,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "SQLite_Database.SQLiteDatabase.#ExecuteNonQuery(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "SQLite_Database.SQLiteDatabase.#ExecuteScalar(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "SQLite_Database.SQLiteDatabase.#UpdateManyTransaction(System.String,System.Collections.Generic.List`1<System.Collections.Generic.Dictionary`2<System.String,System.String>>,System.Collections.Generic.List`1<System.String>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "SQLite_Database.SQLiteDatabase.#InsertManyTransaction(System.String,System.Collections.Generic.List`1<System.Collections.Generic.Dictionary`2<System.String,System.String>>)")]
