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
using FluentAssertions;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite;

namespace BH.Tests.SQLite.Unit
{
    /// <summary>
    /// Unit tests for filter processing and SQL query generation
    /// </summary>
    [TestFixture]
    public class FilterProcessingTests
    {
        [Test]
        public void Test_EqualityFilter_SingleColumn_SingleValue()
        {
            // Test basic equality filter with single column and single value
            
            // Arrange
            EqualityFilterRequest request = new EqualityFilterRequest()
            {
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "SensorId",
                        Values = new List<object> { "TEMP001" }
                    }
                },
                Logic = LogicalOperator.And
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.EqualityFilter(request);
            
            // Assert
            result.Should().NotBeNull("Filter should generate result");
            result.WhereClause.Should().NotBeNullOrEmpty("Should generate SQL condition");
            result.Parameters.Should().NotBeEmpty("Should generate parameters");
            
            // Verify SQL structure
            result.WhereClause.Should().Contain("\"SensorId\"", "Should include quoted column name");
            result.WhereClause.Should().Contain("=", "Should use equality operator for single value");
            result.WhereClause.Should().Contain("@", "Should use parameterized query");
            
            // Verify parameters
            result.Parameters.Should().HaveCount(1, "Should have one parameter for single value");
            result.Parameters.Values.Should().Contain("TEMP001", "Should contain the filter value");
        }

