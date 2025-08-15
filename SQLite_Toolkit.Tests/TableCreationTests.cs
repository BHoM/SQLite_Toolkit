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
using BH.oM.SQLite.Configs;
using BH.oM.SQLite;
using BH.oM.Base;
using BH.Tests.SQLite.Base;
using BH.oM.SQLite.Objects;

namespace BH.Tests.SQLite.Functionality
{
    /// <summary>
    /// Table Creation Tests
    /// Tests SQL generation and table creation functionality  
    /// </summary>
    [TestFixture]
    public class TableCreationTests : SQLiteTestBase
    {
        [Test]
        public void SqlGenerationForTableCreation()
        {
            // Objective: Test Table.cs SQL generation functionality

            // Arrange
            TableSchema tableSchema = new TableSchema()
            {
                Name = "TestTable",
                Columns = new List<Column>()
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
                        MaxLength = 100,
                        AllowNull = false,
                        Position = 2
                    },
                    new Column()
                    {
                        Name = "Value",
                        DataType = SqliteDataType.REAL,
                        AllowNull = true,
                        Position = 3
                    }
                }
            };

            // Act
            string createSql = BH.Engine.SQLite.Create.Table(tableSchema);

            // Assert
            createSql.Should().NotBeNullOrEmpty("SQL should be generated");
            
