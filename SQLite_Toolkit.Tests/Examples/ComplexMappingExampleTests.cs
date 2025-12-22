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
using BH.Adapter.SQLite;
using BH.oM.SQLite.Examples;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Spatial.ShapeProfiles;
using BH.oM.Geometry;
using BH.Engine.Structure;
using BH.Engine.Spatial;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite;
using BH.oM.Adapter.Commands;
using BH.Tests.SQLite.Base;

namespace BH.Tests.SQLite.Examples
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
        public void Example_Bar_BasicPropertyMapping()
        {
            // Step 1: Define property mappings for bars
            // We want to flatten the complex object structure into a simple table
            PushConfig barConfig = new PushConfig()
            {
                Table = "Bars", // Optional: specify table name
                PropertyMappings = new Dictionary<string, string>
                {
                    // Direct property mappings
                    { "BarName", "Name" },
                    { "Angle", "OrientationAngle" },
                    { "ElementType", "FEAType" },
                    
                    // Nested property mappings using dot notation
                    { "StartX", "Start.Position.X" },
                    { "StartY", "Start.Position.Y" },
                    { "StartZ", "Start.Position.Z" },
                    { "EndX", "End.Position.X" },
                    { "EndY", "End.Position.Y" },
                    { "EndZ", "End.Position.Z" },
                    
                    // Material property mappings - using interface-accessible properties only  
                    { "MaterialDamping", "SectionProperty.Material.DampingRatio" }
                },
                ExcludedProperties = new List<string> { "Support" }, // Exclude some primitives
                ValidateMappings = true
            };

            // Step 2: Create bars with complex nested data
            Steel steelMaterial = new Steel()
            {
                Name = "Steel S355",
                Density = 7850.0,
                YoungsModulus = 210000000000.0 // 210 GPa
            };

            Steel concreteMaterial = new Steel()
            {
                Name = "Concrete C30/37",
                Density = 2400.0,
                YoungsModulus = 33000000000.0 // 33 GPa
            };

            RectangleProfile beamProfile = new RectangleProfile(0.5, 0.25, 0, new List<ICurve>());
            RectangleProfile columnProfile = new RectangleProfile(0.4, 0.4, 0, new List<ICurve>());

            SteelSection beamSection = BH.Engine.Structure.Create.SteelSectionFromProfile(beamProfile, steelMaterial, "Beam Section");
            SteelSection columnSection = BH.Engine.Structure.Create.SteelSectionFromProfile(columnProfile, concreteMaterial, "Column Section");

            List<Bar> bars = new List<Bar>
            {
                new Bar()
                {
                    Name = "Beam-B001",
                    Start = new Node() { Position = new Point() { X = 0.0, Y = 0.0, Z = 3.0 } },
                    End = new Node() { Position = new Point() { X = 6.0, Y = 0.0, Z = 3.0 } },
                    SectionProperty = beamSection,
                    FEAType = BarFEAType.Flexural,
                    OrientationAngle = 0.0
                },
                new Bar()
                {
                    Name = "Column-C001",
                    Start = new Node() { Position = new Point() { X = 0.0, Y = 0.0, Z = 0.0 } },
                    End = new Node() { Position = new Point() { X = 0.0, Y = 0.0, Z = 3.0 } },
                    SectionProperty = columnSection,
                    FEAType = BarFEAType.Flexural,
                    OrientationAngle = 0.0
                }
            };

            // Step 3: Push with custom configuration
            List<object> pushedBars = adapter.Push(bars, actionConfig: barConfig);

            pushedBars.Should().HaveCount(2, "Both bars should be pushed successfully");

            // Step 4: Verify data was mapped correctly
            EqualityFilterRequest getAllRequest = new EqualityFilterRequest()
            {
                TableName = "Bars",
                ColumnFilters = new List<ColumnFilter>()
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Name",
                        Values = new List<object>()
                        {
                            "Beam-B001", "Column-C001"
                        }
                    }
                }// Empty filters means get all records
            };

            IEnumerable<object> results = adapter.Pull(getAllRequest);
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull();
            queryResult.Data.Should().HaveCount(2);

            // Verify the beam data
            Dictionary<string, object> beamData = queryResult.Data[0];
            beamData["BarName"].Should().Be("Beam-B001");
            beamData["ElementType"].Should().Be(BarFEAType.Flexural);
            beamData["StartX"].Should().Be(0.0);
            beamData["StartY"].Should().Be(0.0);
            beamData["StartZ"].Should().Be(3.0);
            beamData["EndX"].Should().Be(6.0);
            beamData["MaterialDamping"].Should().Be(0.0); // Default DampingRatio

            // Verify excluded property is not present
            beamData.Should().NotContainKey("Support", "Excluded property should not be in database");
        }

        [Test]
        public void Example_DeeplyNestedProperties_ThermalMapping()
        {
            // Step 1: Create config with deeply nested mappings
            PushConfig thermalConfig = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "BarName", "Name" },
                    { "MaterialDamping", "SectionProperty.Material.DampingRatio" },
                }
            };

            // Step 2: Create bar with complete material data
            Steel thermalMaterial = new Steel()
            {
                Name = "Structural Steel",
                Density = 7850.0,
                YoungsModulus = 210000000000.0, // 210 GPa
                PoissonsRatio = 0.3,
                ThermalExpansionCoeff = 1.2e-5,// 1/K
                DampingRatio = 0.05
            };

            RectangleProfile profile = new RectangleProfile(0.3, 0.2, 0, new List<ICurve>());
            SteelSection section = BH.Engine.Structure.Create.SteelSectionFromProfile(profile, thermalMaterial, "Thermal Section");

            Bar barWithThermalData = new Bar()
            {
                Name = "Bar-T001",
                Start = new Node() { Position = new Point() { X = 0, Y = 0, Z = 0 } },
                End = new Node() { Position = new Point() { X = 3, Y = 0, Z = 0 } },
                SectionProperty = section,
                FEAType = BarFEAType.Flexural
            };

            // Step 3: Push with thermal mapping
            List<object> pushed = adapter.Push(new List<Bar> { barWithThermalData },
                                              actionConfig: thermalConfig);

            pushed.Should().HaveCount(1);

            // Step 4: Verify deeply nested properties were mapped correctly
            EqualityFilterRequest thermalQuery = new EqualityFilterRequest()
            {
                TableName = "Bar",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "BarName",
                        Values = new List<object> { "Bar-T001" }
                    }
                }
            };

            IEnumerable<object> results = adapter.Pull(thermalQuery);
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull();
            queryResult.Data.Should().HaveCount(1);

            Dictionary<string, object> barData = queryResult.Data[0];
            barData["MaterialDamping"].Should().Be(0.05); // Default DampingRatio
        }

        [Test]
        public void Example_PrimitiveFallback_NoConfiguration()
        {
            // Step 1: Push complex object WITHOUT any configuration
            // This triggers the primitive fallback strategy
            Steel material = new Steel()
            {
                Name = "Steel S275",
                Density = 7850.0
            };

            RectangleProfile profile = new RectangleProfile(0.3, 0.2, 0, new List<ICurve>());
            SteelSection section = BH.Engine.Structure.Create.SteelSectionFromProfile(profile, material, "Basic Section");

            Bar barNoConfig = new Bar()
            {
                Name = "Bar-NoConfig",
                OrientationAngle = 0.0,
                FEAType = BarFEAType.Flexural,

                // These complex properties will be ignored in fallback mode
                Start = new Node() { Position = new Point() { X = 1.0, Y = 2.0, Z = 3.0 } },
                End = new Node() { Position = new Point() { X = 5.5, Y = 2.0, Z = 3.0 } },
                SectionProperty = section
            };

            // Step 2: Push without any config (triggers primitive fallback)
            List<object> pushed = adapter.Push(new List<Bar> { barNoConfig });

            pushed.Should().HaveCount(1, "Object should still be pushed using primitive fallback");

            // Step 3: Verify only primitive properties were stored
            EqualityFilterRequest primitiveQuery = new EqualityFilterRequest()
            {
                TableName = "Bar",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Name",
                        Values = new List<object> { "Bar-NoConfig" }
                    }
                }
            };

            IEnumerable<object> results = adapter.Pull(primitiveQuery);
            QueryResult queryResult = results.FirstOrDefault() as QueryResult;

            queryResult.Should().NotBeNull();
            queryResult.Data.Should().HaveCount(1);

            Dictionary<string, object> primitiveData = queryResult.Data[0];

            // Verify primitive properties are present
            primitiveData["Name"].Should().Be("Bar-NoConfig");
            primitiveData["OrientationAngle"].Should().Be(0.0);
            primitiveData["FEAType"].Should().Be(BarFEAType.Flexural);
            primitiveData.Should().ContainKey("BHoM_Guid");

            // Verify complex properties are NOT present
            primitiveData.Should().NotContainKey("Start", "Complex properties should not be in fallback mode");
            primitiveData.Should().NotContainKey("SectionProperty", "Complex properties should not be in fallback mode");
            primitiveData.Should().NotContainKey("StartX", "Nested properties should not be in fallback mode");
        }

        [Test]
        public void Example_MixedMappingStrategy_WithExclusions()
        {
            // Step 1: Create config that maps some nested properties but excludes others
            PushConfig mixedConfig = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    // Map key geometric properties
                    { "StartX", "Start.Position.X" },
                    { "StartY", "Start.Position.Y" },
                    { "StartZ", "Start.Position.Z" },
                    { "EndX", "End.Position.X" },
                    { "EndY", "End.Position.Y" },
                    { "EndZ", "End.Position.Z" },
                    
                    // Map essential material properties
                    { "MaterialDamping", "SectionProperty.Material.DampingRatio" }
                },
                ExcludedProperties = new List<string>
                {
                    "OrientationAngle", // Exclude this primitive property
                    "Release" // Exclude this complex property
                },
                ValidateMappings = true
            };

            // Step 2: Create bar with full data
            Steel timberMaterial = new Steel()
            {
                Name = "Timber GL24h",
                Density = 420.0,
                YoungsModulus = 11600000000.0
            };

            RectangleProfile profile = new RectangleProfile(0.4, 0.2, 0, new List<ICurve>());
            SteelSection section = BH.Engine.Structure.Create.SteelSectionFromProfile(profile, timberMaterial, "Mixed Section");

            Bar mixedBar = new Bar()
            {
                Name = "Bar-Mixed",
                OrientationAngle = 45.0, // This will be excluded
                FEAType = BarFEAType.Flexural, // This will be included (not excluded)
                Start = new Node() { Position = new Point() { X = 2.0, Y = 1.0, Z = 2.5 } },
                End = new Node() { Position = new Point() { X = 7.5, Y = 1.0, Z = 2.5 } },
                SectionProperty = section
            };

            // Step 3: Push with mixed configuration
            List<object> pushed = adapter.Push(new List<Bar> { mixedBar },
                                              actionConfig: mixedConfig);

            pushed.Should().HaveCount(1);

            // Step 4: Verify mixed mapping results
            EqualityFilterRequest mixedQuery = new EqualityFilterRequest()
            {
                TableName = "Bar",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "Name",
                        Values = new List<object> { "Bar-Mixed" }
                    }
                }
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
            mixedData["MaterialDamping"].Should().Be(0.0); // Default DampingRatio

            // Verify non-excluded primitives are present
            mixedData["Name"].Should().Be("Bar-Mixed");
            mixedData["FEAType"].Should().Be(BarFEAType.Flexural);
            mixedData.Should().ContainKey("BHoM_Guid");

            // Verify excluded properties are NOT present
            mixedData.Should().NotContainKey("OrientationAngle", "Excluded primitive should not be present");
            mixedData.Should().NotContainKey("Release", "Excluded complex property should not be present");

            // Verify non-mapped complex properties are NOT present
            mixedData.Should().NotContainKey("YoungsModulus", "Non-mapped material property should not be present");
        }

        [Test]
        public void Example_FilteringComplexMappedData()
        {
            // Step 1: Setup multiple bars with mappings
            PushConfig barConfig = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "StartX", "Start.Position.X" },
                    { "StartY", "Start.Position.Y" },
                    { "MaterialDamping", "SectionProperty.Material.DampingRatio" }
                }
            };

            // Create materials
            Steel steelMaterial = new Steel() { Name = "Steel S355", Density = 7850.0 };
            Steel concreteMaterial = new Steel() { Name = "Concrete C30", Density = 2400.0 };

            // Create sections
            RectangleProfile profile1 = new RectangleProfile(0.4, 0.2, 0, new List<ICurve>());
            RectangleProfile profile2 = new RectangleProfile(0.3, 0.3, 0, new List<ICurve>());

            SteelSection steelSection = BH.Engine.Structure.Create.SteelSectionFromProfile(profile1, steelMaterial, "Steel Section");
            SteelSection concreteSection = BH.Engine.Structure.Create.SteelSectionFromProfile(profile2, concreteMaterial, "Concrete Section");

            List<Bar> bars = new List<Bar>
            {
                new Bar()
                {
                    Name = "Bar-001",
                    FEAType = BarFEAType.Flexural,
                    Start = new Node() { Position = new Point() { X = 0.0, Y = 0.0, Z = 3.0 } },
                    End = new Node() { Position = new Point() { X = 3.0, Y = 0.0, Z = 3.0 } },
                    SectionProperty = steelSection
                },
                new Bar()
                {
                    Name = "Bar-002",
                    FEAType = BarFEAType.CompressionOnly,
                    Start = new Node() { Position = new Point() { X = 5.0, Y = 0.0, Z = 3.0 } },
                    End = new Node() { Position = new Point() { X = 8.0, Y = 0.0, Z = 3.0 } },
                    SectionProperty = steelSection
                },
                new Bar()
                {
                    Name = "Column-001",
                    FEAType = BarFEAType.Flexural,
                    Start = new Node() { Position = new Point() { X = 0.0, Y = 0.0, Z = 0.0 } },
                    End = new Node() { Position = new Point() { X = 0.0, Y = 0.0, Z = 3.0 } },
                    SectionProperty = concreteSection
                }
            };

            adapter.Push(bars, actionConfig: barConfig);

            // Step 2: Filter by mapped property (material damping)
            EqualityFilterRequest materialFilter = new EqualityFilterRequest()
            {
                TableName = "Bar",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "MaterialDamping",
                        Values = new List<object> { 0.0 }
                    }
                }
            };

            IEnumerable<object> steelResults = adapter.Pull(materialFilter);
            QueryResult steelQuery = steelResults.FirstOrDefault() as QueryResult;

            steelQuery.Data.Should().HaveCount(3, "Should find all bars with default damping (all bars have DampingRatio = 0.0)");

            // Step 3: Filter by combination of mapped and primitive properties
            EqualityFilterRequest combinedFilter = new EqualityFilterRequest()
            {
                TableName = "Bar",
                ColumnFilters = new List<ColumnFilter>
                {
                    new ColumnFilter()
                    {
                        ColumnName = "MaterialDamping",
                        Values = new List<object> { 0.0 }
                    },
                    new ColumnFilter()
                    {
                        ColumnName = "FEAType",
                        Values = new List<object> { BarFEAType.Flexural }
                    }
                },
                Logic = LogicalOperator.And
            };

            IEnumerable<object> combinedResults = adapter.Pull(combinedFilter);
            QueryResult combinedQuery = combinedResults.FirstOrDefault() as QueryResult;

            combinedQuery.Data.Should().HaveCount(2, "Should find both flexural bars with default damping (Bar-001 and Column-001)");

            // Verify we found both flexural bars
            List<string> foundNames = combinedQuery.Data.Select(row => row["Name"].ToString()).OrderBy(name => name).ToList();
            foundNames.Should().Contain("Bar-001");
            foundNames.Should().Contain("Column-001");

            // Step 4: Filter by range on mapped numeric property
            RangeFilterRequest positionFilter = new RangeFilterRequest()
            {
                TableName = "Bar",
                ColumnRanges = new List<Dictionary<string, GeneralDomain>>
                {
                    new Dictionary<string, GeneralDomain>
                    {
                        {
                            "StartX",
                            new GeneralDomain() { Min = 2.0, Max = 10.0 }
                        }
                    }
                }
            };

            IEnumerable<object> positionResults = adapter.Pull(positionFilter);
            QueryResult positionQuery = positionResults.FirstOrDefault() as QueryResult;

            positionQuery.Data.Should().HaveCount(1, "Should find bar with StartX = 5.0");
            positionQuery.Data[0]["Name"].Should().Be("Bar-002");
        }
    }
}

