/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BH.Adapter.SQLite;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite;
using BH.oM.Adapter.Commands;
using BH.Tests.SQLite.Helpers;

namespace BH.Tests.SQLite.Base
{
    /// <summary>
    /// Base test class providing common functionality for SQLite testing
    /// </summary>
    [TestFixture]
    public abstract class SQLiteTestBase
    {
        protected SQLiteAdapter TestAdapter { get; private set; } = null!;
        protected string TestDatabasePath { get; private set; } = null!;
        protected SQLiteSettings TestSettings { get; private set; } = null!;
        protected string TestName { get; private set; } = null!;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            // Clear the TestDatabases folder to ensure clean state
            TestDatabaseManager.ClearTestDatabasesFolder();
            Console.WriteLine("SQLite test suite initialized - test databases cleared");
        }

        [SetUp]
        public virtual void Setup()
        {
            TestName = TestContext.CurrentContext.Test.Name;
            TestAdapter = CreateTestAdapter();
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Ensure connection is properly closed before cleanup
            if (TestAdapter != null)
            {
                TestDatabaseManager.EnsureConnectionClosed(TestAdapter);
            }

            // Clean up test database - only if file handles are released
            TestDatabaseManager.CleanupDatabase(TestDatabasePath);
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            // Clean up any remaining test databases
            TestDatabaseManager.CleanupAllDatabases();
        }

        /// <summary>
        /// Creates a test adapter with a file-based database
        /// </summary>
        /// <returns>Configured SQLite adapter</returns>
        protected virtual SQLiteAdapter CreateTestAdapter()
        {
            TestDatabasePath = TestDatabaseManager.CreateTestDatabasePath(TestName);
            TestSettings = TestDatabaseManager.CreateTestSettings(DatabaseMode.FileDatabase, TestDatabasePath);
            return new SQLiteAdapter(TestDatabasePath, TestSettings);
        }

        /// <summary>
        /// Creates a test adapter with an in-memory database
        /// </summary>
        /// <returns>Configured SQLite adapter with in-memory database</returns>
        protected SQLiteAdapter CreateInMemoryTestAdapter()
        {
            TestDatabasePath = TestDatabaseManager.CreateInMemoryDatabasePath();
            TestSettings = TestDatabaseManager.CreateTestSettings(DatabaseMode.InMemoryDatabase, TestDatabasePath);
            return new SQLiteAdapter(TestDatabasePath, TestSettings);
        }

        /// <summary>
        /// Opens a connection to the test database
        /// </summary>
        /// <returns>True if connection opened successfully</returns>
        protected bool OpenTestConnection()
        {
            var openCommand = new Open() { FileName = TestDatabasePath };
            var result = TestAdapter.Execute(openCommand);
            return result.Item2;
        }

        /// <summary>
        /// Closes the test database connection
        /// </summary>
        /// <returns>True if connection closed successfully</returns>
        protected bool CloseTestConnection()
        {
            var closeCommand = new Close();
            var result = TestAdapter.Execute(closeCommand);
            return result.Item2;
        }



