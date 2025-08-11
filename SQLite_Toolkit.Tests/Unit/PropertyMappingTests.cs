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
using BH.Engine.SQLite;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Examples;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Spatial.ShapeProfiles;
using BH.oM.Geometry;
using BH.Engine.Structure;
using BH.Engine.Spatial;
using System.Collections.Generic;


namespace SQLite_Toolkit.Tests.Unit
{
    /// <summary>
    /// Unit tests for property mapping functionality including the three-tier strategy
    /// </summary>
    [TestFixture]
    public class PropertyMappingTests
    {
        [Test]
        public void Test_ResolveColumnSchema_IRecordObject_AllPropertiesIncluded()
        {
            // Test Tier 1: IRecord objects should have all properties automatically mapped
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            PushConfig config = null; // No config needed for IRecord
            
            // Act
            Dictionary<string, PropertyColumnInfo> columnSchema = sensorType.ResolveColumnSchema(config);
            
            // Assert
            columnSchema.Should().NotBeNull("IRecord objects should generate column schema");
            columnSchema.Should().NotBeEmpty("IRecord objects should have mappings for all properties");
            
            // Verify all primitive properties are included
            columnSchema.Should().ContainKey("SensorId", "string property should be mapped");
            columnSchema.Should().ContainKey("Temperature", "double property should be mapped");
            columnSchema.Should().ContainKey("Humidity", "double property should be mapped");
            columnSchema.Should().ContainKey("Timestamp", "DateTime property should be mapped");
            columnSchema.Should().ContainKey("IsValid", "bool property should be mapped");
            columnSchema.Should().ContainKey("StatusCode", "int property should be mapped");
            columnSchema.Should().ContainKey("BHoM_Guid", "BHoM_Guid should be automatically included");
            
            // Verify property types are correctly identified
            columnSchema["Temperature"].PropertyType.Should().Be(typeof(double));
            columnSchema["IsValid"].PropertyType.Should().Be(typeof(bool));
            columnSchema["Timestamp"].PropertyType.Should().Be(typeof(DateTime));
        }

