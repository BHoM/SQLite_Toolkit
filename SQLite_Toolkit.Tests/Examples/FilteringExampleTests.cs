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
        public void Example_MaterialFiltering_EnumAndProperties()
        {
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

            // Scenario 3: Sustainable materials within budget using separate filters
            EqualityFilterRequest sustainableBudget = new EqualityFilterRequest()
            {
                TableName = "SimpleMaterial",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "IsRecyclable",
                        Values = new List<object> { true }
                    }
                }
            };

            IEnumerable<object> budgetResults = adapter.Pull(sustainableBudget);
            QueryResult budgetQuery = budgetResults.FirstOrDefault() as QueryResult;
            budgetQuery.Data.Should().HaveCount(4, "Should find sustainable materials");

            // Then filter by cost using RangeFilterRequest
            RangeFilterRequest affordableRange = new RangeFilterRequest()
            {
                TableName = "SimpleMaterial",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "CostPerCubicMeter",
                            new GeneralDomain() { Min = 0.0m, Max = 200.0m }
                        }
                    }
                },
                InclusiveBounds = true
            };

            IEnumerable<object> affordableResults = adapter.Pull(affordableRange);
            QueryResult affordableQuery = affordableResults.FirstOrDefault() as QueryResult;
            affordableQuery.Data.Should().HaveCount(4, "Should find affordable materials");

            // Note: Complex aggregation analysis (GROUP BY, AVG) is no longer supported 
            // with the current filter system. Such operations would require a different approach.
        }

        [Test]
        public void Example_AdvancedFiltering_WithLimitsOnly()
        {
            // Scenario 1: Limited results using EqualityFilterRequest
            EqualityFilterRequest validReadingsLimited = new EqualityFilterRequest()
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
                MaxResults = 3 // Limit to 3 results
            };

            IEnumerable<object> limitedResults = adapter.Pull(validReadingsLimited);
            QueryResult limitedQuery = limitedResults.FirstOrDefault() as QueryResult;
            limitedQuery.Data.Should().HaveCountLessOrEqualTo(3, "Should return at most 3 results");

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

            // Note: Ordering (ORDER BY), aggregations (GROUP BY, COUNT, AVG), 
            // and complex SQL functions (SUBSTR, ROUND) are no longer supported
            // with the current simplified filter system.
        }
    }
}
