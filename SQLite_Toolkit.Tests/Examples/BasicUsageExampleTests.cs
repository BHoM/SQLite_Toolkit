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
using BH.oM.SQLite.Examples;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite;
using BH.oM.Adapter.Commands;
using SQLite_Toolkit.Tests.Base;

namespace SQLite_Toolkit.Tests.Examples
{
    /// <summary>
    /// Example-based tests showing basic usage patterns for simple IRecord objects.
    /// These tests serve as practical examples for developers learning to use the SQLite Toolkit.
    /// </summary>
    [TestFixture]
    public class BasicUsageExampleTests : SQLiteTestBase
    {
        private SQLiteAdapter adapter;

        [SetUp]
        public void SetUp()
        {
            // Create a fresh adapter for each test
            adapter = CreateInMemoryTestAdapter();
            adapter.Execute(new Open() { FileName = ":memory:" });
        }

        [TearDown]
        public new void TearDown()
        {
            adapter?.Execute(new Close());
        }

        [Test]
        public void Example_BasicPushPull_SensorData()
        {
            /*
             * EXAMPLE: Basic Push and Pull with IRecord Objects
             * 
             * This example shows the simplest way to store and retrieve data using IRecord objects.
             * IRecord objects contain only primitive data types and are automatically mapped to database tables.
             * No configuration is required - the toolkit handles everything automatically.
             */
            
            // Step 1: Create some sensor readings (IRecord objects)
            List<SensorReading> sensorReadings = new List<SensorReading>
            {
                new SensorReading()
                {
                    SensorId = "TEMP001",
                    Temperature = 23.5,
                    Humidity = 65.0,
                    Timestamp = new DateTime(2024, 1, 15, 14, 30, 0),
                    IsValid = true,
                    StatusCode = 200
                },
                new SensorReading()
                {
                    SensorId = "TEMP002",
                    Temperature = 21.8,
                    Humidity = 58.5,
                    Timestamp = new DateTime(2024, 1, 15, 14, 35, 0),
                    IsValid = true,
                    StatusCode = 200
                },
                new SensorReading()
                {
                    SensorId = "TEMP003",
                    Temperature = 25.2,
                    Humidity = 72.0,
                    Timestamp = new DateTime(2024, 1, 15, 14, 40, 0),
                    IsValid = false,
                    StatusCode = 404
                }
            };

            // Step 2: Push the data to the database
            // No configuration needed - the toolkit automatically:
            // - Detects that SensorReading implements IRecord
            // - Creates a table with all primitive properties as columns
            // - Maps all properties to appropriate database columns
            List<object> pushedObjects = adapter.Push(sensorReadings);

            // Verify push was successful
            pushedObjects.Should().HaveCount(3, "All sensor readings should be pushed successfully");

            // Step 3: Pull all data back from the database
            // Use a simple CustomSqlRequest to get all records
            CustomSqlRequest getAllRequest = new CustomSqlRequest()
            {
                SqlQuery = "SELECT * FROM SensorReading ORDER BY Timestamp",
                Parameters = new Dictionary<string, object>(),
                IsReadOnly = true
            };

            IEnumerable<object> retrievedData = adapter.Pull(getAllRequest);
            QueryResult queryResult = retrievedData.FirstOrDefault() as QueryResult;

            // Verify the data was stored and retrieved correctly
            queryResult.Should().NotBeNull("Query should return results");
            queryResult.IsSuccess.Should().BeTrue("Query should succeed");
            queryResult.Data.Should().HaveCount(3, "Should retrieve all 3 sensor readings");

            // Verify data integrity
            Dictionary<string, object> firstRecord = queryResult.Data[0];
            firstRecord["SensorId"].Should().Be("TEMP001");
            firstRecord["Temperature"].Should().Be(23.5);
            firstRecord["Humidity"].Should().Be(65.0);
            firstRecord["IsValid"].Should().Be(true, "Boolean values should be automatically converted from SQLite storage");
            firstRecord["StatusCode"].Should().Be(200, "Integer values should be automatically converted from SQLite storage");
        }

