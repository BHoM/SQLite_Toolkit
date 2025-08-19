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
using BH.oM.SQLite.Configs;
using BH.oM.SQLite;
using BH.oM.Adapter.Commands;
using BH.Tests.SQLite.Base;

namespace BH.Tests.SQLite.Examples
{
    /// <summary>
    /// Example-based tests showing delete functionality usage patterns.
    /// These tests serve as practical examples for developers learning to use the SQLite Toolkit delete operations.
    /// </summary>
    [TestFixture]
    public class DeleteUsageExampleTests : SQLiteTestBase
    {
        private SQLiteAdapter adapter;

        [SetUp]
        public void SetUp()
        {
            // Create a fresh adapter for each test using the same pattern as other tests
            adapter = CreateInMemoryTestAdapter();
            adapter.Execute(new Open() { FileName = ":memory:" });
        }

        [TearDown]
        public new void TearDown()
        {
            adapter?.Execute(new Close());
        }

        [Test]
        public void Example_DeleteWithEqualityFilter_StatusCode()
        {
            // Step 1: Create and push test data
            List<SensorReading> sensorReadings = CreateTestSensorData();
            adapter.Push(sensorReadings);

            // Step 2: Delete all invalid sensor readings (status code 404) using EqualityFilterRequest
            EqualityFilterRequest deleteRequest = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>()
                {
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 404 }
                    }
                }
            };

            int deletedCount = adapter.Remove(deleteRequest);

            // Verify deletion was successful
            deletedCount.Should().Be(1, "Should delete exactly 1 record with status code 404");

            // Step 3: Verify remaining data contains only valid readings
            EqualityFilterRequest getValidRequest = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>()
                {
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 200, 201 }
                    }
                }
            };

            IEnumerable<object> retrievedData = adapter.Pull(getValidRequest);
            QueryResult queryResult = retrievedData.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull("Query should return results");
            queryResult.Data.Should().HaveCount(4, "Should have 4 remaining valid records");
        }

        [Test]
        public void Example_DeleteWithRangeFilter_Temperature()
        {
            // Step 1: Create and push test data
            List<SensorReading> sensorReadings = CreateTestSensorData();
            adapter.Push(sensorReadings);

            // Step 2: Delete readings with temperature above 25°C using RangeFilterRequest
            RangeFilterRequest deleteRequest = new RangeFilterRequest()
            {
                TableName = "SensorReading",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>()
                {
                    new Dictionary<string, GeneralDomain>()
                    {
                        {
                            "Temperature",
                            new GeneralDomain()
                            {
                                Min = 25.0, // Temperature > 25
                                Max = null   // No upper limit
                            }
                        }
                    }
                },
                InclusiveBounds = false // Use > instead of >=
            };

            int deletedCount = adapter.Remove(deleteRequest);

            // Verify deletion was successful  
            deletedCount.Should().Be(2, "Should delete exactly 2 records with temperature > 25°C");

            // Step 3: Verify remaining data contains only readings with temperature <= 25°C
            RangeFilterRequest getRemainingRequest = new RangeFilterRequest()
            {
                TableName = "SensorReading",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>()
                {
                    new Dictionary<string, GeneralDomain>()
                    {
                        {
                            "Temperature",
                            new GeneralDomain()
                            {
                                Min = null,
                                Max = 25.0
                            }
                        }
                    }
                },
                InclusiveBounds = true
            };

            IEnumerable<object> retrievedData = adapter.Pull(getRemainingRequest);
            QueryResult queryResult = retrievedData.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull("Query should return results");
            queryResult.Data.Should().HaveCount(3, "Should have 3 remaining records with temperature <= 25°C");
        }

        [Test]
        public void Example_DeleteWithSimpleFilter_InvalidRecords()
        {
            // Step 1: Create and push test data
            List<SensorReading> sensorReadings = CreateTestSensorData();
            adapter.Push(sensorReadings);

            // Step 2: Delete invalid readings using EqualityFilterRequest
            EqualityFilterRequest deleteRequest = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>()
                {
                    new ColumnFilter()
                    {
                        ColumnName = "IsValid",
                        Values = new List<object> { false }
                    }
                }
            };

            DeleteConfig deleteConfig = new DeleteConfig();

            int deletedCount = adapter.Remove(deleteRequest, deleteConfig);

            // Verify deletion was successful
            deletedCount.Should().Be(1, "Should delete 1 invalid record");

            // Step 3: Verify remaining data
            EqualityFilterRequest getAllRequest = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>() // Empty means get all
            };

            IEnumerable<object> retrievedData = adapter.Pull(getAllRequest);
            QueryResult queryResult = retrievedData.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull("Query should return results");
            queryResult.Data.Should().HaveCount(4, "Should have 4 remaining valid records");
        }

        [Test]
        public void Example_DeleteDryRun_PreviewDeletion()
        {
            // Step 1: Create and push test data
            List<SensorReading> sensorReadings = CreateTestSensorData();
            adapter.Push(sensorReadings);

            // Step 2: Perform a dry run to see how many records would be deleted
            EqualityFilterRequest deleteRequest = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>()
                {
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 200 }
                    }
                }
            };

            DeleteConfig deleteConfig = new DeleteConfig()
            {
                DryRun = true // Don't actually delete
            };

            int wouldDeleteCount = adapter.Remove(deleteRequest, deleteConfig);

            // Verify dry run result
            wouldDeleteCount.Should().Be(3, "Dry run should indicate 3 records would be deleted");

            // Step 3: Verify no actual deletion occurred
            EqualityFilterRequest getAllRequest = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>() // Empty means get all
            };

            IEnumerable<object> retrievedData = adapter.Pull(getAllRequest);
            QueryResult queryResult = retrievedData.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull("Query should return results");
            queryResult.Data.Should().HaveCount(5, "All 5 original records should still exist after dry run");
        }

        [Test]
        public void Example_DeleteWithRowLimit_SafetyCheck()
        {
            // Step 1: Create and push test data
            List<SensorReading> sensorReadings = CreateTestSensorData();
            adapter.Push(sensorReadings);

            // Step 2: Try to delete all valid readings but limit to maximum 2 rows
            EqualityFilterRequest deleteRequest = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>()
                {
                    new ColumnFilter()
                    {
                        ColumnName = "IsValid",
                        Values = new List<object> { true }
                    }
                }
            };

            DeleteConfig deleteConfig = new DeleteConfig()
            {
                MaxRowsToDelete = 2 // Safety limit
            };

            // This should fail because 4 records match but limit is 2
            int deletedCount = adapter.Remove(deleteRequest, deleteConfig);

            // Verify deletion was prevented by safety limit
            deletedCount.Should().Be(0, "No records should be deleted due to safety limit");

            // Step 3: Verify all data is still present
            EqualityFilterRequest getAllRequest = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>() // Empty means get all
            };

            IEnumerable<object> retrievedData = adapter.Pull(getAllRequest);
            QueryResult queryResult = retrievedData.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull("Query should return results");
            queryResult.Data.Should().HaveCount(5, "All 5 original records should still exist");
        }

        /// <summary>
        /// Creates test sensor data for deletion examples - reusing pattern from BasicUsageExampleTests
        /// </summary>
        private List<SensorReading> CreateTestSensorData()
        {
            return new List<SensorReading>
            {
                new SensorReading()
                {
                    SensorId = "TEMP001",
                    Temperature = 20.0,
                    Humidity = 60.0,
                    Timestamp = new DateTime(2024, 1, 15, 14, 30, 0),
                    IsValid = true,
                    StatusCode = 200
                },
                new SensorReading()
                {
                    SensorId = "TEMP002",
                    Temperature = 25.0,
                    Humidity = 65.0,
                    Timestamp = new DateTime(2024, 1, 15, 14, 35, 0),
                    IsValid = true,
                    StatusCode = 200
                },
                new SensorReading()
                {
                    SensorId = "TEMP003",
                    Temperature = 22.0,
                    Humidity = 58.0,
                    Timestamp = new DateTime(2024, 1, 15, 14, 40, 0),
                    IsValid = true,
                    StatusCode = 200
                },
                new SensorReading()
                {
                    SensorId = "TEMP004",
                    Temperature = 30.0,
                    Humidity = 75.0,
                    Timestamp = new DateTime(2024, 1, 15, 14, 45, 0),
                    IsValid = true,
                    StatusCode = 201
                },
                new SensorReading()
                {
                    SensorId = "TEMP005",
                    Temperature = 18.0,
                    Humidity = 45.0,
                    Timestamp = new DateTime(2024, 1, 15, 14, 50, 0),
                    IsValid = false,
                    StatusCode = 404
                }
            };
        }
    }
}
