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
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite;
using BH.oM.Adapter.Commands;

using SQLite_Toolkit.Tests.Base;

namespace SQLite_Toolkit.Tests.Examples
{
    /// <summary>
    /// Example-based tests showing advanced property mapping for complex BHoM objects.
    /// These examples demonstrate the second tier of the three-tier strategy using PushConfig.
    /// </summary>
    [TestFixture]
    public class ComplexMappingExampleTests : SQLiteTestBase
    {
        private SQLiteAdapter adapter;

        [SetUp]
        public void SetUp()
        {
            adapter = CreateInMemoryTestAdapter();
            adapter.Execute(new Open() { FileName = ":memory:" });
        }

        [TearDown]
        public new void TearDown()
        {
            adapter?.Execute(new Close());
        }

        [Test]
        public void Example_StructuralElement_BasicPropertyMapping()
        {
            /*
             * EXAMPLE: Basic Property Mapping for Complex Objects
             * 
             * This example shows how to map complex BHoM objects to database tables
             * using PushConfig with property mappings. This is the second tier of the
             * three-tier strategy for objects that don't implement IRecord.
             */

            // Step 1: Define property mappings for structural elements
            // We want to flatten the complex object structure into a simple table
            PushConfig structuralElementConfig = new PushConfig()
            {
                Table = "StructuralElements", // Optional: specify table name
                PropertyMappings = new Dictionary<string, string>
                {
                    // Direct property mappings
                    { "ElementName", "ElementName" },
                    { "Area", "CrossSectionalArea" },
                    { "Length", "Length" },
                    { "ElementType", "ElementType" },
                    { "LoadBearing", "IsLoadBearing" },
                    
                    // Nested property mappings using dot notation
                    { "StartX", "StartPosition.X" },
                    { "StartY", "StartPosition.Y" },
                    { "StartZ", "StartPosition.Z" },
                    { "EndX", "EndPosition.X" },
                    { "EndY", "EndPosition.Y" },
                    { "EndZ", "EndPosition.Z" },
                    
                    // Material property mappings
                    { "MaterialName", "Material.Name" },
                    { "MaterialDensity", "Material.Density" },
                    { "YoungModulus", "Material.YoungModulus" }
                },
                ExcludedProperties = new List<string> { "DesignDate" }, // Exclude some primitives
                ValidateMappings = true
            };

            // Step 2: Create structural elements with complex nested data
            List<StructuralElement> elements = new List<StructuralElement>
            {
                new StructuralElement()
                {
                    ElementName = "Beam-B001",
                    CrossSectionalArea = 0.025, // 25 cm²
                    Length = 6.0, // 6 metres
                    StartPosition = new PositionCoordinates() { X = 0.0, Y = 0.0, Z = 3.0 },
                    EndPosition = new PositionCoordinates() { X = 6.0, Y = 0.0, Z = 3.0 },
                    Material = new MaterialProperties()
                    {
                        Name = "Steel S355",
                        Density = 7850.0,
                        YoungModulus = 210000000000.0 // 210 GPa
                    },
                    ElementType = ElementType.Beam,
                    IsLoadBearing = true
                },
                new StructuralElement()
                {
                    ElementName = "Column-C001",
                    CrossSectionalArea = 0.04, // 40 cm²
                    Length = 3.0, // 3 metres
                    StartPosition = new PositionCoordinates() { X = 0.0, Y = 0.0, Z = 0.0 },
                    EndPosition = new PositionCoordinates() { X = 0.0, Y = 0.0, Z = 3.0 },
                    Material = new MaterialProperties()
                    {
                        Name = "Concrete C30/37",
                        Density = 2400.0,
                        YoungModulus = 33000000000.0 // 33 GPa
                    },
                    ElementType = ElementType.Column,
                    IsLoadBearing = true
                }
            };

            // Step 3: Push with custom configuration
            List<object> pushedElements = adapter.Push(elements, actionConfig: structuralElementConfig);

            pushedElements.Should().HaveCount(2, "Both structural elements should be pushed successfully");

            // Step 4: Verify data was mapped correctly
            CustomSqlRequest getAllRequest = new CustomSqlRequest()
            {
                SqlQuery = "SELECT * FROM StructuralElements ORDER BY ElementName",
                IsReadOnly = true
            };

            IEnumerable<object> results = adapter.Pull(getAllRequest);
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull();
            queryResult.Data.Should().HaveCount(2);

            // Verify the beam data
            Dictionary<string, object> beamData = queryResult.Data[0];
            beamData["ElementName"].Should().Be("Beam-B001");
            beamData["Area"].Should().Be(0.025);
            beamData["StartX"].Should().Be(0.0);
            beamData["StartY"].Should().Be(0.0);
            beamData["StartZ"].Should().Be(3.0);
            beamData["EndX"].Should().Be(6.0);
            beamData["MaterialName"].Should().Be("Steel S355");
            beamData["MaterialDensity"].Should().Be(7850.0);

            // Verify excluded property is not present
            beamData.Should().NotContainKey("DesignDate", "Excluded property should not be in database");
        }