        [Test]
        public void Example_FilteredQueries_EqualityFilter()
        {
            /*
             * EXAMPLE: Filtered Queries with EqualityFilterRequest
             * 
             * This example shows how to retrieve specific data using the EqualityFilterRequest.
             * This is much simpler than writing SQL queries manually.
             */

            // Step 1: Setup test data
            List<SensorReading> sensorReadings = new List<SensorReading>
            {
                new SensorReading() { SensorId = "TEMP001", Temperature = 20.0, IsValid = true, StatusCode = 200 },
                new SensorReading() { SensorId = "TEMP002", Temperature = 25.0, IsValid = true, StatusCode = 200 },
                new SensorReading() { SensorId = "TEMP003", Temperature = 30.0, IsValid = false, StatusCode = 404 },
                new SensorReading() { SensorId = "TEMP004", Temperature = 22.0, IsValid = true, StatusCode = 201 }
            };

            adapter.Push(sensorReadings);

            // Step 2: Filter by single value - get only valid readings
            EqualityFilterRequest validOnlyFilter = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "IsValid",
                        Values = new List<object> { true }
                    }
                }
            };

            IEnumerable<object> validResults = adapter.Pull(validOnlyFilter);
            QueryResult validQueryResult = validResults.FirstOrDefault() as QueryResult;

            validQueryResult.Should().NotBeNull();
            validQueryResult.Data.Should().HaveCount(3, "Should find 3 valid sensor readings");

            // Step 3: Filter by multiple values - get readings with specific status codes
            EqualityFilterRequest statusCodesFilter = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 200, 201 } // Multiple values create an IN clause
                    }
                }
            };

            IEnumerable<object> statusResults = adapter.Pull(statusCodesFilter);
            QueryResult statusQueryResult = statusResults.FirstOrDefault() as QueryResult;

            statusQueryResult.Should().NotBeNull();
            statusQueryResult.Data.Should().HaveCount(3, "Should find all readings with status 200 or 201");

            // Step 4: Combine multiple filters with AND logic
            EqualityFilterRequest combinedFilter = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "IsValid",
                        Values = new List<object> { true }
                    },
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 200 }
                    }
                },
                Logic = LogicalOperator.And
            };

            IEnumerable<object> combinedResults = adapter.Pull(combinedFilter);
            QueryResult combinedQueryResult = combinedResults.FirstOrDefault() as QueryResult;

            combinedQueryResult.Should().NotBeNull();
            combinedQueryResult.Data.Should().HaveCount(2, "Should find readings that are both valid AND have status 200");
        }

        [Test]
        public void Example_RangeQueries_TemperatureRange()
        {
            /*
             * EXAMPLE: Range Queries with RangeFilterRequest
             * 
             * This example shows how to query for data within specific numeric or date ranges.
             */

            // Step 1: Setup test data with varied temperatures
            List<SensorReading> sensorReadings = new List<SensorReading>
            {
                new SensorReading() { SensorId = "TEMP001", Temperature = 15.0, Timestamp = new DateTime(2024, 1, 1, 10, 0, 0) },
                new SensorReading() { SensorId = "TEMP002", Temperature = 20.0, Timestamp = new DateTime(2024, 1, 2, 10, 0, 0) },
                new SensorReading() { SensorId = "TEMP003", Temperature = 25.0, Timestamp = new DateTime(2024, 1, 3, 10, 0, 0) },
                new SensorReading() { SensorId = "TEMP004", Temperature = 30.0, Timestamp = new DateTime(2024, 1, 4, 10, 0, 0) },
                new SensorReading() { SensorId = "TEMP005", Temperature = 35.0, Timestamp = new DateTime(2024, 1, 5, 10, 0, 0) }
            };

            adapter.Push(sensorReadings);

            // Step 2: Query for temperatures in a specific range (20°C to 30°C inclusive)
            RangeFilterRequest temperatureRangeFilter = new RangeFilterRequest()
            {
                TableName = "SensorReading",
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

            IEnumerable<object> tempResults = adapter.Pull(temperatureRangeFilter);
            QueryResult tempQueryResult = tempResults.FirstOrDefault() as QueryResult;

            tempQueryResult.Should().NotBeNull();
            tempQueryResult.Data.Should().HaveCount(3, "Should find readings with temperature between 20-30°C inclusive");

            // Step 3: Query for readings within a date range
            RangeFilterRequest dateRangeFilter = new RangeFilterRequest()
            {
                TableName = "SensorReading",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Timestamp",
                            new GeneralDomain() 
                            { 
                                Min = new DateTime(2024, 1, 2, 0, 0, 0),
                                Max = new DateTime(2024, 1, 4, 23, 59, 59)
                            }
                        }
                    }
                },
                InclusiveBounds = true
            };

            IEnumerable<object> dateResults = adapter.Pull(dateRangeFilter);
            QueryResult dateQueryResult = dateResults.FirstOrDefault() as QueryResult;

            dateQueryResult.Should().NotBeNull();
            dateQueryResult.Data.Should().HaveCount(3, "Should find readings from Jan 2-4, 2024");

            // Step 4: Combine temperature and date ranges
            RangeFilterRequest combinedRangeFilter = new RangeFilterRequest()
            {
                TableName = "SensorReading",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Temperature",
                            new GeneralDomain() { Min = 22.0, Max = 32.0 }
                        },
                        {
                            "Timestamp",
                            new GeneralDomain() 
                            { 
                                Min = new DateTime(2024, 1, 3, 0, 0, 0),
                                Max = new DateTime(2024, 1, 5, 23, 59, 59)
                            }
                        }
                    }
                },
                Logic = LogicalOperator.And,
                InclusiveBounds = true
            };

            IEnumerable<object> combinedResults = adapter.Pull(combinedRangeFilter);
            QueryResult combinedQueryResult = combinedResults.FirstOrDefault() as QueryResult;

            combinedQueryResult.Should().NotBeNull();
            combinedQueryResult.Data.Should().HaveCount(2, "Should find readings matching both temperature and date criteria");
        }

        [Test]
        public void Example_MaterialDatabase_EnumHandling()
        {
            /*
             * EXAMPLE: Working with Enums in IRecord Objects
             * 
             * This example shows how enums are automatically handled in IRecord objects.
             * Enums are stored as integers in the database but can be queried by their enum values.
             */

            // Step 1: Create materials with different types
            List<SimpleMaterial> materials = new List<SimpleMaterial>
            {
                new SimpleMaterial()
                {
                    MaterialName = "Structural Steel S355",
                    Density = 7850.0,
                    YoungModulus = 210000000000.0, // 210 GPa
                    PoissonRatio = 0.3,
                    Type = MaterialType.Steel,
                    IsRecyclable = true,
                    CostPerCubicMeter = 150.0m
                },
                new SimpleMaterial()
                {
                    MaterialName = "Concrete C30/37",
                    Density = 2400.0,
                    YoungModulus = 33000000000.0, // 33 GPa
                    PoissonRatio = 0.2,
                    Type = MaterialType.Concrete,
                    IsRecyclable = false,
                    CostPerCubicMeter = 80.0m
                },
                new SimpleMaterial()
                {
                    MaterialName = "CLT Timber",
                    Density = 500.0,
                    YoungModulus = 12000000000.0, // 12 GPa
                    PoissonRatio = 0.35,
                    Type = MaterialType.Wood,
                    IsRecyclable = true,
                    CostPerCubicMeter = 200.0m
                }
            };

            adapter.Push(materials);

            // Step 2: Filter by material type (enum value)
            EqualityFilterRequest steelFilter = new EqualityFilterRequest()
            {
                TableName = "SimpleMaterial",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Type",
                        Values = new List<object> { MaterialType.Steel } // Can use enum directly
                    }
                }
            };

            IEnumerable<object> steelResults = adapter.Pull(steelFilter);
            QueryResult steelQueryResult = steelResults.FirstOrDefault() as QueryResult;

            steelQueryResult.Should().NotBeNull();
            steelQueryResult.Data.Should().HaveCount(1, "Should find 1 steel material");
            steelQueryResult.Data[0]["MaterialName"].Should().Be("Structural Steel S355");

            // Step 3: Filter by multiple material types
            EqualityFilterRequest sustainableFilter = new EqualityFilterRequest()
            {
                TableName = "SimpleMaterial",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Type",
                        Values = new List<object> { MaterialType.Steel, MaterialType.Wood }
                    }
                }
            };

            IEnumerable<object> sustainableResults = adapter.Pull(sustainableFilter);
            QueryResult sustainableQueryResult = sustainableResults.FirstOrDefault() as QueryResult;

            sustainableQueryResult.Should().NotBeNull();
            sustainableQueryResult.Data.Should().HaveCount(2, "Should find steel and wood materials");

            // Step 4: Combine enum filter with other criteria
            EqualityFilterRequest recyclableMetalsFilter = new EqualityFilterRequest()
            {
                TableName = "SimpleMaterial",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Type",
                        Values = new List<object> { MaterialType.Steel, MaterialType.Aluminium }
                    },
                    new ColumnFilter()
                    {
                        ColumnName = "IsRecyclable",
                        Values = new List<object> { true }
                    }
                },
                Logic = LogicalOperator.And
            };

            IEnumerable<object> recyclableResults = adapter.Pull(recyclableMetalsFilter);
            QueryResult recyclableQueryResult = recyclableResults.FirstOrDefault() as QueryResult;

            recyclableQueryResult.Should().NotBeNull();
            recyclableQueryResult.Data.Should().HaveCount(1, "Should find recyclable metal materials");
        }

        [Test]
        public void Example_DataIntegrityAndGuidHandling()
        {
            /*
             * EXAMPLE: Data Integrity and GUID Handling
             * 
             * This example shows how BHoM_Guid is automatically handled and how to maintain data integrity.
             */

            // Step 1: Create sensor readings with specific GUIDs
            Guid sensor1Guid = Guid.NewGuid();
            Guid sensor2Guid = Guid.NewGuid();

            List<SensorReading> readings = new List<SensorReading>
            {
                new SensorReading()
                {
                    BHoM_Guid = sensor1Guid,
                    SensorId = "TEMP001",
                    Temperature = 23.5,
                    IsValid = true
                },
                new SensorReading()
                {
                    BHoM_Guid = sensor2Guid,
                    SensorId = "TEMP002", 
                    Temperature = 24.8,
                    IsValid = true
                }
            };

            adapter.Push(readings);

            // Step 2: Query by specific GUID
            EqualityFilterRequest guidFilter = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "BHoM_Guid",
                        Values = new List<object> { sensor1Guid }
                    }
                }
            };

            IEnumerable<object> guidResults = adapter.Pull(guidFilter);
            QueryResult guidQueryResult = guidResults.FirstOrDefault() as QueryResult;

            guidQueryResult.Should().NotBeNull();
            guidQueryResult.Data.Should().HaveCount(1, "Should find exactly one record with specific GUID");
            guidQueryResult.Data[0]["SensorId"].Should().Be("TEMP001");

            // Step 3: Update existing record (INSERT OR REPLACE behavior)
            SensorReading updatedReading = new SensorReading()
            {
                BHoM_Guid = sensor1Guid, // Same GUID will update existing record
                SensorId = "TEMP001",
                Temperature = 25.0, // Updated temperature
                IsValid = true
            };

            adapter.Push(new List<SensorReading> { updatedReading });

            // Step 4: Verify update worked
            IEnumerable<object> updatedResults = adapter.Pull(guidFilter);
            QueryResult updatedQueryResult = updatedResults.FirstOrDefault() as QueryResult;

            updatedQueryResult.Should().NotBeNull();
            updatedQueryResult.Data.Should().HaveCount(1, "Should still have exactly one record");
            updatedQueryResult.Data[0]["Temperature"].Should().Be(25.0, "Temperature should be updated");

            // Step 5: Verify total count is still 2 (no duplicate created)
            CustomSqlRequest countAllRequest = new CustomSqlRequest()
            {
                SqlQuery = "SELECT COUNT(*) as RecordCount FROM SensorReading",
                IsReadOnly = true
            };

            IEnumerable<object> countResults = adapter.Pull(countAllRequest);
            QueryResult countQueryResult = countResults.FirstOrDefault() as QueryResult;

            countQueryResult.Should().NotBeNull();
            countQueryResult.Data[0]["RecordCount"].Should().Be(2L, "Should still have only 2 total records");
        }
    }
}
