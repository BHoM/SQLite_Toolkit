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
using BH.Tests.SQLite.Base;

namespace BH.Tests.SQLite.Examples
{
    /// <summary>
    /// Comprehensive examples showing all filtering capabilities of the SQLite Toolkit.
    /// These examples demonstrate the full power of the filtering system including
    /// equality filters, range filters, custom SQL, and combinations.
    /// </summary>
    [TestFixture]
    public class FilteringExampleTests : SQLiteTestBase
    {
        private SQLiteAdapter adapter;

        [SetUp]
        public void SetUp()
        {
            adapter = CreateInMemoryTestAdapter();
            adapter.Execute(new Open() { FileName = ":memory:" });
            
            // Setup comprehensive test data
            SetupSensorData();
            SetupMaterialData();
        }

        [TearDown]
        public new void TearDown()
        {
            adapter?.Execute(new Close());
        }

        private void SetupSensorData()
        {
            List<SensorReading> sensorReadings = new List<SensorReading>
            {
                new SensorReading() 
                { 
                    SensorId = "TEMP001", Temperature = 15.5, Humidity = 45.0, 
                    Timestamp = new DateTime(2024, 1, 1, 8, 0, 0), IsValid = true, StatusCode = 200 
                },
                new SensorReading() 
                { 
                    SensorId = "TEMP002", Temperature = 22.0, Humidity = 55.0, 
                    Timestamp = new DateTime(2024, 1, 1, 12, 0, 0), IsValid = true, StatusCode = 200 
                },
                new SensorReading() 
                { 
                    SensorId = "TEMP003", Temperature = 28.5, Humidity = 65.0, 
                    Timestamp = new DateTime(2024, 1, 1, 16, 0, 0), IsValid = true, StatusCode = 200 
                },
                new SensorReading() 
                { 
                    SensorId = "TEMP004", Temperature = 19.0, Humidity = 48.0, 
                    Timestamp = new DateTime(2024, 1, 2, 8, 0, 0), IsValid = false, StatusCode = 404 
                },
                new SensorReading() 
                { 
                    SensorId = "TEMP005", Temperature = 25.0, Humidity = 60.0, 
                    Timestamp = new DateTime(2024, 1, 2, 12, 0, 0), IsValid = true, StatusCode = 201 
                },
                new SensorReading() 
                { 
                    SensorId = "HUM001", Temperature = 20.0, Humidity = 75.0, 
                    Timestamp = new DateTime(2024, 1, 3, 8, 0, 0), IsValid = true, StatusCode = 200 
                },
                new SensorReading() 
                { 
                    SensorId = "HUM002", Temperature = 24.0, Humidity = 80.0, 
                    Timestamp = new DateTime(2024, 1, 3, 12, 0, 0), IsValid = false, StatusCode = 500 
                }
            };

            adapter.Push(sensorReadings);
        }

        private void SetupMaterialData()
        {
            List<SimpleMaterial> materials = new List<SimpleMaterial>
            {
                new SimpleMaterial()
                {
                    MaterialName = "Steel S235", Density = 7850.0, YoungModulus = 200000000000.0,
                    Type = MaterialType.Steel, IsRecyclable = true, CostPerCubicMeter = 120.0m
                },
                new SimpleMaterial()
                {
                    MaterialName = "Steel S355", Density = 7850.0, YoungModulus = 210000000000.0,
                    Type = MaterialType.Steel, IsRecyclable = true, CostPerCubicMeter = 150.0m
                },
                new SimpleMaterial()
                {
                    MaterialName = "Concrete C25/30", Density = 2400.0, YoungModulus = 31000000000.0,
                    Type = MaterialType.Concrete, IsRecyclable = false, CostPerCubicMeter = 75.0m
                },
                new SimpleMaterial()
                {
                    MaterialName = "Concrete C30/37", Density = 2400.0, YoungModulus = 33000000000.0,
                    Type = MaterialType.Concrete, IsRecyclable = false, CostPerCubicMeter = 85.0m
                },
                new SimpleMaterial()
                {
                    MaterialName = "CLT Timber", Density = 500.0, YoungModulus = 12000000000.0,
                    Type = MaterialType.Wood, IsRecyclable = true, CostPerCubicMeter = 250.0m
                },
                new SimpleMaterial()
                {
                    MaterialName = "Aluminium 6061", Density = 2700.0, YoungModulus = 69000000000.0,
                    Type = MaterialType.Aluminium, IsRecyclable = true, CostPerCubicMeter = 400.0m
                }
            };

            adapter.Push(materials);
        }