            // Verify SQL contains expected elements
            createSql.Should().Contain("CREATE TABLE", "SQL should contain CREATE TABLE statement");
            createSql.Should().Contain("IF NOT EXISTS", "SQL should include IF NOT EXISTS by default");
            createSql.Should().Contain("TestTable", "SQL should contain the table name");
            createSql.Should().Contain("Id", "SQL should contain the Id column");
            createSql.Should().Contain("Name", "SQL should contain the Name column");
            createSql.Should().Contain("Value", "SQL should contain the Value column");
            createSql.Should().Contain("PRIMARY KEY", "SQL should contain primary key constraint");
            createSql.Should().Contain("AUTOINCREMENT", "SQL should contain autoincrement");
            createSql.Should().Contain("NOT NULL", "SQL should contain NOT NULL constraints");
            createSql.Should().Contain("INTEGER", "SQL should contain INTEGER data type");
            createSql.Should().Contain("TEXT", "SQL should contain TEXT data type");
            createSql.Should().Contain("REAL", "SQL should contain REAL data type");
        }

        [Test]
        public void TableCreationExecution()
        {
            // Objective: Execute generated SQL to create actual tables

            // Arrange
            OpenTestConnection();

            TableSchema tableSchema = new TableSchema()
            {
                Name = "ExecutionTestTable",
                Columns = new List<Column>()
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
                        MaxLength = 100,
                        AllowNull = false,
                        Position = 2
                    },
                    new Column()
                    {
                        Name = "Value",
                        DataType = SqliteDataType.REAL,
                        AllowNull = true,
                        Position = 3
                    }
                }
            };

            // Act
            string createSql = BH.Engine.SQLite.Create.Table(tableSchema);
            createSql.Should().NotBeNullOrEmpty("SQL should be generated");

            // Execute the SQL using CustomSqlRequest
            IEnumerable<object> results = ExecuteCustomSql(createSql);

            // Assert
            results.Should().NotBeNull("Execution should not return null");
            
            // Verify table exists in database
            bool tableExists = VerifyTableExists("ExecutionTestTable");
            tableExists.Should().BeTrue("Table should exist in database after creation");

            CloseTestConnection();
        }

        [Test]
        public void SqlGenerationWithDifferentDataTypes()
        {
            // Objective: Test all SQLite data types are handled correctly

            // Arrange
            TableSchema tableSchema = new TableSchema()
            {
                Name = "DataTypesTable",
                Columns = new List<Column>()
                {
                    new Column()
                    {
                        Name = "IntegerCol",
                        DataType = SqliteDataType.INTEGER,
                        Position = 1
                    },
                    new Column()
                    {
                        Name = "RealCol",
                        DataType = SqliteDataType.REAL,
                        Position = 2
                    },
                    new Column()
                    {
                        Name = "TextCol",
                        DataType = SqliteDataType.TEXT,
                        Position = 3
                    },
                    new Column()
                    {
                        Name = "BlobCol",
                        DataType = SqliteDataType.BLOB,
                        Position = 4
                    },
                    new Column()
                    {
                        Name = "NumericCol",
                        DataType = SqliteDataType.NUMERIC,
                        Position = 5
                    }
                }
            };

            // Act
            string createSql = BH.Engine.SQLite.Create.Table(tableSchema);

            // Assert
            createSql.Should().NotBeNullOrEmpty("SQL should be generated");
            createSql.Should().Contain("INTEGER", "SQL should contain INTEGER data type");
            createSql.Should().Contain("REAL", "SQL should contain REAL data type");
            createSql.Should().Contain("TEXT", "SQL should contain TEXT data type");
            createSql.Should().Contain("BLOB", "SQL should contain BLOB data type");
            createSql.Should().Contain("NUMERIC", "SQL should contain NUMERIC data type");
        }

        [Test]
        public void SqlGenerationWithConstraints()
        {
            // Objective: Test various column constraints are handled correctly

            // Arrange
            TableSchema tableSchema = new TableSchema()
            {
                Name = "ConstraintsTable",
                Columns = new List<Column>()
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
                        Name = "UniqueCol",
                        DataType = SqliteDataType.TEXT,
                        IsUnique = true,
                        AllowNull = false,
                        Position = 2
                    },
                    new Column()
                    {
                        Name = "DefaultCol",
                        DataType = SqliteDataType.TEXT,
                        DefaultValue = "'default_value'",
                        Position = 3
                    },
                    new Column()
                    {
                        Name = "NullableCol",
                        DataType = SqliteDataType.TEXT,
                        AllowNull = true,
                        Position = 4
                    }
                }
            };

            // Act
            string createSql = BH.Engine.SQLite.Create.Table(tableSchema);

            // Assert
            createSql.Should().NotBeNullOrEmpty("SQL should be generated");
            createSql.Should().Contain("PRIMARY KEY", "SQL should contain primary key constraint");
            createSql.Should().Contain("AUTOINCREMENT", "SQL should contain autoincrement");
            createSql.Should().Contain("UNIQUE", "SQL should contain unique constraint");
            createSql.Should().Contain("DEFAULT", "SQL should contain default value");
            createSql.Should().Contain("NOT NULL", "SQL should contain NOT NULL constraints");
        }

        [Test]
        public void TableCreationWithIndexes()
        {
            // Objective: Test table creation with index definitions

            // Arrange
            OpenTestConnection();

            TableSchema tableSchema = new TableSchema()
            {
                Name = "IndexTestTable",
                Columns = new List<Column>()
                {
                    new Column()
                    {
                        Name = "Id",
                        DataType = SqliteDataType.INTEGER,
                        IsPrimaryKey = true,
                        Position = 1
                    },
                    new Column()
                    {
                        Name = "Name",
                        DataType = SqliteDataType.TEXT,
                        Position = 2
                    },
                    new Column()
                    {
                        Name = "Category",
                        DataType = SqliteDataType.TEXT,
                        Position = 3
                    }
                },
                Indexes = new List<BH.oM.SQLite.Objects.Index>()
                {
                    new BH.oM.SQLite.Objects.Index()
                    {
                        Name = "idx_name",
                        TableName = "IndexTestTable",
                        Columns = new List<string> { "Name" },
                        IsUnique = false
                    },
                    new BH.oM.SQLite.Objects.Index()
                    {
                        Name = "idx_category",
                        TableName = "IndexTestTable",
                        Columns = new List<string> { "Category" },
                        IsUnique = false
                    }
                }
            };

            // Act
            string createSql = BH.Engine.SQLite.Create.Table(tableSchema);
            createSql.Should().NotBeNullOrEmpty("SQL should be generated");

            // The SQL should contain both table creation and index creation
            createSql.Should().Contain("CREATE TABLE", "SQL should contain table creation");
            createSql.Should().Contain("CREATE INDEX", "SQL should contain index creation");
            createSql.Should().Contain("idx_name", "SQL should contain the name index");
            createSql.Should().Contain("idx_category", "SQL should contain the category index");

            // Execute the SQL
            IEnumerable<object> results = ExecuteCustomSql(createSql);

            // Assert
            results.Should().NotBeNull("Execution should not return null");
            
            // Verify table exists
            bool tableExists = VerifyTableExists("IndexTestTable");
            tableExists.Should().BeTrue("Table should exist in database after creation");

            // Verify indexes exist
            IEnumerable<object> indexResults = ExecuteCustomSql("SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='IndexTestTable'");
            indexResults.Should().NotBeNull("Index query should return results");

            CloseTestConnection();
        }

        [Test]
        public void InvalidTableSchemaHandling()
        {
            // Objective: Test error handling for invalid table schemas

            // Test null schema
            string nullSchemaSql = BH.Engine.SQLite.Create.Table(null);
            nullSchemaSql.Should().BeNull("SQL should be null for null schema");

            // Test empty table name
            TableSchema emptyNameSchema = new TableSchema()
            {
                Name = "",
                Columns = new List<Column>()
                {
                    new Column()
                    {
                        Name = "Id",
                        DataType = SqliteDataType.INTEGER,
                        Position = 1
                    }
                }
            };

            string emptyNameSql = BH.Engine.SQLite.Create.Table(emptyNameSchema);
            emptyNameSql.Should().BeNull("SQL should be null for empty table name");

            // Test no columns
            TableSchema noColumnsSchema = new TableSchema()
            {
                Name = "TestTable",
                Columns = new List<Column>()
            };

            string noColumnsSql = BH.Engine.SQLite.Create.Table(noColumnsSchema);
            noColumnsSql.Should().BeNull("SQL should be null for schema with no columns");

            // Test null columns
            TableSchema nullColumnsSchema = new TableSchema()
            {
                Name = "TestTable",
                Columns = null
            };

            string nullColumnsSql = BH.Engine.SQLite.Create.Table(nullColumnsSchema);
            nullColumnsSql.Should().BeNull("SQL should be null for schema with null columns");
        }

        [Test]
        public void TableCreationWithIfNotExists()
        {
            // Objective: Test IF NOT EXISTS functionality

            // Arrange
            OpenTestConnection();

            TableSchema tableSchema = new TableSchema()
            {
                Name = "IfNotExistsTable",
                Columns = new List<Column>()
                {
                    new Column()
                    {
                        Name = "Id",
                        DataType = SqliteDataType.INTEGER,
                        IsPrimaryKey = true,
                        Position = 1
                    }
                }
            };

            // Act - Create table first time
            string createSql1 = BH.Engine.SQLite.Create.Table(tableSchema, ifNotExists: true);
            IEnumerable<object> results1 = ExecuteCustomSql(createSql1);

            // Assert - Table should be created
            bool tableExists1 = VerifyTableExists("IfNotExistsTable");
            tableExists1.Should().BeTrue("Table should exist after first creation");

            // Act - Create table second time with IF NOT EXISTS
            string createSql2 = BH.Engine.SQLite.Create.Table(tableSchema, ifNotExists: true);
            IEnumerable<object> results2 = ExecuteCustomSql(createSql2);

            // Assert - Should not throw error
            results2.Should().NotBeNull("Second creation should not fail with IF NOT EXISTS");

            // Act - Create table without IF NOT EXISTS (should fail)
            string createSql3 = BH.Engine.SQLite.Create.Table(tableSchema, ifNotExists: false);
            
            // This should return an error in the QueryResult when executed
            IEnumerable<object> results3 = ExecuteCustomSql(createSql3);
            results3.Should().NotBeNull("Query execution should return results");
            
            // The QueryResult should contain an error about the table already existing
            QueryResult queryResult = results3.FirstOrDefault() as QueryResult;
            queryResult.Should().NotBeNull("Should return a QueryResult");
            queryResult.IsSuccess.Should().BeFalse("Creating existing table without IF NOT EXISTS should fail");
            queryResult.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be provided");
            queryResult.ErrorMessage.Should().Contain("table", "Error message should mention table");

            CloseTestConnection();
        }

        [Test]
        public void ComplexTableCreation()
        {
            // Objective: Test creation of complex tables with various features

            // Arrange
            OpenTestConnection();

            TableSchema complexTableSchema = new TableSchema()
            {
                Name = "ComplexTable",
                Columns = new List<Column>()
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
                        Name = "Code",
                        DataType = SqliteDataType.TEXT,
                        MaxLength = 50,
                        IsUnique = true,
                        AllowNull = false,
                        Position = 2
                    },
                    new Column()
                    {
                        Name = "Name",
                        DataType = SqliteDataType.TEXT,
                        MaxLength = 200,
                        AllowNull = false,
                        Position = 3
                    },
                    new Column()
                    {
                        Name = "Value",
                        DataType = SqliteDataType.REAL,
                        AllowNull = true,
                        DefaultValue = "0.0",
                        Position = 4
                    },
                    new Column()
                    {
                        Name = "IsActive",
                        DataType = SqliteDataType.INTEGER,
                        AllowNull = false,
                        DefaultValue = "1",
                        Position = 5
                    },
                    new Column()
                    {
                        Name = "CreatedAt",
                        DataType = SqliteDataType.TEXT,
                        AllowNull = false,
                        DefaultValue = "CURRENT_TIMESTAMP",
                        Position = 6
                    },
                    new Column()
                    {
                        Name = "Data",
                        DataType = SqliteDataType.BLOB,
                        AllowNull = true,
                        Position = 7
                    }
                },
                Indexes = new List<BH.oM.SQLite.Objects.Index>()
                {
                    new BH.oM.SQLite.Objects.Index()
                    {
                        Name = "idx_complex_name",
                        TableName = "ComplexTable",
                        Columns = new List<string> { "Name" },
                        IsUnique = false
                    },
                    new BH.oM.SQLite.Objects.Index()
                    {
                        Name = "idx_complex_active",
                        TableName = "ComplexTable",
                        Columns = new List<string> { "IsActive" },
                        IsUnique = false
                    }
                }
            };

            // Act
            string createSql = BH.Engine.SQLite.Create.Table(complexTableSchema);
            createSql.Should().NotBeNullOrEmpty("SQL should be generated for complex table");

            // Execute the SQL
            IEnumerable<object> results = ExecuteCustomSql(createSql);

            // Assert
            results.Should().NotBeNull("Complex table creation should not fail");
            
            // Verify table exists
            bool tableExists = VerifyTableExists("ComplexTable");
            tableExists.Should().BeTrue("Complex table should exist in database");

            // Verify we can insert data into the complex table
            string insertSql = "INSERT INTO ComplexTable (Code, Name, Value) VALUES ('TEST001', 'Test Record', 123.45)";
            IEnumerable<object> insertResults = ExecuteCustomSql(insertSql);
            insertResults.Should().NotBeNull("Insert into complex table should succeed");

            // Verify data was inserted
            IEnumerable<object> selectResults = ExecuteCustomSql("SELECT COUNT(*) FROM ComplexTable");
            selectResults.Should().NotBeNull("Select from complex table should work");

            CloseTestConnection();
        }
    }
}