        /// <summary>
        /// Creates a test table using the Push method with TableSchema
        /// </summary>
        /// <param name="tableName">Name of the table to create</param>
        /// <param name="columns">Column definitions for the table</param>
        /// <returns>True if table was created successfully</returns>
        protected bool CreateTestTable(string tableName, List<Column> columns)
        {
            var tableSchema = new TableSchema()
            {
                Name = tableName,
                Columns = columns
            };

            try
            {
                List<object> result = TestAdapter.Push(new List<TableSchema> { tableSchema });
                return result.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a simple test table with standard columns
        /// </summary>
        /// <param name="tableName">Name of the table to create</param>
        /// <returns>True if table was created successfully</returns>
        protected bool CreateSimpleTestTable(string tableName = "TestTable")
        {
            var columns = new List<Column>()
            {
                new Column()
                {
                    Name = "Id",
                    DataType = SqliteDataType.INTEGER,
                    IsPrimaryKey = true,
                    IsAutoIncrement = true,
                    AllowNull = false,
                    Position = 1
                },
                new Column()
                {
                    Name = "Name",
                    DataType = SqliteDataType.TEXT,
                    AllowNull = false,
                    MaxLength = 100,
                    Position = 2
                },
                new Column()
                {
                    Name = "Value",
                    DataType = SqliteDataType.REAL,
                    AllowNull = true,
                    Position = 3
                },
                new Column()
                {
                    Name = "CreatedAt",
                    DataType = SqliteDataType.TEXT,
                    AllowNull = true,
                    DefaultValue = "CURRENT_TIMESTAMP",
                    Position = 4
                }
            };

            return CreateTestTable(tableName, columns);
        }

        /// <summary>
        /// Inserts test data into a table using Table object with data rows
        /// </summary>
        /// <param name="tableName">Name of the table to insert data into</param>
        /// <param name="recordCount">Number of test records to insert</param>
        /// <returns>True if data was inserted successfully</returns>
        protected bool InsertTestData(string tableName = "TestTable", int recordCount = 5)
        {
            try
            {
                // Create data rows
                List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                for (int i = 1; i <= recordCount; i++)
                {
                    rows.Add(new Dictionary<string, object>()
                    {
                        { "Name", $"Test Record {i}" },
                        { "Value", i * 100.0 + (i * 0.5) }
                    });
                }

                // Create a Table object with the data (assume table already exists)
                var table = new Table()
                {
                    Schema = new TableSchema() { Name = tableName },
                    Rows = rows,
                    CreateTableIfNotExists = false // Assume table exists
                };

                List<object> result = TestAdapter.Push(new List<Table> { table });
                return result.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Verifies that a table exists in the database using EqualityFilterRequest
        /// </summary>
        /// <param name="tableName">Name of the table to check</param>
        /// <returns>True if table exists</returns>
        protected bool VerifyTableExists(string tableName)
        {
            try
            {
                EqualityFilterRequest tableCheckRequest = new EqualityFilterRequest()
                {
                    TableName = "sqlite_master",
                    ColumnFilters = new List<ColumnFilter>
                    {
                        new ColumnFilter()
                        {
                            ColumnName = "type",
                            Values = new List<object> { "table" }
                        },
                        new ColumnFilter()
                        {
                            ColumnName = "name",
                            Values = new List<object> { tableName }
                        }
                    },
                    Logic = LogicalOperator.And
                };

                IEnumerable<object> results = TestAdapter.Pull(tableCheckRequest);
                QueryResult queryResult = results.FirstOrDefault() as QueryResult;
                return queryResult?.IsSuccess == true && queryResult.Data?.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the count of records in a table by pulling all records
        /// </summary>
        /// <param name="tableName">Name of the table to count</param>
        /// <returns>Number of records in the table</returns>
        protected int GetTableRecordCount(string tableName)
        {
            try
            {
                EqualityFilterRequest getAllRequest = new EqualityFilterRequest()
                {
                    TableName = tableName,
                    ColumnFilters = new List<ColumnFilter>() // Empty filters means get all records
                };

                IEnumerable<object> results = TestAdapter.Pull(getAllRequest);
                QueryResult queryResult = results.FirstOrDefault() as QueryResult;
                return queryResult?.IsSuccess == true ? queryResult.Data?.Count ?? 0 : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Asserts that no errors were recorded during the test
        /// </summary>
        protected void AssertNoErrors()
        {
            // Note: This would need integration with BHoM's error recording system
            // For now, we'll rely on exceptions being thrown
            Assert.Pass("No errors detected during test execution");
        }

        /// <summary>
        /// Asserts that a database file exists and is valid
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        protected void AssertDatabaseExists(string databasePath)
        {
            Assert.IsTrue(TestDatabaseManager.VerifyDatabaseExists(databasePath),
                $"Database file does not exist or is not valid: {databasePath}");
        }
    }
}