        [Test]
        public void Test_EqualityFilter_SingleColumn_MultipleValues()
        {
            // Test equality filter with IN clause for multiple values
            
            // Arrange
            EqualityFilterRequest request = new EqualityFilterRequest()
            {
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 200, 201, 202 }
                    }
                }
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.EqualityFilter(request);
            
            // Assert
            result.Should().NotBeNull("Filter should generate result");
            result.WhereClause.Should().Contain("\"StatusCode\"", "Should include quoted column name");
            result.WhereClause.Should().Contain("IN", "Should use IN clause for multiple values");
            result.WhereClause.Should().Contain("(", "Should have parentheses for IN clause");
            
            // Verify all values are parameterized
            result.Parameters.Should().HaveCount(3, "Should have parameters for all values");
            result.Parameters.Values.Should().Contain(200);
            result.Parameters.Values.Should().Contain(201);
            result.Parameters.Values.Should().Contain(202);
        }

        [Test]
        public void Test_EqualityFilter_MultipleColumns_AndLogic()
        {
            // Test equality filter with multiple columns combined with AND logic
            
            // Arrange
            EqualityFilterRequest request = new EqualityFilterRequest()
            {
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "SensorId",
                        Values = new List<object> { "TEMP001" }
                    },
                    new ColumnFilter()
                    {
                        ColumnName = "IsValid",
                        Values = new List<object> { true }
                    }
                },
                Logic = LogicalOperator.And
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.EqualityFilter(request);
            
            // Assert
            result.Should().NotBeNull("Filter should generate result");
            result.WhereClause.Should().Contain("\"SensorId\"", "Should include first column");
            result.WhereClause.Should().Contain("\"IsValid\"", "Should include second column");
            result.WhereClause.Should().Contain("AND", "Should use AND logic");
            
            result.Parameters.Should().HaveCount(2, "Should have parameters for both columns");
        }

        [Test]
        public void Test_EqualityFilter_MultipleColumns_OrLogic()
        {
            // Test equality filter with multiple columns combined with OR logic
            
            // Arrange
            EqualityFilterRequest request = new EqualityFilterRequest()
            {
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 404 }
                    },
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 500 }
                    }
                },
                Logic = LogicalOperator.Or
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.EqualityFilter(request);
            
            // Assert
            result.Should().NotBeNull("Filter should generate result");
            result.WhereClause.Should().Contain("OR", "Should use OR logic");
            result.Parameters.Should().HaveCount(2, "Should have parameters for both conditions");
        }

        [Test]
        public void Test_RangeFilter_NumericRange()
        {
            // Test range filter with numeric values
            
            // Arrange
            RangeFilterRequest request = new RangeFilterRequest()
            {
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Temperature",
                            new GeneralDomain() { Min = 20.0, Max = 30.0 }
                        }
                    }
                },
                InclusiveBounds = true
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.RangeFilter(request);
            
            // Assert
            result.Should().NotBeNull("Range filter should generate result");
            result.WhereClause.Should().Contain("\"Temperature\"", "Should include column name");
            result.WhereClause.Should().Contain(">=", "Should include minimum bound condition");
            result.WhereClause.Should().Contain("<=", "Should include maximum bound condition");
            result.WhereClause.Should().Contain("AND", "Should combine min and max conditions with AND");
            
            result.Parameters.Should().HaveCount(2, "Should have parameters for min and max values");
            result.Parameters.Values.Should().Contain(20.0, "Should include minimum value");
            result.Parameters.Values.Should().Contain(30.0, "Should include maximum value");
        }

        [Test]
        public void Test_RangeFilter_ExclusiveBounds()
        {
            // Test range filter with exclusive bounds
            
            // Arrange
            RangeFilterRequest request = new RangeFilterRequest()
            {
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Humidity",
                            new GeneralDomain() { Min = 50.0, Max = 80.0 }
                        }
                    }
                },
                InclusiveBounds = false
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.RangeFilter(request);
            
            // Assert
            result.Should().NotBeNull("Range filter should generate result");
            result.WhereClause.Should().Contain(">", "Should use exclusive greater than");
            result.WhereClause.Should().Contain("<", "Should use exclusive less than");
            result.WhereClause.Should().NotContain(">=", "Should not use inclusive operators");
            result.WhereClause.Should().NotContain("<=", "Should not use inclusive operators");
        }

        [Test]
        public void Test_RangeFilter_DateTimeRange()
        {
            // Test range filter with DateTime values
            
            // Arrange
            DateTime startDate = new DateTime(2024, 1, 1);
            DateTime endDate = new DateTime(2024, 12, 31);
            
            RangeFilterRequest request = new RangeFilterRequest()
            {
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Timestamp",
                            new GeneralDomain() { Min = startDate, Max = endDate }
                        }
                    }
                }
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.RangeFilter(request);
            
            // Assert
            result.Should().NotBeNull("DateTime range filter should generate result");
            result.WhereClause.Should().Contain("\"Timestamp\"", "Should include column name");
            result.Parameters.Should().HaveCount(2, "Should have parameters for date range");
            result.Parameters.Values.Should().Contain(startDate, "Should include start date");
            result.Parameters.Values.Should().Contain(endDate, "Should include end date");
        }

        [Test]
        public void Test_RangeFilter_MultipleColumns()
        {
            // Test range filter with multiple columns
            
            // Arrange
            RangeFilterRequest request = new RangeFilterRequest()
            {
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Temperature",
                            new GeneralDomain() { Min = 20.0, Max = 30.0 }
                        },
                        {
                            "Humidity",
                            new GeneralDomain() { Min = 40.0, Max = 60.0 }
                        }
                    }
                },
                Logic = LogicalOperator.And
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.RangeFilter(request);
            
            // Assert
            result.Should().NotBeNull("Multi-column range filter should generate result");
            result.WhereClause.Should().Contain("\"Temperature\"", "Should include first column");
            result.WhereClause.Should().Contain("\"Humidity\"", "Should include second column");
            result.WhereClause.Should().Contain("AND", "Should combine ranges with AND logic");
            
            result.Parameters.Should().HaveCount(4, "Should have parameters for both ranges (min/max each)");
        }

        [Test]
        public void Test_CombineFilterResults_MultipleFilters()
        {
            // Test combining multiple filter results
            
            // Arrange
            FilterCommand equalityFilter = new FilterCommand()
            {
                WhereClause = "\"SensorId\" = @param1",
                Parameters = new Dictionary<string, object> { { "@param1", "TEMP001" } }
            };
            
            FilterCommand rangeFilter = new FilterCommand()
            {
                WhereClause = "\"Temperature\" >= @param2 AND \"Temperature\" <= @param3",
                Parameters = new Dictionary<string, object> 
                { 
                    { "@param2", 20.0 },
                    { "@param3", 30.0 }
                }
            };
            
            List<FilterCommand> filters = new List<FilterCommand> { equalityFilter, rangeFilter };
            
            // Act
            FilterCommand combinedResult = BH.Engine.SQLite.Compute.CombineFilterResults(filters, LogicalOperator.And);
            
            // Assert
            combinedResult.Should().NotBeNull("Combined filter should generate result");
            combinedResult.WhereClause.Should().Contain("\"SensorId\" = @param1", "Should include equality condition");
            combinedResult.WhereClause.Should().Contain("\"Temperature\"", "Should include range condition");
            combinedResult.WhereClause.Should().Contain("AND", "Should combine with AND logic");
            combinedResult.WhereClause.Should().Contain("(", "Should have parentheses for grouping");
            
            combinedResult.Parameters.Should().HaveCount(3, "Should have all parameters from both filters");
            combinedResult.Parameters.Should().ContainKey("@param1");
            combinedResult.Parameters.Should().ContainKey("@param2");
            combinedResult.Parameters.Should().ContainKey("@param3");
        }

        [Test]
        public void Test_SelectQuery_WithFilter()
        {
            // Test building SELECT query with filter
            
            // Arrange
            string tableName = "SensorReadings";
            FilterCommand filter = new FilterCommand()
            {
                WhereClause = "\"SensorId\" = @param1",
                Parameters = new Dictionary<string, object> { { "@param1", "TEMP001" } }
            };
            
            // Act
            string selectQuery = BH.Engine.SQLite.Compute.SelectQuery(tableName, filter);
            
            // Assert
            selectQuery.Should().NotBeNullOrEmpty("Should generate SELECT query");
            selectQuery.Should().StartWith("SELECT", "Should be a SELECT statement");
            selectQuery.Should().Contain($"\"{tableName}\"", "Should include quoted table name");
            selectQuery.Should().Contain("WHERE", "Should include WHERE clause");
            selectQuery.Should().Contain("\"SensorId\" = @param1", "Should include filter condition");
        }

        [Test]
        public void Test_DeleteQuery_WithFilter()
        {
            // Test building DELETE query with filter
            
            // Arrange
            string tableName = "SensorReadings";
            FilterCommand filter = new FilterCommand()
            {
                WhereClause = "\"IsValid\" = @param1",
                Parameters = new Dictionary<string, object> { { "@param1", false } }
            };
            
            // Act
            string deleteQuery = BH.Engine.SQLite.Compute.DeleteQuery(tableName, filter);
            
            // Assert
            deleteQuery.Should().NotBeNullOrEmpty("Should generate DELETE query");
            deleteQuery.Should().StartWith("DELETE", "Should be a DELETE statement");
            deleteQuery.Should().Contain($"\"{tableName}\"", "Should include quoted table name");
            deleteQuery.Should().Contain("WHERE", "Should include WHERE clause");
            deleteQuery.Should().Contain("\"IsValid\" = @param1", "Should include filter condition");
        }

        [Test]
        public void Test_CountQuery_WithFilter()
        {
            // Test building COUNT query with filter
            
            // Arrange
            string tableName = "SensorReadings";
            FilterCommand filter = new FilterCommand()
            {
                WhereClause = "\"Temperature\" > @param1",
                Parameters = new Dictionary<string, object> { { "@param1", 25.0 } }
            };
            
            // Act
            string countQuery = BH.Engine.SQLite.Compute.CountQuery(tableName, filter);
            
            // Assert
            countQuery.Should().NotBeNullOrEmpty("Should generate COUNT query");
            countQuery.Should().StartWith("SELECT COUNT(*)", "Should be a COUNT statement");
            countQuery.Should().Contain($"\"{tableName}\"", "Should include quoted table name");
            countQuery.Should().Contain("WHERE", "Should include WHERE clause");
            countQuery.Should().Contain("\"Temperature\" > @param1", "Should include filter condition");
        }

        [Test]
        public void Test_EqualityFilter_WithSingleSortColumn()
        {
            // Test equality filter with single column sorting

            // Arrange
            EqualityFilterRequest request = new EqualityFilterRequest()
            {
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "SensorId",
                        Values = new List<object> { "TEMP001", "TEMP002" }
                    }
                },
                SortColumns = new List<SortColumn>
                {
                    new SortColumn()
                    {
                        ColumnName = "Timestamp",
                        SortDirection = SortOrder.DESC
                    }
                },
                MaxResults = 10
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.EqualityFilter(request);
            
            // Assert
            result.Should().NotBeNull("Filter should generate result");
            result.OrderByClause.Should().NotBeNullOrEmpty("Should generate ORDER BY clause");
            result.OrderByClause.Should().Be("\"Timestamp\" DESC", "Should generate correct ORDER BY clause");
            result.Limit.Should().Be(10, "Should set limit from MaxResults");
            
            // Verify WHERE clause still works
            result.WhereClause.Should().Contain("\"SensorId\"", "Should include filter condition");
            result.WhereClause.Should().Contain("IN", "Should use IN clause for multiple values");
        }

        [Test]
        public void Test_EqualityFilter_WithMultipleSortColumns()
        {
            // Test equality filter with multiple column sorting and priorities
            
            // Arrange
            EqualityFilterRequest request = new EqualityFilterRequest()
            {
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "IsValid",
                        Values = new List<object> { true }
                    }
                },
                SortColumns = new List<SortColumn>
                {
                    new SortColumn()
                    {
                        ColumnName = "Priority",
                        SortDirection = SortOrder.DESC,
                        Priority = 0
                    },
                    new SortColumn()
                    {
                        ColumnName = "Name",
                        SortDirection = SortOrder.ASC,
                        Priority = 1
                    },
                    new SortColumn()
                    {
                        ColumnName = "Timestamp",
                        SortDirection = SortOrder.DESC,
                        Priority = 2
                    }
                }
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.EqualityFilter(request);
            
            // Assert
            result.Should().NotBeNull("Filter should generate result");
            result.OrderByClause.Should().NotBeNullOrEmpty("Should generate ORDER BY clause");
            result.OrderByClause.Should().Be("\"Priority\" DESC, \"Name\" ASC, \"Timestamp\" DESC", 
                "Should generate correct multi-column ORDER BY clause with priority ordering");
        }

        [Test]
        public void Test_RangeFilter_WithSingleSortColumn()
        {
            // Test range filter with single column sorting
            
            // Arrange
            RangeFilterRequest request = new RangeFilterRequest()
            {
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Temperature",
                            new GeneralDomain() { Min = 20.0, Max = 30.0 }
                        }
                    }
                },
                SortColumns = new List<SortColumn>
                {
                    new SortColumn()
                    {
                        ColumnName = "Temperature",
                        SortDirection = SortOrder.ASC
                    }
                },
                MaxResults = 25
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.RangeFilter(request);
            
            // Assert
            result.Should().NotBeNull("Filter should generate result");
            result.OrderByClause.Should().NotBeNullOrEmpty("Should generate ORDER BY clause");
            result.OrderByClause.Should().Be("\"Temperature\" ASC", "Should generate correct ORDER BY clause");
            result.Limit.Should().Be(25, "Should set limit from MaxResults");
            
            // Verify WHERE clause still works
            result.WhereClause.Should().Contain("\"Temperature\"", "Should include range condition");
            result.WhereClause.Should().Contain(">=", "Should include minimum bound");
            result.WhereClause.Should().Contain("<=", "Should include maximum bound");
        }

        [Test]
        public void Test_RangeFilter_WithMultipleSortColumns()
        {
            // Test range filter with multiple column sorting
            
            // Arrange
            RangeFilterRequest request = new RangeFilterRequest()
            {
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Temperature",
                            new GeneralDomain() { Min = 15.0, Max = 35.0 }
                        },
                        {
                            "Humidity",
                            new GeneralDomain() { Min = 30.0, Max = 70.0 }
                        }
                    }
                },
                SortColumns = new List<SortColumn>
                {
                    new SortColumn()
                    {
                        ColumnName = "Timestamp",
                        SortDirection = SortOrder.DESC,
                        Priority = 0
                    },
                    new SortColumn()
                    {
                        ColumnName = "SensorId",
                        SortDirection = SortOrder.ASC,
                        Priority = 1
                    }
                }
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.RangeFilter(request);
            
            // Assert
            result.Should().NotBeNull("Filter should generate result");
            result.OrderByClause.Should().NotBeNullOrEmpty("Should generate ORDER BY clause");
            result.OrderByClause.Should().Be("\"Timestamp\" DESC, \"SensorId\" ASC", 
                "Should generate correct multi-column ORDER BY clause");
        }

        [Test]
        public void Test_OrderByClause_EmptyList()
        {
            // Test ORDER BY clause generation with empty sort columns
            
            // Arrange
            List<SortColumn> sortColumns = new List<SortColumn>();
            
            // Act
            string result = BH.Adapter.SQLite.Convert.OrderByClause(sortColumns);
            
            // Assert
            result.Should().BeEmpty("Should return empty string for empty sort columns list");
        }

        [Test]
        public void Test_OrderByClause_NullList()
        {
            // Test ORDER BY clause generation with null sort columns
            
            // Arrange
            List<SortColumn> sortColumns = null;
            
            // Act
            string result = BH.Adapter.SQLite.Convert.OrderByClause(sortColumns);
            
            // Assert
            result.Should().BeEmpty("Should return empty string for null sort columns list");
        }

        [Test]
        public void Test_OrderByClause_InvalidColumnName()
        {
            // Test ORDER BY clause generation with invalid column name
            
            // Arrange
            List<SortColumn> sortColumns = new List<SortColumn>
            {
                new SortColumn()
                {
                    ColumnName = "ValidColumn",
                    SortDirection = SortOrder.ASC
                },
                new SortColumn()
                {
                    ColumnName = "Invalid Column; DROP TABLE", // SQL injection attempt
                    SortDirection = SortOrder.DESC
                },
                new SortColumn()
                {
                    ColumnName = "AnotherValidColumn",
                    SortDirection = SortOrder.ASC
                }
            };
            
            // Act
            string result = BH.Adapter.SQLite.Convert.OrderByClause(sortColumns);
            
            // Assert
            result.Should().NotBeNullOrEmpty("Should generate ORDER BY clause");
            result.Should().Contain("\"ValidColumn\" ASC", "Should include valid columns");
            result.Should().Contain("\"AnotherValidColumn\" ASC", "Should include valid columns");
            result.Should().NotContain("DROP TABLE", "Should exclude invalid column names");
            result.Should().Be("\"ValidColumn\" ASC, \"AnotherValidColumn\" ASC", 
                "Should only include valid columns");
        }

        [Test]
        public void Test_OrderByClause_PriorityOrdering()
        {
            // Test ORDER BY clause generation with priority-based ordering
            
            // Arrange - Add columns in non-priority order to verify priority sorting works
            List<SortColumn> sortColumns = new List<SortColumn>
            {
                new SortColumn()
                {
                    ColumnName = "ThirdPriority",
                    SortDirection = SortOrder.ASC,
                    Priority = 2
                },
                new SortColumn()
                {
                    ColumnName = "FirstPriority",
                    SortDirection = SortOrder.DESC,
                    Priority = 0
                },
                new SortColumn()
                {
                    ColumnName = "SecondPriority",
                    SortDirection = SortOrder.ASC,
                    Priority = 1
                }
            };
            
            // Act
            string result = BH.Adapter.SQLite.Convert.OrderByClause(sortColumns);
            
            // Assert
            result.Should().NotBeNullOrEmpty("Should generate ORDER BY clause");
            result.Should().Be("\"FirstPriority\" DESC, \"SecondPriority\" ASC, \"ThirdPriority\" ASC", 
                "Should order columns by priority regardless of input order");
        }

        [Test]
        public void Test_EqualityFilter_SortingWithNoFilter()
        {
            // Test equality filter with sorting but no actual filter conditions
            
            // Arrange
            EqualityFilterRequest request = new EqualityFilterRequest()
            {
                ColumnFilters = new List<ColumnFilter>(), // Empty filters
                SortColumns = new List<SortColumn>
                {
                    new SortColumn()
                    {
                        ColumnName = "CreatedDate",
                        SortDirection = SortOrder.DESC
                    }
                }
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.EqualityFilter(request);
            
            // Assert
            result.Should().NotBeNull("Filter should generate result");
            result.WhereClause.Should().BeEmpty("Should have empty WHERE clause when no filters");
            result.OrderByClause.Should().Be("\"CreatedDate\" DESC", "Should still generate ORDER BY clause");
            result.Parameters.Should().BeEmpty("Should have no parameters when no filters");
        }

        [Test]
        public void Test_RangeFilter_SortingWithNoFilter()
        {
            // Test range filter with sorting but no actual filter conditions
            
            // Arrange
            RangeFilterRequest request = new RangeFilterRequest()
            {
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>(), // Empty ranges
                SortColumns = new List<SortColumn>
                {
                    new SortColumn()
                    {
                        ColumnName = "Priority",
                        SortDirection = SortOrder.ASC
                    },
                    new SortColumn()
                    {
                        ColumnName = "Name",
                        SortDirection = SortOrder.DESC
                    }
                }
            };
            
            // Act
            FilterCommand result = BH.Adapter.SQLite.Convert.RangeFilter(request);
            
            // Assert
            result.Should().BeNull("Range filter should return null when no ranges provided");
        }
    }
}

