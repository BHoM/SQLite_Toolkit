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
using NUnit.Framework;
using FluentAssertions;
using BH.Engine.SQLite;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Examples;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Spatial.ShapeProfiles;
using BH.oM.Geometry;
using BH.Engine.Structure;
using BH.Engine.Spatial;

namespace SQLite_Toolkit.Tests.Unit
{
    /// <summary>
    /// Unit tests for validation methods including property paths and schema validation
    /// </summary>
    [TestFixture]
    public class ValidationTests
    {
        [Test]
        public void Test_ValidateTableName_ValidNames_ReturnsTrue()
        {
            // Test valid table names
            
            // Arrange & Act & Assert
            BH.Engine.SQLite.Query.IsValid("Users", true).Should().BeTrue("Simple name should be valid");
            BH.Engine.SQLite.Query.IsValid("SensorReadings", true).Should().BeTrue("CamelCase name should be valid");
            BH.Engine.SQLite.Query.IsValid("sensor_readings", true).Should().BeTrue("Snake_case name should be valid");
            BH.Engine.SQLite.Query.IsValid("Table123", true).Should().BeTrue("Name with numbers should be valid");
            BH.Engine.SQLite.Query.IsValid("_Table", true).Should().BeTrue("Name starting with underscore should be valid");
        }

        [Test]
        public void Test_ValidateTableName_InvalidNames_ReturnsFalse()
        {
            // Test invalid table names
            
            // Arrange & Act & Assert
            BH.Engine.SQLite.Query.IsValid("", true).Should().BeFalse("Empty name should be invalid");
            BH.Engine.SQLite.Query.IsValid(null, true).Should().BeFalse("Null name should be invalid");
            BH.Engine.SQLite.Query.IsValid("123Table", true).Should().BeFalse("Name starting with number should be invalid");
            BH.Engine.SQLite.Query.IsValid("Table Name", true).Should().BeFalse("Name with spaces should be invalid");
            BH.Engine.SQLite.Query.IsValid("Table-Name", true).Should().BeFalse("Name with hyphens should be invalid");
            BH.Engine.SQLite.Query.IsValid("Table.Name", true).Should().BeFalse("Name with dots should be invalid");
            BH.Engine.SQLite.Query.IsValid("Table;DROP", true).Should().BeFalse("SQL injection attempt should be invalid");
        }

        [Test]
        public void Test_ValidateColumnName_ValidNames_ReturnsTrue()
        {
            // Test valid column names
            
            // Arrange & Act & Assert
            BH.Engine.SQLite.Query.IsValid("Id").Should().BeTrue("Simple name should be valid");
            BH.Engine.SQLite.Query.IsValid("FirstName").Should().BeTrue("CamelCase name should be valid");
            BH.Engine.SQLite.Query.IsValid("first_name").Should().BeTrue("Snake_case name should be valid");
            BH.Engine.SQLite.Query.IsValid("Column123").Should().BeTrue("Name with numbers should be valid");
            BH.Engine.SQLite.Query.IsValid("_Column").Should().BeTrue("Name starting with underscore should be valid");
        }

        [Test]
        public void Test_ValidateColumnName_InvalidNames_ReturnsFalse()
        {
            // Test invalid column names
            
            // Arrange & Act & Assert
            BH.Engine.SQLite.Query.IsValid("").Should().BeFalse("Empty name should be invalid");
            BH.Engine.SQLite.Query.IsValid((string)null).Should().BeFalse("Null name should be invalid");
            BH.Engine.SQLite.Query.IsValid("123Column").Should().BeFalse("Name starting with number should be invalid");
            BH.Engine.SQLite.Query.IsValid("Column Name").Should().BeFalse("Name with spaces should be invalid");
            BH.Engine.SQLite.Query.IsValid("Column-Name").Should().BeFalse("Name with hyphens should be invalid");
            BH.Engine.SQLite.Query.IsValid("Column.Name").Should().BeFalse("Name with dots should be invalid");
        }

        [Test]
        public void Test_ValidateColumnNames_Collection_ValidatesAll()
        {
            // Test validation of column name collection
            
            // Arrange
            List<string> validNames = new List<string> { "Id", "Name", "CreatedDate" };
            List<string> invalidNames = new List<string> { "Id", "Invalid Name", "CreatedDate" };
            
            // Act & Assert
            BH.Engine.SQLite.Query.IsValid((IEnumerable<string>)validNames).Should().BeTrue("All valid names should pass validation");
            BH.Engine.SQLite.Query.IsValid((IEnumerable<string>)invalidNames).Should().BeFalse("Collection with invalid name should fail validation");
        }

        [Test]
        public void Test_IsValidPropertyPath_ValidPaths_ReturnsTrue()
        {
            // Test valid property paths
            
            // Arrange
            Type barType = typeof(Bar);
            
            // Act & Assert
            barType.IsValid("Name").Should().BeTrue("Direct property should be valid");
            barType.IsValid("Start.Position.X").Should().BeTrue("Two-level nested property should be valid");
            barType.IsValid("SectionProperty.Material.DampingRatio").Should().BeTrue("Two-level nested property should be valid");
            barType.IsValid("BHoM_Guid").Should().BeTrue("BHoM base property should be valid");
        }

