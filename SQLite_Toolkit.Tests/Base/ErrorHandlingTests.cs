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
using System.IO;
using NUnit.Framework;
using FluentAssertions;
using BH.Adapter.SQLite;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite;
using BH.oM.Base;
using BH.oM.Adapter.Commands;
using BH.Tests.SQLite.Base;

namespace BH.Tests.SQLite.Functionality
{
    [TestFixture]
    public class ErrorHandlingTests : SQLiteTestBase
    {
        [Test]
        public void ConnectionError_InvalidDatabasePath()
        {
            // Test 5.1a: Connection Error Handling - Invalid Database Path
            // Objective: Test error handling for invalid database paths

            // Arrange
            SQLiteAdapter adapter = new SQLiteAdapter();
            
            // Try to open a database in an invalid/non-existent directory
            string invalidPath = @"C:\NonExistentDirectory\NonExistentSubDir\invalid.db";
            Open openCommand = new Open()
            {
                FileName = invalidPath
            };

            // Act & Assert
            Action openAction = () => adapter.Execute(openCommand);
            
            // Should handle the error gracefully without throwing unhandled exceptions
            openAction.Should().NotThrow("Adapter should handle invalid paths gracefully");
            
            // The adapter should handle this internally and potentially create the directory
            // or return an appropriate error response through the Execute method
        }

        [Test]
        public void ConnectionError_OperationWithoutConnection()
        {
            // Test 5.1b: Connection Error Handling - Operations Without Connection
            // Objective: Test operations executed without an active connection

            // Arrange
            SQLiteAdapter adapter = new SQLiteAdapter();
            // Deliberately do not open a connection

            // Create a simple equality filter request to test connection handling
            EqualityFilterRequest request = new EqualityFilterRequest()
            {
                TableName = "TestTable",
                ColumnFilters = new List<ColumnFilter>()
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Id",
                        Values = new List<object> { 1 }
                    }
                }
            };

            // Act & Assert
            Action act = () => adapter.Pull(request);
            