        [Test]
        public void Example_DeeplyNestedProperties_ThermalMapping()
        {
            /*
             * EXAMPLE: Deeply Nested Property Mapping
             * 
             * This example shows how to map properties that are multiple levels deep
             * in the object hierarchy, such as Material.Thermal.Conductivity.
             */

            // Step 1: Create config with deeply nested mappings
            PushConfig thermalConfig = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "ElementName", "ElementName" },
                    { "MaterialName", "Material.Name" },
                    { "Density", "Material.Density" },
                    
                    // Deeply nested thermal properties
                    { "ThermalConductivity", "Material.Thermal.Conductivity" },
                    { "SpecificHeat", "Material.Thermal.SpecificHeat" },
                    { "ExpansionCoeff", "Material.Thermal.ExpansionCoefficient" }
                }
            };

            // Step 2: Create element with complete thermal data
            StructuralElement elementWithThermal = new StructuralElement()
            {
                ElementName = "Wall-W001",
                Material = new MaterialProperties()
                {
                    Name = "Insulated Concrete",
                    Density = 1800.0,
                    Thermal = new ThermalProperties()
                    {
                        Conductivity = 0.15, // W/(m·K)
                        SpecificHeat = 1000.0, // J/(kg·K)
                        ExpansionCoefficient = 0.00001 // 1/K
                    }
                }
            };

            // Step 3: Push with thermal mapping
            List<object> pushed = adapter.Push(new List<StructuralElement> { elementWithThermal }, 
                                              actionConfig: thermalConfig);

            pushed.Should().HaveCount(1);

            // Step 4: Verify deeply nested properties were mapped correctly
            CustomSqlRequest thermalQuery = new CustomSqlRequest()
            {
                SqlQuery = "SELECT * FROM StructuralElement WHERE ElementName = 'Wall-W001'",
                IsReadOnly = true
            };

            IEnumerable<object> results = adapter.Pull(thermalQuery);
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull();
            queryResult.Data.Should().HaveCount(1);

            Dictionary<string, object> wallData = queryResult.Data[0];
            wallData["ThermalConductivity"].Should().Be(0.15);
            wallData["SpecificHeat"].Should().Be(1000.0);
            wallData["ExpansionCoeff"].Should().Be(0.00001);
        }

        [Test]
        public void Example_PrimitiveFallback_NoConfiguration()
        {
            /*
             * EXAMPLE: Primitive Property Fallback (Tier 3)
             * 
             * This example shows what happens when no PushConfig is provided for complex objects.
             * The system falls back to the third tier: mapping only primitive properties.
             */

            // Step 1: Push complex object WITHOUT any configuration
            // This triggers the primitive fallback strategy
            StructuralElement elementNoConfig = new StructuralElement()
            {
                ElementName = "Beam-NoConfig",
                CrossSectionalArea = 0.03,
                Length = 4.5,
                ElementType = ElementType.Beam,
                DesignDate = new DateTime(2024, 1, 15),
                IsLoadBearing = true,
                
                // These complex properties will be ignored in fallback mode
                StartPosition = new PositionCoordinates() { X = 1.0, Y = 2.0, Z = 3.0 },
                EndPosition = new PositionCoordinates() { X = 5.5, Y = 2.0, Z = 3.0 },
                Material = new MaterialProperties()
                {
                    Name = "Steel S275",
                    Density = 7850.0
                }
            };

            // Step 2: Push without any config (triggers primitive fallback)
            List<object> pushed = adapter.Push(new List<StructuralElement> { elementNoConfig });

            pushed.Should().HaveCount(1, "Object should still be pushed using primitive fallback");

            // Step 3: Verify only primitive properties were stored
            CustomSqlRequest primitiveQuery = new CustomSqlRequest()
            {
                SqlQuery = "SELECT * FROM StructuralElement WHERE ElementName = 'Beam-NoConfig'",
                IsReadOnly = true
            };

            IEnumerable<object> results = adapter.Pull(primitiveQuery);
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull();
            queryResult.Data.Should().HaveCount(1);

            Dictionary<string, object> primitiveData = queryResult.Data[0];
            
            // Verify primitive properties are present
            primitiveData["ElementName"].Should().Be("Beam-NoConfig");
            primitiveData["CrossSectionalArea"].Should().Be(0.03);
            primitiveData["Length"].Should().Be(4.5);
            primitiveData["ElementType"].Should().Be((int)ElementType.Beam);
            primitiveData["IsLoadBearing"].Should().Be(true, "Boolean values should be automatically converted from SQLite storage");
            primitiveData.Should().ContainKey("BHoM_Guid");

            // Verify complex properties are NOT present
            primitiveData.Should().NotContainKey("StartPosition", "Complex properties should not be in fallback mode");
            primitiveData.Should().NotContainKey("Material", "Complex properties should not be in fallback mode");
            primitiveData.Should().NotContainKey("StartX", "Nested properties should not be in fallback mode");
        }

        [Test]
        public void Example_MixedMappingStrategy_WithExclusions()
        {
            /*
             * EXAMPLE: Mixed Mapping Strategy with Property Exclusions
             * 
             * This example shows how to combine explicit property mappings with
             * automatic primitive inclusion, while excluding specific properties.
             */

            // Step 1: Create config that maps some nested properties but excludes others
            PushConfig mixedConfig = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    // Map key geometric properties
                    { "StartX", "StartPoint.X" },
                    { "StartY", "StartPoint.Y" },
                    { "StartZ", "StartPoint.Z" },
                    { "EndX", "EndPoint.X" },
                    { "EndY", "EndPoint.Y" },
                    { "EndZ", "EndPoint.Z" },
                    
                    // Map essential material properties
                    { "MaterialName", "Material.Name" },
                    { "MaterialDensity", "Material.Density" }
                },
                ExcludedProperties = new List<string> 
                { 
                    "DesignDate", // Exclude this primitive property
                    "ElementType" // Exclude this enum property
                },
                ValidateMappings = true
            };

            // Step 2: Create element with full data
            StructuralElement mixedElement = new StructuralElement()
            {
                ElementName = "Beam-Mixed",
                CrossSectionalArea = 0.035,
                Length = 5.5,
                ElementType = ElementType.Beam, // This will be excluded
                DesignDate = new DateTime(2024, 1, 15), // This will be excluded
                IsLoadBearing = true, // This will be included (not excluded)
                StartPosition = new PositionCoordinates() { X = 2.0, Y = 1.0, Z = 2.5 },
                EndPosition = new PositionCoordinates() { X = 7.5, Y = 1.0, Z = 2.5 },
                Material = new MaterialProperties()
                {
                    Name = "Timber GL24h",
                    Density = 420.0,
                    YoungModulus = 11600000000.0
                }
            };

            // Step 3: Push with mixed configuration
            List<object> pushed = adapter.Push(new List<StructuralElement> { mixedElement }, 
                                              actionConfig: mixedConfig);

            pushed.Should().HaveCount(1);

            // Step 4: Verify mixed mapping results
            CustomSqlRequest mixedQuery = new CustomSqlRequest()
            {
                SqlQuery = "SELECT * FROM StructuralElement WHERE ElementName = 'Beam-Mixed'",
                IsReadOnly = true
            };

            IEnumerable<object> results = adapter.Pull(mixedQuery);
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull();
            queryResult.Data.Should().HaveCount(1);

            Dictionary<string, object> mixedData = queryResult.Data[0];

            // Verify mapped properties are present
            mixedData["StartX"].Should().Be(2.0);
            mixedData["StartY"].Should().Be(1.0);
            mixedData["StartZ"].Should().Be(2.5);
            mixedData["MaterialName"].Should().Be("Timber GL24h");
            mixedData["MaterialDensity"].Should().Be(420.0);

            // Verify non-excluded primitives are present
            mixedData["ElementName"].Should().Be("Beam-Mixed");
            mixedData["CrossSectionalArea"].Should().Be(0.035);
            mixedData["Length"].Should().Be(5.5);
            mixedData["IsLoadBearing"].Should().Be(true, "Boolean values should be automatically converted from SQLite storage");
            mixedData.Should().ContainKey("BHoM_Guid");

            // Verify excluded properties are NOT present
            mixedData.Should().NotContainKey("DesignDate", "Excluded primitive should not be present");
            mixedData.Should().NotContainKey("ElementType", "Excluded enum should not be present");

            // Verify non-mapped complex properties are NOT present
            mixedData.Should().NotContainKey("YoungModulus", "Non-mapped material property should not be present");
        }

        [Test]
        public void Example_FilteringComplexMappedData()
        {
            /*
             * EXAMPLE: Filtering Complex Mapped Data
             * 
             * This example shows how to filter data that was stored using complex property mappings.
             * You can filter on both the mapped columns and the automatically included primitives.
             */

            // Step 1: Setup multiple structural elements with mappings
            PushConfig structuralConfig = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "StartX", "StartPoint.X" },
                    { "StartY", "StartPoint.Y" },
                    { "MaterialName", "Material.Name" },
                    { "MaterialDensity", "Material.Density" }
                }
            };

            List<StructuralElement> elements = new List<StructuralElement>
            {
                new StructuralElement()
                {
                    ElementName = "Beam-001",
                    IsLoadBearing = true,
                    StartPosition = new PositionCoordinates() { X = 0.0, Y = 0.0, Z = 3.0 },
                    Material = new MaterialProperties() { Name = "Steel S355", Density = 7850.0 }
                },
                new StructuralElement()
                {
                    ElementName = "Beam-002",
                    IsLoadBearing = false,
                    StartPosition = new PositionCoordinates() { X = 5.0, Y = 0.0, Z = 3.0 },
                    Material = new MaterialProperties() { Name = "Steel S355", Density = 7850.0 }
                },
                new StructuralElement()
                {
                    ElementName = "Column-001",
                    IsLoadBearing = true,
                    StartPosition = new PositionCoordinates() { X = 0.0, Y = 0.0, Z = 0.0 },
                    Material = new MaterialProperties() { Name = "Concrete C30", Density = 2400.0 }
                }
            };

            adapter.Push(elements, actionConfig: structuralConfig);

            // Step 2: Filter by mapped property (material name)
            EqualityFilterRequest materialFilter = new EqualityFilterRequest()
            {
                TableName = "StructuralElement",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "MaterialName",
                        Values = new List<object> { "Steel S355" }
                    }
                }
            };

            IEnumerable<object> steelResults = adapter.Pull(materialFilter);
            QueryResult steelQuery = steelResults.FirstOrDefault() as QueryResult;

            steelQuery.Data.Should().HaveCount(2, "Should find both steel beams");

            // Step 3: Filter by combination of mapped and primitive properties
            EqualityFilterRequest combinedFilter = new EqualityFilterRequest()
            {
                TableName = "StructuralElement",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "MaterialName",
                        Values = new List<object> { "Steel S355" }
                    },
                    new ColumnFilter()
                    {
                        ColumnName = "IsLoadBearing",
                        Values = new List<object> { true }
                    }
                },
                Logic = LogicalOperator.And
            };

            IEnumerable<object> combinedResults = adapter.Pull(combinedFilter);
            QueryResult combinedQuery = combinedResults.FirstOrDefault() as QueryResult;

            combinedQuery.Data.Should().HaveCount(1, "Should find only load-bearing steel beam");
            combinedQuery.Data[0]["ElementName"].Should().Be("Beam-001");

            // Step 4: Filter by range on mapped numeric property
            RangeFilterRequest positionFilter = new RangeFilterRequest()
            {
                TableName = "StructuralElement",
                ColumnRanges = new Dictionary<string, GeneralDomain>
                {
                    {
                        "StartX",
                        new GeneralDomain() { Min = 2.0, Max = 10.0 }
                    }
                }
            };

            IEnumerable<object> positionResults = adapter.Pull(positionFilter);
            QueryResult positionQuery = positionResults.FirstOrDefault() as QueryResult;

            positionQuery.Data.Should().HaveCount(1, "Should find beam with StartX = 5.0");
            positionQuery.Data[0]["ElementName"].Should().Be("Beam-002");
        }
    }
}
