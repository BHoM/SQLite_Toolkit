/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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
using FluentAssertions;
using BH.Adapter.SQLite;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite;
using BH.oM.Base;
using SQLite_Toolkit.Tests.Base;

namespace SQLite_Toolkit.Tests
{
    [TestFixture]
    public class Phase4_DataRetrievalTests : SQLiteTestBase
    {
        [Test]
        public void Test_4_1_CustomSqlRequest_BasicExecution()
        {
            // Test 4.1: Custom SQL Execution
            // Objective: Test CustomSqlRequest functionality with basic SELECT statements

            // Arrange
            OpenTestConnection();

            // Create test table and insert data
            CreateTestTableWithData("DataRetrievalTable");

            CustomSqlRequest customRequest = new CustomSqlRequest()
            {
                SqlQuery = "SELECT * FROM DataRetrievalTable ORDER BY Id;",
                Parameters = new Dictionary<string, object>()
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(customRequest);

            // Assert
            results.Should().NotBeNull("CustomSqlRequest should return results");
            results.Should().NotBeEmpty("Should return data from table");

            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Query should be successful");
            queryResult.Data.Should().NotBeEmpty("Should contain data rows");
            queryResult.Data.Count.Should().BeGreaterThan(0, "Should have test data");

            // Verify data structure
            Dictionary<string, object> firstRow = queryResult.Data.First();
            firstRow.Should().ContainKey("Id", "Should contain Id column");
            firstRow.Should().ContainKey("Name", "Should contain Name column");
            firstRow.Should().ContainKey("Value", "Should contain Value column");

            CloseTestConnection();
        }

        [Test]
        public void Test_4_2_CustomSqlRequest_WithParameters()
        {
            // Test 4.2: Custom SQL with Parameters
            // Objective: Test CustomSqlRequest with parameterized queries

            // Arrange
            OpenTestConnection();

            // Create test table and insert data
            CreateTestTableWithData("ParameterTestTable");

            CustomSqlRequest customRequest = new CustomSqlRequest()
            {
                SqlQuery = "SELECT * FROM ParameterTestTable WHERE Id > @minId AND Value < @maxValue ORDER BY Id;",
                Parameters = new Dictionary<string, object>()
                {
                    {"@minId", 2},
                    {"@maxValue", 50.0}
                }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(customRequest);

            // Assert
            results.Should().NotBeNull("CustomSqlRequest should return results");
            results.Should().NotBeEmpty("Should return filtered data");

            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Parameterized query should be successful");
            queryResult.Data.Should().NotBeEmpty("Should contain filtered data");

            // Verify filtering worked
            foreach (Dictionary<string, object> row in queryResult.Data)
            {
                int id = System.Convert.ToInt32(row["Id"]);
                double value = System.Convert.ToDouble(row["Value"]);
                
                id.Should().BeGreaterThan(2, "Should respect @minId parameter");
                value.Should().BeLessThan(50.0, "Should respect @maxValue parameter");
            }

            CloseTestConnection();
        }

        [Test]
        public void Test_4_3_CustomSqlRequest_ComplexQuery()
        {
            // Test 4.3: Complex Custom SQL Query
            // Objective: Test CustomSqlRequest with complex SQL including joins, aggregations

            // Arrange
            OpenTestConnection();

            // Create related tables
            CreateTestTableWithData("Products");
            CreateCategoryTestTable("Categories");

            CustomSqlRequest customRequest = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT 
                        p.Id,
                        p.Name,
                        p.Value,
                        c.CategoryName,
                        CASE 
                            WHEN p.Value > 30 THEN 'High'
                            WHEN p.Value > 15 THEN 'Medium'
                            ELSE 'Low'
                        END as ValueCategory
                    FROM Products p
                    LEFT JOIN Categories c ON p.Id = c.ProductId
                    WHERE p.Value > @minValue
                    ORDER BY p.Value DESC;",
                Parameters = new Dictionary<string, object>()
                {
                    {"@minValue", 10.0}
                }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(customRequest);

            // Assert
            results.Should().NotBeNull("Complex query should return results");
            results.Should().NotBeEmpty("Should return joined data");

            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Complex query should be successful");
            queryResult.Data.Should().NotBeEmpty("Should contain joined data");

            // Verify complex query results
            Dictionary<string, object> firstRow = queryResult.Data.First();
            firstRow.Should().ContainKey("Id", "Should contain Id column");
            firstRow.Should().ContainKey("Name", "Should contain Name column");
            firstRow.Should().ContainKey("Value", "Should contain Value column");
            firstRow.Should().ContainKey("CategoryName", "Should contain joined CategoryName");
            firstRow.Should().ContainKey("ValueCategory", "Should contain calculated ValueCategory");

            CloseTestConnection();
        }

        [Test]
        public void Test_4_4_TableRequest_AdvancedFiltering()
        {
            // Test 4.4: Advanced TableRequest Filtering
            // Objective: Test TableRequest with complex WHERE conditions and sorting

            // Arrange
            OpenTestConnection();

            // Create test table with more data
            CreateTestTableWithData("AdvancedFilterTable", 20);

            TableRequest tableRequest = new TableRequest()
            {
                Name = "AdvancedFilterTable",
                Columns = new List<string> { "Id", "Name", "Value" },
                WhereConditions = new List<string> 
                { 
                    "Value > 15.0",
                    "Value < 45.0",
                    "Id % 2 = 0"  // Even IDs only
                },
                OrderBy = new List<string> { "Value DESC", "Id ASC" },
                Limit = 5,
                Offset = 0
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(tableRequest);

            // Assert
            results.Should().NotBeNull("TableRequest should return results");
            results.Should().NotBeEmpty("Should return filtered data");

            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Advanced filtering should be successful");
            queryResult.Data.Should().NotBeEmpty("Should contain filtered data");
            queryResult.Data.Count.Should().BeLessOrEqualTo(5, "Should respect LIMIT clause");

            // Verify filtering and sorting
            List<Dictionary<string, object>> sortedData = queryResult.Data.ToList();
            for (int i = 0; i < sortedData.Count; i++)
            {
                Dictionary<string, object> row = sortedData[i];
                
                int id = System.Convert.ToInt32(row["Id"]);
                double value = System.Convert.ToDouble(row["Value"]);
                
                // Verify WHERE conditions
                value.Should().BeGreaterThan(15.0, "Should respect Value > 15.0 condition");
                value.Should().BeLessThan(45.0, "Should respect Value < 45.0 condition");
                (id % 2).Should().Be(0, "Should respect Id % 2 = 0 condition (even IDs)");
                
                // Verify ORDER BY (Value DESC, Id ASC)
                if (i > 0)
                {
                    double prevValue = System.Convert.ToDouble(sortedData[i-1]["Value"]);
                    value.Should().BeLessOrEqualTo(prevValue, "Should be sorted by Value DESC");
                }
            }

            CloseTestConnection();
        }

        [Test]
        public void Test_4_5_TableRequest_PaginationAndDistinct()
        {
            // Test 4.5: TableRequest with Pagination and DISTINCT
            // Objective: Test LIMIT/OFFSET pagination and DISTINCT functionality

            // Arrange
            OpenTestConnection();

            // Create test table with duplicate values
            CreateTestTableWithDuplicates("PaginationTestTable");

            TableRequest tableRequest = new TableRequest()
            {
                Name = "PaginationTestTable",
                Columns = new List<string> { "Name", "Value" },
                Distinct = true,
                OrderBy = new List<string> { "Value ASC" },
                Limit = 3,
                Offset = 1
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(tableRequest);

            // Assert
            results.Should().NotBeNull("TableRequest should return results");
            results.Should().NotBeEmpty("Should return distinct paginated data");

            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Pagination query should be successful");
            queryResult.Data.Should().NotBeEmpty("Should contain paginated data");
            queryResult.Data.Count.Should().BeLessOrEqualTo(3, "Should respect LIMIT clause");

            // Verify DISTINCT worked (no duplicate Name-Value combinations)
            HashSet<string> uniqueRows = new HashSet<string>();
            foreach (Dictionary<string, object> row in queryResult.Data)
            {
                string rowKey = $"{row["Name"]}-{row["Value"]}";
                uniqueRows.Add(rowKey).Should().BeTrue("DISTINCT should eliminate duplicates");
            }

            CloseTestConnection();
        }

        [Test]
        public void Test_4_6_CustomSqlRequest_ErrorHandling()
        {
            // Test 4.6: Error Handling in CustomSqlRequest
            // Objective: Test error handling for invalid SQL queries

            // Arrange
            OpenTestConnection();

            CustomSqlRequest invalidRequest = new CustomSqlRequest()
            {
                SqlQuery = "SELECT * FROM NonExistentTable WHERE InvalidColumn = 'test';",
                Parameters = new Dictionary<string, object>()
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(invalidRequest);

            // Assert
            results.Should().NotBeNull("Should return results even for invalid query");

            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeFalse("Invalid query should fail");
            queryResult.ErrorMessage.Should().NotBeNullOrEmpty("Should contain error message");
            queryResult.Data.Should().BeNullOrEmpty("Should not contain data for failed query");

            CloseTestConnection();
        }

        [Test]
        public void Test_4_7_TableRequest_EmptyResults()
        {
            // Test 4.7: TableRequest with No Matching Results
            // Objective: Test handling of queries that return no results

            // Arrange
            OpenTestConnection();

            // Create test table with data
            CreateTestTableWithData("EmptyResultsTable");

            TableRequest tableRequest = new TableRequest()
            {
                Name = "EmptyResultsTable",
                Columns = new List<string> { "Id", "Name", "Value" },
                WhereConditions = new List<string> { "Value > 1000.0" }, // No data should match
                OrderBy = new List<string> { "Id ASC" }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(tableRequest);

            // Assert
            results.Should().NotBeNull("TableRequest should return results");
            results.Should().NotBeEmpty("Should return QueryResult even for empty data");

            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Query should be successful even with no results");
            queryResult.Data.Should().NotBeNull("Data should not be null");
            queryResult.Data.Should().BeEmpty("Should contain no data rows");

            CloseTestConnection();
        }

        [Test]
        public void Test_4_8_CustomSqlRequest_AggregationQueries()
        {
            // Test 4.8: CustomSqlRequest with Aggregation Functions
            // Objective: Test SQL queries with COUNT, SUM, AVG, MIN, MAX functions

            // Arrange
            OpenTestConnection();

            // Create test table with data
            CreateTestTableWithData("AggregationTestTable", 15);

            CustomSqlRequest aggregationRequest = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT 
                        COUNT(*) as TotalCount,
                        SUM(Value) as TotalValue,
                        AVG(Value) as AverageValue,
                        MIN(Value) as MinValue,
                        MAX(Value) as MaxValue,
                        COUNT(DISTINCT CAST(Value/10 AS INTEGER)) as ValueGroups
                    FROM AggregationTestTable
                    WHERE Value > @minValue;",
                Parameters = new Dictionary<string, object>()
                {
                    {"@minValue", 5.0}
                }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(aggregationRequest);

            // Assert
            results.Should().NotBeNull("Aggregation query should return results");
            results.Should().NotBeEmpty("Should return aggregated data");

            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Aggregation query should be successful");
            queryResult.Data.Should().NotBeEmpty("Should contain aggregated results");
            queryResult.Data.Count.Should().Be(1, "Aggregation should return single row");

            // Verify aggregation results
            Dictionary<string, object> aggregationRow = queryResult.Data.First();
            aggregationRow.Should().ContainKey("TotalCount", "Should contain COUNT result");
            aggregationRow.Should().ContainKey("TotalValue", "Should contain SUM result");
            aggregationRow.Should().ContainKey("AverageValue", "Should contain AVG result");
            aggregationRow.Should().ContainKey("MinValue", "Should contain MIN result");
            aggregationRow.Should().ContainKey("MaxValue", "Should contain MAX result");
            aggregationRow.Should().ContainKey("ValueGroups", "Should contain DISTINCT COUNT result");

            int totalCount = System.Convert.ToInt32(aggregationRow["TotalCount"]);
            totalCount.Should().BeGreaterThan(0, "Should have counted filtered rows");

            CloseTestConnection();
        }

        [Test]
        public void Test_4_9_TableRequest_ComplexColumnSelection()
        {
            // Test 4.9: TableRequest with Complex Column Selection
            // Objective: Test selective column retrieval and column aliasing

            // Arrange
            OpenTestConnection();

            // Create test table with data
            CreateTestTableWithData("ColumnSelectionTable");

            TableRequest tableRequest = new TableRequest()
            {
                Name = "ColumnSelectionTable",
                Columns = new List<string> { "Id", "Name" }, // Only select specific columns
                WhereConditions = new List<string> { "Id <= 5" },
                OrderBy = new List<string> { "Id ASC" }
            };

            // Act
            IEnumerable<object> results = TestAdapter.Pull(tableRequest);

            // Assert
            results.Should().NotBeNull("TableRequest should return results");
            results.Should().NotBeEmpty("Should return selected columns");

            QueryResult queryResult = results.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeTrue("Column selection query should be successful");
            queryResult.Data.Should().NotBeEmpty("Should contain selected data");

            // Verify only selected columns are returned
            foreach (Dictionary<string, object> row in queryResult.Data)
            {
                row.Should().ContainKey("Id", "Should contain selected Id column");
                row.Should().ContainKey("Name", "Should contain selected Name column");
                row.Should().NotContainKey("Value", "Should not contain unselected Value column");
            }

            CloseTestConnection();
        }

        #region Helper Methods

        /// <summary>
        /// Creates a test table with sample data for testing
        /// </summary>
        private void CreateTestTableWithData(string tableName, int rowCount = 10)
        {
            // Create table
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

            string createSql = BH.Engine.SQLite.Create.Table(tableSchema);
            if (!string.IsNullOrEmpty(createSql))
            {
                ExecuteCustomSql(createSql);

                // Insert test data
                for (int i = 1; i <= rowCount; i++)
                {
                    string insertSql = $"INSERT INTO {tableName} (Name, Value) VALUES ('Item_{i}', {i * 2.5});";
                    ExecuteCustomSql(insertSql);
                }
            }
        }

        /// <summary>
        /// Creates a category test table for join testing
        /// </summary>
        private void CreateCategoryTestTable(string tableName)
        {
            // Create category table
            TableSchema categorySchema = new TableSchema()
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
                        Name = "ProductId",
                        DataType = SqliteDataType.INTEGER,
                        AllowNull = false,
                        Position = 2
                    },
                    new Column()
                    {
                        Name = "CategoryName",
                        DataType = SqliteDataType.TEXT,
                        AllowNull = false,
                        Position = 3
                    }
                }
            };

            string createSql = BH.Engine.SQLite.Create.Table(categorySchema);
            if (!string.IsNullOrEmpty(createSql))
            {
                ExecuteCustomSql(createSql);

                // Insert category data
                string[] categories = { "Electronics", "Books", "Tools", "Food", "Clothing" };
                for (int i = 1; i <= 5; i++)
                {
                    string insertSql = $"INSERT INTO {tableName} (ProductId, CategoryName) VALUES ({i}, '{categories[i-1]}');";
                    ExecuteCustomSql(insertSql);
                }
            }
        }

        /// <summary>
        /// Creates a test table with duplicate values for DISTINCT testing
        /// </summary>
        private void CreateTestTableWithDuplicates(string tableName)
        {
            // Create table
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
                        Position = 3
                    }
                }
            };

            string createSql = BH.Engine.SQLite.Create.Table(tableSchema);
            if (!string.IsNullOrEmpty(createSql))
            {
                ExecuteCustomSql(createSql);

                // Insert duplicate data
                string[] names = { "Alpha", "Beta", "Gamma", "Alpha", "Beta", "Delta", "Gamma", "Alpha" };
                double[] values = { 10.0, 20.0, 30.0, 10.0, 20.0, 40.0, 30.0, 10.0 };

                for (int i = 0; i < names.Length; i++)
                {
                    string insertSql = $"INSERT INTO {tableName} (Name, Value) VALUES ('{names[i]}', {values[i]});";
                    ExecuteCustomSql(insertSql);
                }
            }
        }

        #endregion
    }
} 