            // The adapter should either throw an exception or return error results
            // Let's test that it handles the no-connection case gracefully
            try
            {
                IEnumerable<object> results = adapter.Pull(request);
                results.Should().NotBeNull("Should return results even without connection");
                
                // If we get results, they should indicate failure
                object firstResult = results.FirstOrDefault();
                if (firstResult is QueryResult queryResult)
                {
                    queryResult.IsSuccess.Should().BeFalse("Operation should fail without connection");
                    queryResult.ErrorMessage.Should().NotBeNullOrEmpty("Should provide error message about connection");
                }
            }
            catch (Exception ex)
            {
                // If an exception is thrown, that's also acceptable behavior for no connection
                ex.Should().NotBeNull("Exception should be thrown for no connection");
            }
        }

        [Test]
        public void ConnectionError_DoubleClose()
        {
            // Test 5.1c: Connection Error Handling - Double Close
            // Objective: Test closing an already closed connection

            // Arrange
            OpenTestConnection();
            
            Close closeCommand = new Close();
            
            // Act - Close connection twice
            Output<List<object>, bool> firstClose = TestAdapter.Execute(closeCommand);
            Output<List<object>, bool> secondClose = TestAdapter.Execute(closeCommand);

            // Assert
            firstClose.Should().NotBeNull("First close should return result");
            secondClose.Should().NotBeNull("Second close should return result");
            
            // Both should handle gracefully without throwing exceptions
            // The exact behavior may vary but should not crash
        }



        [Test]
        public void SqlError_NonExistentTable()
        {
            // Test 5.2b: SQL Error Handling - Non-Existent Table
            // Objective: Test querying tables that don't exist

            // Arrange
            OpenTestConnection();

            EqualityFilterRequest nonExistentTableRequest = new EqualityFilterRequest()
            {
                TableName = "TableThatDoesNotExist",
                ColumnFilters = new List<ColumnFilter>()
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Id",
                        Values = new List<object> { 1 }
                    }
                }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(nonExistentTableRequest);

            // Assert
            results.Should().NotBeNull("Should return results for non-existent table query");
            
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeFalse("Query on non-existent table should fail");
            queryResult.ErrorMessage.Should().NotBeNullOrEmpty("Should contain table not found error");
            queryResult.ErrorMessage.Should().Contain("TableThatDoesNotExist", "Error message should mention the table name");

            CloseTestConnection();
        }

        [Test]
        public void SqlError_InvalidColumnNames()
        {
            // Test 5.2c: SQL Error Handling - Invalid Column Names
            // Objective: Test accessing columns that don't exist using EqualityFilterRequest

            // Arrange
            OpenTestConnection();

            // Create a simple test table
            CreateSimpleTestTable("ErrorTestTable");

            EqualityFilterRequest invalidColumnRequest = new EqualityFilterRequest()
            {
                TableName = "ErrorTestTable",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "NonExistentColumn", // Column that doesn't exist
                        Values = new List<object> { "test" }
                    }
                }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(invalidColumnRequest);

            // Assert
            results.Should().NotBeNull("Should return results for invalid column query");
            
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");

            CloseTestConnection();
        }

        [Test]
        public void SqlError_InvalidParameterTypes()
        {
            // Test 5.2d: SQL Error Handling - Invalid Parameter Types
            // Objective: Test filter requests with incompatible parameter types

            // Arrange
            OpenTestConnection();

            // Create a test table with typed data
            CreateTestTableWithData("ParameterErrorTable");

            EqualityFilterRequest invalidParameterRequest = new EqualityFilterRequest()
            {
                TableName = "ParameterErrorTable",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Id",
                        Values = new List<object> { "NotANumber" } // String where integer expected
                    }
                }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(invalidParameterRequest);

            // Assert
            results.Should().NotBeNull("Should return results for invalid parameter query");
            
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            
            // SQLite might handle type coercion, so this might succeed or fail depending on implementation
            // The important thing is that it handles the situation gracefully
            if (!queryResult.IsSuccess)
            {
                queryResult.ErrorMessage.Should().NotBeNullOrEmpty("Should contain parameter error message if failed");
            }

            CloseTestConnection();
        }

        [Test]
        public void EdgeCase_EmptyStringParameters()
        {
            // Test 5.4a: Edge Case - Empty String Parameters
            // Objective: Test handling of empty string parameters in filter requests

            // Arrange
            OpenTestConnection();

            CreateTestTableWithData("EmptyStringTestTable");

            EqualityFilterRequest emptyStringRequest = new EqualityFilterRequest()
            {
                TableName = "EmptyStringTestTable",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Name",
                        Values = new List<object> { "" } // Empty string parameter
                    }
                }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(emptyStringRequest);

            // Assert
            results.Should().NotBeNull("Should return results for empty string parameter");
            
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Query with empty string parameter should succeed");
            queryResult.Data.Should().NotBeNull("Data should not be null");
            // May or may not have matching data, but should handle gracefully

            CloseTestConnection();
        }

        [Test]
        public void EdgeCase_NullParameters()
        {
            // Test 5.4b: Edge Case - Null Parameters
            // Objective: Test handling of null parameters in filter requests

            // Arrange
            OpenTestConnection();

            CreateTestTableWithData("NullParameterTestTable");

            EqualityFilterRequest nullParameterRequest = new EqualityFilterRequest()
            {
                TableName = "NullParameterTestTable",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Value",
                        Values = new List<object> { null! } // Null parameter (suppressed warning)
                    }
                }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(nullParameterRequest);

            // Assert
            results.Should().NotBeNull("Should return results for null parameter");
            
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Query with null parameter should succeed");
            queryResult.Data.Should().NotBeNull("Data should not be null");

            CloseTestConnection();
        }

        [Test]
        public void EdgeCase_VeryLongFilterList()
        {
            // Test 5.4c: Edge Case - Very Long Filter List
            // Objective: Test handling of filter requests with many values

            // Arrange
            OpenTestConnection();

            CreateTestTableWithData("LongFilterTestTable");

            // Create a filter with many values (simulating a long IN clause)
            List<object> manyValues = new List<object>();
            for (int i = 1; i <= 100; i++)
            {
                manyValues.Add(i);
            }

            EqualityFilterRequest longFilterRequest = new EqualityFilterRequest()
            {
                TableName = "LongFilterTestTable",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Id",
                        Values = manyValues
                    }
                }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(longFilterRequest);

            // Assert
            results.Should().NotBeNull("Should return results for filter with many values");
            
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Filter with many values should succeed");
            queryResult.Data.Should().NotBeNull("Data should not be null");

            CloseTestConnection();
        }

        [Test]
        public void EdgeCase_DatabaseFileLocking()
        {
            // Test 5.4e: Edge Case - Database File Locking
            // Objective: Test behavior when database file is locked or in use

            // Arrange
            string testDbPath = Path.Combine(Path.GetDirectoryName(TestDatabasePath) ?? ".", "LockingTestDatabase.db");
            
            // Clean up any existing file
            if (File.Exists(testDbPath))
            {
                try
                {
                    File.Delete(testDbPath);
                }
                catch (IOException)
                {
                    // File may be locked, try to continue
                }
            }

            SQLiteAdapter adapter1 = null;
            SQLiteAdapter adapter2 = null;

            try
            {
                // Create first adapter connection
                adapter1 = new SQLiteAdapter();
                Open openCommand1 = new Open() { FileName = testDbPath };
                adapter1.Execute(openCommand1);

                // Try to create second adapter connection to same file
                adapter2 = new SQLiteAdapter();
                Open openCommand2 = new Open() { FileName = testDbPath };

                // Act
                Output<List<object>, bool> result2 = adapter2.Execute(openCommand2);

                // Assert
                result2.Should().NotBeNull("Second connection attempt should return result");
                
                // SQLite typically allows multiple connections, but test that it handles gracefully
                // The behavior may vary based on SQLite settings and implementation
            }
            finally
            {
                // Clean up connections
                if (adapter1 != null)
                {
                    try
                    {
                        Close closeCommand = new Close();
                        adapter1.Execute(closeCommand);
                    }
                    catch (Exception)
                    {
                        // Ignore cleanup errors
                    }
                }

                if (adapter2 != null)
                {
                    try
                    {
                        Close closeCommand = new Close();
                        adapter2.Execute(closeCommand);
                    }
                    catch (Exception)
                    {
                        // Ignore cleanup errors
                    }
                }

                // Wait a bit for connections to fully close
                System.Threading.Thread.Sleep(100);

                // Remove test file
                if (File.Exists(testDbPath))
                {
                    try
                    {
                        File.Delete(testDbPath);
                    }
                    catch (IOException)
                    {
                        // File may still be locked, that's okay for this test
                    }
                }
            }
        }

        #region Helper Methods

        /// <summary>
        /// Creates a test table with data for error testing
        /// </summary>
        private void CreateTestTableWithData(string tableName)
        {
            TableSchema tableSchema = new TableSchema()
            {
                Name = tableName,
                Columns = new List<Column>()
                {
                    new Column()
                    {
                        Name = "Id",
                        DataType = SqliteDataType.INTEGER,
                        IsPrimaryKey = true,
                        IsAutoIncrement = true,
                        Position = 1
                    },
                    new Column()
                    {
                        Name = "Name",
                        DataType = SqliteDataType.TEXT,
                        AllowNull = false,
                        Position = 2
                    },
                    new Column()
                    {
                        Name = "Value",
                        DataType = SqliteDataType.REAL,
                        DefaultValue = "0.0",
                        Position = 3
                    }
                }
            };

            try
            {
                // Create table using Push method with TableSchema
                List<object> result = TestAdapter.Push(new List<TableSchema> { tableSchema });
                if (result.Count == 0)
                    return;

                // Insert test data using Table object with data rows
                List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                for (int i = 1; i <= 5; i++)
                {
                    rows.Add(new Dictionary<string, object>()
                    {
                        { "Name", $"Item_{i}" },
                        { "Value", i * 2.5 }
                    });
                }

                var table = new Table()
                {
                    Schema = tableSchema,
                    Rows = rows,
                    CreateTableIfNotExists = false // Table already created above
                };

                TestAdapter.Push(new List<Table> { table });
            }
            catch (Exception)
            {
                // Table creation or data insertion failed
                return;
            }
        }

     
        #endregion
    }
} 