        [Test]
        public void Test_IsValidPropertyPath_InvalidPaths_ReturnsFalse()
        {
            // Test invalid property paths
            
            // Arrange
            Type barType = typeof(Bar);
            
            // Act & Assert
            barType.IsValid("NonExistentProperty").Should().BeFalse("Non-existent property should be invalid");
            barType.IsValid("SectionProperty.NonExistentProperty").Should().BeFalse("Invalid nested property should be invalid");
            barType.IsValid("Start.Position.NonExistentAxis").Should().BeFalse("Invalid position property should be invalid");
            barType.IsValid("SectionProperty.Material.Name").Should().BeFalse("Interface properties requiring casting should be invalid");
            barType.IsValid("").Should().BeFalse("Empty path should be invalid");
            barType.IsValid(null).Should().BeFalse("Null path should be invalid");
        }

        [Test]
        public void Test_GetPropertyType_ValidPaths_ReturnsCorrectTypes()
        {
            // Test getting property types from valid paths
            
            // Arrange
            Type barType = typeof(Bar);
            
            // Act & Assert
            barType.GetPropertyType("Name").Should().Be(typeof(string), "String property should return string type");
            barType.GetPropertyType("OrientationAngle").Should().Be(typeof(double), "Double property should return double type");
            barType.GetPropertyType("FEAType").Should().Be(typeof(BarFEAType), "Enum property should return enum type");
            barType.GetPropertyType("Start.Position.X").Should().Be(typeof(double), "Nested double property should return double type");
            barType.GetPropertyType("SectionProperty.Material.DampingRatio").Should().Be(typeof(double), "Nested double property should return double type");
        }

        [Test]
        public void Test_GetPropertyType_InvalidPaths_ReturnsNull()
        {
            // Test getting property types from invalid paths
            
            // Arrange
            Type barType = typeof(Bar);
            
            // Act & Assert
            barType.GetPropertyType("NonExistentProperty").Should().BeNull("Invalid property should return null");
            barType.GetPropertyType("SectionProperty.NonExistentProperty").Should().BeNull("Invalid nested property should return null");
            barType.GetPropertyType("SectionProperty.Material.Name").Should().BeNull("Interface properties requiring casting should return null");
            barType.GetPropertyType("").Should().BeNull("Empty path should return null");
            barType.GetPropertyType(null).Should().BeNull("Null path should return null");
        }