        [Test]
        public void Example_EqualityFilters_SimpleAndComplex()
        {
            /*
             * EXAMPLE: Equality Filters - Simple and Complex Scenarios
             * 
             * This example demonstrates various equality filtering scenarios from
             * simple single-value filters to complex multi-column combinations.
             */

            // Scenario 1: Simple single value filter
            EqualityFilterRequest validSensorsOnly = new EqualityFilterRequest()
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

            IEnumerable<object> validResults = adapter.Pull(validSensorsOnly);
            QueryResult validQuery = validResults.FirstOrDefault() as QueryResult;
            validQuery.Data.Should().HaveCount(5, "Should find 5 valid sensor readings");

            // Scenario 2: Multiple values in same column (IN clause)
            EqualityFilterRequest successStatusCodes = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 200, 201 } // Creates SQL: WHERE StatusCode IN (200, 201)
                    }
                }
            };

            IEnumerable<object> successResults = adapter.Pull(successStatusCodes);
            QueryResult successQuery = successResults.FirstOrDefault() as QueryResult;
            successQuery.Data.Should().HaveCount(5, "Should find readings with status 200 or 201");

            // Scenario 3: Multiple columns with AND logic
            EqualityFilterRequest validSuccessReadings = new EqualityFilterRequest()
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
                        Values = new List<object> { 200, 201 }
                    }
                },
                Logic = LogicalOperator.And // Both conditions must be true
            };

            IEnumerable<object> validSuccessResults = adapter.Pull(validSuccessReadings);
            QueryResult validSuccessQuery = validSuccessResults.FirstOrDefault() as QueryResult;
            validSuccessQuery.Data.Should().HaveCount(5, "Should find valid readings with success status codes");

            // Scenario 4: Multiple columns with OR logic
            EqualityFilterRequest errorOrHumidity = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "StatusCode",
                        Values = new List<object> { 404, 500 }
                    },
                    new ColumnFilter()
                    {
                        ColumnName = "SensorId",
                        Values = new List<object> { "HUM001", "HUM002" }
                    }
                },
                Logic = LogicalOperator.Or // Either condition can be true
            };

            IEnumerable<object> errorOrHumResults = adapter.Pull(errorOrHumidity);
            QueryResult errorOrHumQuery = errorOrHumResults.FirstOrDefault() as QueryResult;
            errorOrHumQuery.Data.Should().HaveCount(3, "Should find error readings OR humidity sensors");

            // Scenario 5: Complex sensor ID pattern matching
            EqualityFilterRequest temperatureSensors = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "SensorId",
                        Values = new List<object> { "TEMP001", "TEMP002", "TEMP003", "TEMP004", "TEMP005" }
                    }
                }
            };

            IEnumerable<object> tempResults = adapter.Pull(temperatureSensors);
            QueryResult tempQuery = tempResults.FirstOrDefault() as QueryResult;
            tempQuery.Data.Should().HaveCount(5, "Should find all TEMP sensors");
        }

        [Test]
        public void Example_RangeFilters_NumericAndDateTime()
        {
            /*
             * EXAMPLE: Range Filters for Numeric and DateTime Data
             * 
             * This example shows how to filter data within specific ranges,
             * including both numeric values and date/time ranges.
             */

            // Scenario 1: Temperature range filtering
            RangeFilterRequest comfortableTemperature = new RangeFilterRequest()
            {
                TableName = "SensorReading",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Temperature",
                            new GeneralDomain() { Min = 20.0, Max = 25.0 }
                        }
                    }
                },
                InclusiveBounds = true
            };

            IEnumerable<object> comfortResults = adapter.Pull(comfortableTemperature);
            QueryResult comfortQuery = comfortResults.FirstOrDefault() as QueryResult;
            comfortQuery.Data.Should().HaveCount(4, "Should find readings with temperature 20-25°C");

            // Scenario 2: Humidity range with exclusive bounds
            RangeFilterRequest moderateHumidity = new RangeFilterRequest()
            {
                TableName = "SensorReading",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Humidity",
                            new GeneralDomain() { Min = 50.0, Max = 70.0 }
                        }
                    }
                },
                InclusiveBounds = false // Exclusive: 50 < Humidity < 70
            };

            IEnumerable<object> moderateResults = adapter.Pull(moderateHumidity);
            QueryResult moderateQuery = moderateResults.FirstOrDefault() as QueryResult;
            moderateQuery.Data.Should().HaveCount(3, "Should find readings with humidity between 50-70% (exclusive)");

            // Scenario 3: Date/time range filtering
            RangeFilterRequest firstDayReadings = new RangeFilterRequest()
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
                                Min = new DateTime(2024, 1, 1, 0, 0, 0),
                                Max = new DateTime(2024, 1, 1, 23, 59, 59)
                            }
                        }
                    }
                },
                InclusiveBounds = true
            };

            IEnumerable<object> firstDayResults = adapter.Pull(firstDayReadings);
            QueryResult firstDayQuery = firstDayResults.FirstOrDefault() as QueryResult;
            firstDayQuery.Data.Should().HaveCount(3, "Should find all readings from January 1st");

            // Scenario 4: Multiple range conditions with AND logic
            RangeFilterRequest optimalConditions = new RangeFilterRequest()
            {
                TableName = "SensorReading",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "Temperature",
                            new GeneralDomain() { Min = 20.0, Max = 25.0 }
                        },
                        {
                            "Humidity",
                            new GeneralDomain() { Min = 50.0, Max = 65.0 }
                        }
                    }
                },
                Logic = LogicalOperator.And,
                InclusiveBounds = true
            };

            IEnumerable<object> optimalResults = adapter.Pull(optimalConditions);
            QueryResult optimalQuery = optimalResults.FirstOrDefault() as QueryResult;
            optimalQuery.Data.Should().HaveCount(2, "Should find readings with optimal temperature AND humidity");

            // Scenario 5: Material property ranges
            RangeFilterRequest highStrengthMaterials = new RangeFilterRequest()
            {
                TableName = "SimpleMaterial",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "YoungModulus",
                            new GeneralDomain() { Min = 100000000000.0, Max = 300000000000.0 } // 100-300 GPa
                        }
                    }
                },
                InclusiveBounds = true
            };

            IEnumerable<object> highStrengthResults = adapter.Pull(highStrengthMaterials);
            QueryResult highStrengthQuery = highStrengthResults.FirstOrDefault() as QueryResult;
            highStrengthQuery.Data.Should().HaveCount(2, "Should find high-strength materials (steels only, aluminium is 69 GPa < 100 GPa minimum)");
        }

        [Test]
        public void Example_CombinedFilters_EqualityAndRange()
        {
            /*
             * EXAMPLE: Combining Different Filter Types
             * 
             * This example shows how to combine equality and range filters
             * for sophisticated data queries using custom SQL requests.
             */

            // Scenario 1: Valid readings within temperature range
            // Since we can't directly combine different filter types in one request,
            // we use CustomSqlRequest for complex combinations
            CustomSqlRequest validTemperatureRange = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT * FROM SensorReading 
                    WHERE IsValid = @isValid 
                    AND Temperature BETWEEN @minTemp AND @maxTemp 
                    ORDER BY Timestamp",
                Parameters = new Dictionary<string, object>
                {
                    { "@isValid", true },
                    { "@minTemp", 20.0 },
                    { "@maxTemp", 28.0 }
                },
                IsReadOnly = true
            };

            IEnumerable<object> validTempResults = adapter.Pull(validTemperatureRange);
            QueryResult validTempQuery = validTempResults.FirstOrDefault() as QueryResult;
            validTempQuery.Data.Should().HaveCount(3, "Should find valid readings in temperature range");

            // Scenario 2: Specific sensors with high humidity
            CustomSqlRequest specificSensorsHighHumidity = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT * FROM SensorReading 
                    WHERE SensorId IN (@sensor1, @sensor2, @sensor3) 
                    AND Humidity > @humidityThreshold
                    ORDER BY Humidity DESC",
                Parameters = new Dictionary<string, object>
                {
                    { "@sensor1", "TEMP003" },
                    { "@sensor2", "HUM001" },
                    { "@sensor3", "HUM002" },
                    { "@humidityThreshold", 70.0 }
                },
                IsReadOnly = true
            };

            IEnumerable<object> highHumidityResults = adapter.Pull(specificSensorsHighHumidity);
            QueryResult highHumidityQuery = highHumidityResults.FirstOrDefault() as QueryResult;
            highHumidityQuery.Data.Should().HaveCount(2, "Should find specific sensors with high humidity");

            // Scenario 3: Material analysis - cost-effective sustainable materials
            CustomSqlRequest sustainableCostEffective = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT MaterialName, Type, Density, CostPerCubicMeter, IsRecyclable
                    FROM SimpleMaterial 
                    WHERE IsRecyclable = @recyclable 
                    AND CostPerCubicMeter BETWEEN @minCost AND @maxCost
                    ORDER BY CostPerCubicMeter ASC",
                Parameters = new Dictionary<string, object>
                {
                    { "@recyclable", true },
                    { "@minCost", 100.0 },
                    { "@maxCost", 300.0 }
                },
                IsReadOnly = true
            };

            IEnumerable<object> sustainableResults = adapter.Pull(sustainableCostEffective);
            QueryResult sustainableQuery = sustainableResults.FirstOrDefault() as QueryResult;
            sustainableQuery.Data.Should().HaveCount(3, "Should find cost-effective recyclable materials");

            // Scenario 4: Time-based analysis with aggregation
            CustomSqlRequest dailyAverages = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT 
                        DATE(Timestamp) as ReadingDate,
                        AVG(Temperature) as AvgTemperature,
                        AVG(Humidity) as AvgHumidity,
                        COUNT(*) as ReadingCount
                    FROM SensorReading 
                    WHERE IsValid = @isValid
                    GROUP BY DATE(Timestamp)
                    ORDER BY ReadingDate",
                Parameters = new Dictionary<string, object>
                {
                    { "@isValid", true }
                },
                IsReadOnly = true
            };

            IEnumerable<object> dailyResults = adapter.Pull(dailyAverages);
            QueryResult dailyQuery = dailyResults.FirstOrDefault() as QueryResult;
            dailyQuery.Data.Should().HaveCount(3, "Should have data for 3 days");

            // Verify aggregated data
            Dictionary<string, object> firstDay = dailyQuery.Data[0];
            firstDay["ReadingDate"].Should().Be("2024-01-01");
            firstDay["ReadingCount"].Should().Be(3L); // 3 valid readings on first day
        }

        [Test]
        public void Example_MaterialFiltering_EnumAndProperties()
        {
            /*
             * EXAMPLE: Advanced Material Filtering with Enums and Properties
             * 
             * This example demonstrates filtering materials by type (enum),
             * combining with property ranges and boolean conditions.
             */

            // Scenario 1: Filter by material type (enum)
            EqualityFilterRequest metalMaterials = new EqualityFilterRequest()
            {
                TableName = "SimpleMaterial",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Type",
                        Values = new List<object> { MaterialType.Steel, MaterialType.Aluminium }
                    }
                }
            };

            IEnumerable<object> metalResults = adapter.Pull(metalMaterials);
            QueryResult metalQuery = metalResults.FirstOrDefault() as QueryResult;
            metalQuery.Data.Should().HaveCount(3, "Should find all metal materials");

            // Scenario 2: High-performance materials (high Young's modulus)
            RangeFilterRequest highPerformance = new RangeFilterRequest()
            {
                TableName = "SimpleMaterial",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "YoungModulus",
                            new GeneralDomain() { Min = 150000000000.0, Max = double.MaxValue } // > 150 GPa
                        }
                    }
                }
            };

            IEnumerable<object> highPerfResults = adapter.Pull(highPerformance);
            QueryResult highPerfQuery = highPerfResults.FirstOrDefault() as QueryResult;
            highPerfQuery.Data.Should().HaveCount(2, "Should find high-performance materials (high-grade steels)");

            // Scenario 3: Sustainable materials within budget
            CustomSqlRequest sustainableBudget = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT * FROM SimpleMaterial 
                    WHERE IsRecyclable = @recyclable 
                    AND CostPerCubicMeter <= @maxCost
                    ORDER BY CostPerCubicMeter ASC",
                Parameters = new Dictionary<string, object>
                {
                    { "@recyclable", true },
                    { "@maxCost", 200.0m }
                },
                IsReadOnly = true
            };

            IEnumerable<object> budgetResults = adapter.Pull(sustainableBudget);
            QueryResult budgetQuery = budgetResults.FirstOrDefault() as QueryResult;
            budgetQuery.Data.Should().HaveCount(2, "Should find affordable sustainable materials");

            // Scenario 4: Material comparison analysis
            CustomSqlRequest materialComparison = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT 
                        Type,
                        COUNT(*) as MaterialCount,
                        AVG(Density) as AvgDensity,
                        AVG(CAST(YoungModulus as REAL)) as AvgYoungModulus,
                        AVG(CAST(CostPerCubicMeter as REAL)) as AvgCost
                    FROM SimpleMaterial 
                    GROUP BY Type
                    ORDER BY AvgYoungModulus DESC",
                Parameters = new Dictionary<string, object>(),
                IsReadOnly = true
            };

            IEnumerable<object> comparisonResults = adapter.Pull(materialComparison);
            QueryResult comparisonQuery = comparisonResults.FirstOrDefault() as QueryResult;
            comparisonQuery.Data.Should().HaveCount(4, "Should have analysis for each material type");

            // Verify steel has highest average Young's modulus
            Dictionary<string, object> topMaterial = comparisonQuery.Data[0];
            topMaterial["Type"].Should().Be(MaterialType.Steel);
        }

        [Test]
        public void Example_AdvancedFiltering_WithLimitsAndOrdering()
        {
            /*
             * EXAMPLE: Advanced Filtering with Result Limits and Ordering
             * 
             * This example shows how to use MaxResults property and custom SQL
             * for sophisticated data retrieval patterns.
             */

            // Scenario 1: Top 3 highest temperatures
            CustomSqlRequest topTemperatures = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT * FROM SensorReading 
                    WHERE IsValid = @isValid
                    ORDER BY Temperature DESC 
                    LIMIT @limit",
                Parameters = new Dictionary<string, object>
                {
                    { "@isValid", true },
                    { "@limit", 3 }
                },
                IsReadOnly = true
            };

            IEnumerable<object> topTempResults = adapter.Pull(topTemperatures);
            QueryResult topTempQuery = topTempResults.FirstOrDefault() as QueryResult;
            topTempQuery.Data.Should().HaveCount(3, "Should return top 3 temperatures");
            
            double firstTemp = System.Convert.ToDouble(topTempQuery.Data[0]["Temperature"]);
            double secondTemp = System.Convert.ToDouble(topTempQuery.Data[1]["Temperature"]);
            firstTemp.Should().BeGreaterThanOrEqualTo(secondTemp, "Results should be ordered by temperature DESC");

            // Scenario 2: Most recent readings with limit using EqualityFilterRequest
            EqualityFilterRequest recentValidReadings = new EqualityFilterRequest()
            {
                TableName = "SensorReading",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "IsValid",
                        Values = new List<object> { true }
                    }
                },
                MaxResults = 4 // Limit to 4 most recent
            };

            IEnumerable<object> recentResults = adapter.Pull(recentValidReadings);
            QueryResult recentQuery = recentResults.FirstOrDefault() as QueryResult;
            recentQuery.Data.Should().HaveCountLessOrEqualTo(4, "Should return at most 4 results");

            // Scenario 3: Most expensive materials
            CustomSqlRequest expensiveMaterials = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT MaterialName, Type, CostPerCubicMeter
                    FROM SimpleMaterial 
                    ORDER BY CostPerCubicMeter DESC 
                    LIMIT @limit",
                Parameters = new Dictionary<string, object>
                {
                    { "@limit", 2 }
                },
                IsReadOnly = true
            };

            IEnumerable<object> expensiveResults = adapter.Pull(expensiveMaterials);
            QueryResult expensiveQuery = expensiveResults.FirstOrDefault() as QueryResult;
            expensiveQuery.Data.Should().HaveCount(2, "Should return 2 most expensive materials");

            // Scenario 4: Sensor performance statistics
            CustomSqlRequest sensorStats = new CustomSqlRequest()
            {
                SqlQuery = @"
                    SELECT 
                        SUBSTR(SensorId, 1, 4) as SensorType,
                        COUNT(*) as TotalReadings,
                        SUM(CASE WHEN IsValid = 1 THEN 1 ELSE 0 END) as ValidReadings,
                        ROUND(AVG(Temperature), 2) as AvgTemperature,
                        ROUND(AVG(Humidity), 2) as AvgHumidity
                    FROM SensorReading 
                    GROUP BY SUBSTR(SensorId, 1, 4)
                    ORDER BY TotalReadings DESC",
                Parameters = new Dictionary<string, object>(),
                IsReadOnly = true
            };

            IEnumerable<object> statsResults = adapter.Pull(sensorStats);
            QueryResult statsQuery = statsResults.FirstOrDefault() as QueryResult;
            statsQuery.Data.Should().HaveCount(2, "Should have stats for TEMP and HUM sensor types");

            Dictionary<string, object> tempStats = statsQuery.Data[0];
            tempStats["SensorType"].Should().Be("TEMP");
            tempStats["TotalReadings"].Should().Be(5L);
        }
    }
}