        [Test]
        public void Test_ResolveColumnSchema_WithPushConfigMappings()
        {
            // Test Tier 2: PushConfig mappings with non-excluded primitives
            
            // Arrange
            Type barType = typeof(Bar);
            PushConfig config = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "BarName", "Name" },
                    { "StartX", "Start.Position.X" },
                    { "StartY", "Start.Position.Y" },
                    { "StartZ", "Start.Position.Z" },
                    { "EndX", "End.Position.X" },
                    { "EndY", "End.Position.Y" },
                    { "EndZ", "End.Position.Z" },
                    { "MaterialDamping", "SectionProperty.Material.DampingRatio" }
                },
                ExcludedProperties = new List<string> { "OrientationAngle" } // Exclude this primitive
            };
            
            // Act
            Dictionary<string, PropertyColumnInfo> columnSchema = barType.ResolveColumnSchema(config);
            
            // Assert
            columnSchema.Should().NotBeNull("Complex objects with config should generate column schema");
            columnSchema.Should().NotBeEmpty("Should have mappings from config and non-excluded primitives");
            
            // Verify mapped properties
            columnSchema.Should().ContainKey("BarName", "Direct property mapping should work");
            columnSchema.Should().ContainKey("StartX", "Nested property mapping should work");
            columnSchema.Should().ContainKey("MaterialDamping", "Two-level nested property mapping should work");

            
            // Verify primitive properties are included (except excluded ones)
            columnSchema.Should().ContainKey("Name", "Non-excluded primitive should be included");
            columnSchema.Should().ContainKey("FEAType", "Non-excluded primitive should be included");
            columnSchema.Should().NotContainKey("OrientationAngle", "Excluded primitive should not be included");
            
            // Verify BHoM_Guid is included
            columnSchema.Should().ContainKey("BHoM_Guid", "BHoM_Guid should always be included");
        }

        [Test]
        public void Test_ResolveColumnSchema_PrimitiveFallback()
        {
            // Test Tier 3: Fallback to primitive properties only
            
            // Arrange
            Type barType = typeof(Bar);
            PushConfig config = null; // No config provided
            
            // Act
            Dictionary<string, PropertyColumnInfo> columnSchema = barType.ResolveColumnSchema(config);
            
            // Assert
            columnSchema.Should().NotBeNull("Objects should generate primitive column schema as fallback");
            columnSchema.Should().NotBeEmpty("Should have mappings for primitive properties");
            
            // Verify only primitive properties are included
            columnSchema.Should().ContainKey("Name", "string primitive should be included");
            columnSchema.Should().ContainKey("OrientationAngle", "double primitive should be included");
            columnSchema.Should().ContainKey("FEAType", "enum primitive should be included");
            columnSchema.Should().ContainKey("BHoM_Guid", "BHoM_Guid should be included");
            
            // Verify complex properties are NOT included
            columnSchema.Should().NotContainKey("Start", "Complex object should not be included in fallback");
            columnSchema.Should().NotContainKey("End", "Complex object should not be included in fallback");
            columnSchema.Should().NotContainKey("SectionProperty", "Complex object should not be included in fallback");
            columnSchema.Should().NotContainKey("Release", "Complex object should not be included in fallback");
        }

        [Test]
        public void Test_ResolveColumnSchema_WithExcludedProperties()
        {
            // Test exclusion functionality
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            PushConfig config = new PushConfig()
            {
                ExcludedProperties = new List<string> { "StatusCode", "Timestamp" }
            };
            
            // Act
            Dictionary<string, PropertyColumnInfo> columnSchema = sensorType.ResolveColumnSchema(config);
            
            // Assert
            columnSchema.Should().NotBeNull("IRecord with exclusions should still generate schema");
            
            // Verify excluded properties are not included
            columnSchema.Should().NotContainKey("StatusCode", "Excluded property should not be mapped");
            columnSchema.Should().NotContainKey("Timestamp", "Excluded property should not be mapped");
            
            // Verify non-excluded properties are still included
            columnSchema.Should().ContainKey("SensorId", "Non-excluded property should be mapped");
            columnSchema.Should().ContainKey("Temperature", "Non-excluded property should be mapped");
            columnSchema.Should().ContainKey("Humidity", "Non-excluded property should be mapped");
            columnSchema.Should().ContainKey("IsValid", "Non-excluded property should be mapped");
            columnSchema.Should().ContainKey("BHoM_Guid", "BHoM_Guid should not be excludable");
        }

        [Test]
        public void Test_ResolveColumnSchema_InvalidPropertyPath()
        {
            // Test handling of invalid property mappings
            
            // Arrange
            Type barType = typeof(Bar);
            PushConfig config = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "ValidMapping", "Name" },
                    { "InvalidMapping", "NonExistentProperty.Value" },
                    { "InvalidNested", "SectionProperty.NonExistentProperty" }
                },
                ValidateMappings = false // Allow invalid mappings for this test
            };
            
            // Act
            Dictionary<string, PropertyColumnInfo> columnSchema = barType.ResolveColumnSchema(config);
            
            // Assert
            columnSchema.Should().NotBeNull("Should still generate schema despite invalid mappings");
            
            // Valid mapping should be included
            columnSchema.Should().ContainKey("ValidMapping", "Valid property mapping should work");
            
            // Invalid mappings should be filtered out
            columnSchema.Should().NotContainKey("InvalidMapping", "Invalid property path should be filtered out");
            columnSchema.Should().NotContainKey("InvalidNested", "Invalid nested property path should be filtered out");
        }

        [Test]
        public void Test_ExtractColumnValues_IRecordObject()
        {
            // Test value extraction from IRecord objects
            
            // Arrange
            SensorReading sensor = new SensorReading()
            {
                SensorId = "TEMP001",
                Temperature = 23.5,
                Humidity = 65.0,
                Timestamp = new DateTime(2024, 1, 15, 14, 30, 0),
                IsValid = true,
                StatusCode = 200
            };
            
            Dictionary<string, PropertyColumnInfo> columnSchema = typeof(SensorReading).ResolveColumnSchema(null);
            
            // Act
            Dictionary<string, object> columnValues = sensor.ExtractColumnValues(columnSchema);
            
            // Assert
            columnValues.Should().NotBeNull("Should extract column values");
            columnValues.Should().NotBeEmpty("Should have values for all mapped columns");
            
            // Verify extracted values
            columnValues["SensorId"].Should().Be("TEMP001");
            columnValues["Temperature"].Should().Be(23.5);
            columnValues["Humidity"].Should().Be(65.0);
            columnValues["IsValid"].Should().Be(true);
            columnValues["StatusCode"].Should().Be(200);
            columnValues["Timestamp"].Should().Be(new DateTime(2024, 1, 15, 14, 30, 0));
            columnValues["BHoM_Guid"].Should().Be(sensor.BHoM_Guid);
        }

        [Test]
        public void Test_ExtractColumnValues_WithNestedPropertyMappings()
        {
            // Test value extraction with complex nested property mappings
            
            // Arrange
            Steel steelMaterial = new Steel()
            {
                Name = "Steel S355",
                Density = 7850.0,
                ThermalExpansionCoeff = 1.2e-5
            };
            
            RectangleProfile profile = new RectangleProfile(0.5, 0.3, 0, new List<ICurve>());
            SteelSection section = BH.Engine.Structure.Create.SteelSectionFromProfile(profile, steelMaterial, "S355 Rectangle");
            
            Node startNode = new Node() { Position = new Point() { X = 0, Y = 0, Z = 0 } };
            Node endNode = new Node() { Position = new Point() { X = 5, Y = 0, Z = 0 } };
            
            Bar bar = new Bar()
            {
                Name = "Beam-001",
                Start = startNode,
                End = endNode,
                SectionProperty = section
            };
            
            PushConfig config = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "StartX", "Start.Position.X" },
                    { "EndX", "End.Position.X" },
                    { "MaterialDamping", "SectionProperty.Material.DampingRatio" }
                }
            };
            
            Dictionary<string, PropertyColumnInfo> columnSchema = typeof(Bar).ResolveColumnSchema(config);
            
            // Act
            Dictionary<string, object> columnValues = bar.ExtractColumnValues(columnSchema);
            
            // Assert
            columnValues.Should().NotBeNull("Should extract nested property values");
            
            // Verify nested property extraction
            columnValues["StartX"].Should().Be(0.0);
            columnValues["EndX"].Should().Be(5.0);
            columnValues["MaterialDamping"].Should().Be(0.0); // Default DampingRatio value
        }
    }
}