        [Test]
        public void Test_ValidatePropertyMappings_ValidMappings_ReturnsTrue()
        {
            // Test validation of valid property mappings
            
            // Arrange
            Type barType = typeof(Bar);
            PushConfig config = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "BarName", "Name" },
                    { "StartX", "Start.Position.X" },
                    { "MaterialDamping", "SectionProperty.Material.DampingRatio" }
                }
            };
            
            // Act
            Dictionary<string, string> validMappings = config.ValidatePropertyMappings(barType);
            bool isValid = validMappings != null && validMappings.Count > 0;
            
            // Assert
            isValid.Should().BeTrue("All valid property mappings should pass validation");
        }

        [Test]
        public void Test_ValidatePropertyMappings_InvalidMappings_ReturnsFalse()
        {
            // Test validation of invalid property mappings
            
            // Arrange
            Type barType = typeof(Bar);
            PushConfig config = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "BarName", "Name" }, // Valid
                    { "InvalidMapping", "NonExistentProperty" }, // Invalid
                    { "MaterialDamping", "SectionProperty.Material.DampingRatio" } // Valid
                }
            };
            
            // Act
            Dictionary<string, string> validMappings = config.ValidatePropertyMappings(barType);
            bool isValid = validMappings == null || validMappings.Count < config.PropertyMappings.Count;
            
            // Assert
            isValid.Should().BeTrue("Invalid property mappings should be filtered out");
        }

        [Test]
        public void Test_ValidatePropertyMappings_EmptyConfig_ReturnsTrue()
        {
            // Test validation with empty or null config
            
            // Arrange
            Type barType = typeof(Bar);
            
            // Act & Assert - Null config should not throw and empty config should work
            PushConfig nullConfig = null;
            PushConfig emptyConfig = new PushConfig();
            
            // These should not throw exceptions
            Action nullConfigAction = () => nullConfig?.ValidatePropertyMappings(barType);
            Action emptyConfigAction = () => emptyConfig.ValidatePropertyMappings(barType);
            
            nullConfigAction.Should().NotThrow("Null config should not throw");
            emptyConfigAction.Should().NotThrow("Empty config should not throw");
        }

        [Test]
        public void Test_ValidatePropertyMappings_InvalidColumnNames_ReturnsFalse()
        {
            // Test validation with invalid column names in mappings
            
            // Arrange
            Type barType = typeof(Bar);
            PushConfig config = new PushConfig()
            {
                PropertyMappings = new Dictionary<string, string>
                {
                    { "Valid Name", "Name" }, // Invalid column name (space)
                    { "MaterialDamping", "SectionProperty.Material.DampingRatio" } // Valid
                }
            };
            
            // Act
            Dictionary<string, string> validMappings = config.ValidatePropertyMappings(barType);
            bool isValid = validMappings == null || validMappings.Count < config.PropertyMappings.Count;
            
            // Assert
            isValid.Should().BeTrue("Invalid column names should be filtered out");
        }

        [Test]
        public void Test_IsPrimitive_PrimitiveTypes_ReturnsTrue()
        {
            // Test primitive type detection for database compatibility
            
            // Arrange & Act & Assert
            typeof(string).IsPrimitive().Should().BeTrue("String should be primitive for database");
            typeof(int).IsPrimitive().Should().BeTrue("Int should be primitive for database");
            typeof(double).IsPrimitive().Should().BeTrue("Double should be primitive for database");
            typeof(bool).IsPrimitive().Should().BeTrue("Bool should be primitive for database");
            typeof(DateTime).IsPrimitive().Should().BeTrue("DateTime should be primitive for database");
            typeof(Guid).IsPrimitive().Should().BeTrue("Guid should be primitive for database");
            typeof(decimal).IsPrimitive().Should().BeTrue("Decimal should be primitive for database");
            typeof(BarFEAType).IsPrimitive().Should().BeTrue("Enum should be primitive for database");
        }

        [Test]
        public void Test_IsPrimitive_NullableTypes_ReturnsTrue()
        {
            // Test nullable primitive types
            
            // Arrange & Act & Assert
            typeof(int?).IsPrimitive().Should().BeTrue("Nullable int should be primitive for database");
            typeof(double?).IsPrimitive().Should().BeTrue("Nullable double should be primitive for database");
            typeof(bool?).IsPrimitive().Should().BeTrue("Nullable bool should be primitive for database");
            typeof(DateTime?).IsPrimitive().Should().BeTrue("Nullable DateTime should be primitive for database");
            typeof(Guid?).IsPrimitive().Should().BeTrue("Nullable Guid should be primitive for database");
        }

        [Test]
        public void Test_IsPrimitive_ComplexTypes_ReturnsFalse()
        {
            // Test complex types that are not primitive for database
            
            // Arrange & Act & Assert
            typeof(Point).IsPrimitive().Should().BeFalse("Point should not be primitive for database");
            typeof(Vector).IsPrimitive().Should().BeFalse("Vector should not be primitive for database");
            typeof(Steel).IsPrimitive().Should().BeFalse("Custom object should not be primitive for database");
            typeof(List<string>).IsPrimitive().Should().BeFalse("Collection should not be primitive for database");
            typeof(object).IsPrimitive().Should().BeFalse("Object should not be primitive for database");
        }

        [Test]
        public void Test_GetPrimitiveProperties_IRecordObject_ReturnsAllProperties()
        {
            // Test primitive property extraction from IRecord object
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            
            // Act
            Dictionary<string, Type> primitiveProperties = sensorType.GetPrimitiveProperties();
            
            // Assert
            primitiveProperties.Should().NotBeNull("Should return dictionary of primitive properties");
            primitiveProperties.Should().NotBeEmpty("IRecord object should have primitive properties");
            primitiveProperties.Should().ContainKey("SensorId", "String property should be included");
            primitiveProperties.Should().ContainKey("Temperature", "Double property should be included");
            primitiveProperties.Should().ContainKey("Humidity", "Double property should be included");
            primitiveProperties.Should().ContainKey("Timestamp", "DateTime property should be included");
            primitiveProperties.Should().ContainKey("IsValid", "Bool property should be included");
            primitiveProperties.Should().ContainKey("StatusCode", "Int property should be included");
            primitiveProperties.Should().ContainKey("BHoM_Guid", "BHoM_Guid should be included");
        }

        [Test]
        public void Test_GetPrimitiveProperties_ComplexObject_ReturnsOnlyPrimitives()
        {
            // Test primitive property extraction from complex object
            
            // Arrange
            Type barType = typeof(Bar);
            
            // Act
            Dictionary<string, Type> primitiveProperties = barType.GetPrimitiveProperties();
            
            // Assert
            primitiveProperties.Should().NotBeNull("Should return dictionary of primitive properties");
            primitiveProperties.Should().NotBeEmpty("Complex object should have some primitive properties");
            
            // Should include primitive properties
            primitiveProperties.Should().ContainKey("Name", "String property should be included");
            primitiveProperties.Should().ContainKey("OrientationAngle", "Double property should be included");
            primitiveProperties.Should().ContainKey("FEAType", "Enum property should be included");
            primitiveProperties.Should().ContainKey("BHoM_Guid", "BHoM_Guid should be included");
            
            // Should not include complex properties
            primitiveProperties.Should().NotContainKey("Start", "Complex object should not be included");
            primitiveProperties.Should().NotContainKey("End", "Complex object should not be included");
            primitiveProperties.Should().NotContainKey("SectionProperty", "Complex object should not be included");
            primitiveProperties.Should().NotContainKey("Release", "Collection should not be included");
        }
    }
}
