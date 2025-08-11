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
using BH.oM.SQLite;
using BH.oM.SQLite.Examples;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Spatial.ShapeProfiles;
using BH.oM.Geometry;
using BH.Engine.Structure;
using BH.Engine.Spatial;
using BH.oM.Base;


namespace SQLite_Toolkit.Tests.Unit
{
    /// <summary>
    /// Unit tests for IRecord interface detection and validation
    /// </summary>
    [TestFixture]
    public class IRecordDetectionTests
    {
        [Test]
        public void Test_IsIRecord_IRecordObject_ReturnsTrue()
        {
            // Test IRecord detection on objects that implement IRecord
            
            // Arrange
            SensorReading sensor = new SensorReading();
            SimpleMaterial material = new SimpleMaterial();
            
            // Act & Assert
            sensor.IsIRecord().Should().BeTrue("SensorReading implements IRecord and should be detected");
            material.IsIRecord().Should().BeTrue("SimpleMaterial implements IRecord and should be detected");
        }

        [Test]
        public void Test_IsIRecord_NonIRecordObject_ReturnsFalse()
        {
            // Test IRecord detection on objects that don't implement IRecord
            
            // Arrange
            Bar bar = new Bar();
            Point position = new Point();
            Steel materialProps = new Steel();
            
            // Act & Assert
            bar.IsIRecord().Should().BeFalse("Bar does not implement IRecord");
            position.IsIRecord().Should().BeFalse("Point does not implement IRecord");
            materialProps.IsIRecord().Should().BeFalse("Steel does not implement IRecord");
        }

        [Test]
        public void Test_IsIRecord_NullObject_ReturnsFalse()
        {
            // Test IRecord detection on null object
            
            // Arrange
            object nullObject = null;
            
            // Act & Assert
            nullObject.IsIRecord().Should().BeFalse("Null object should not be detected as IRecord");
        }

        [Test]
        public void Test_IsIRecord_TypeOverload_IRecordType_ReturnsTrue()
        {
            // Test IRecord detection on types that implement IRecord
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            Type materialType = typeof(SimpleMaterial);
            
            // Act & Assert
            sensorType.IsIRecord().Should().BeTrue("SensorReading type implements IRecord");
            materialType.IsIRecord().Should().BeTrue("SimpleMaterial type implements IRecord");
        }

        [Test]
        public void Test_IsIRecord_TypeOverload_NonIRecordType_ReturnsFalse()
        {
            // Test IRecord detection on types that don't implement IRecord
            
            // Arrange
            Type barType = typeof(Bar);
            Type positionType = typeof(Point);
            Type stringType = typeof(string);
            
            // Act & Assert
            barType.IsIRecord().Should().BeFalse("Bar type does not implement IRecord");
            positionType.IsIRecord().Should().BeFalse("Point type does not implement IRecord");
            stringType.IsIRecord().Should().BeFalse("string type does not implement IRecord");
        }

        [Test]
        public void Test_IsIRecord_TypeOverload_NullType_ReturnsFalse()
        {
            // Test IRecord detection on null type
            
            // Arrange
            Type nullType = null;
            
            // Act & Assert
            nullType.IsIRecord().Should().BeFalse("Null type should not be detected as IRecord");
        }

        [Test]
        public void Test_ValidateIRecordProperties_ValidIRecordObject_ReturnsTrue()
        {
            // Test validation of IRecord object with only primitive properties
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            
            // Act
            bool isValid = BH.Engine.SQLite.Compute.ValidateIRecordProperties(sensorType);
            
            // Assert
            isValid.Should().BeTrue("SensorReading should be valid as it contains only primitive properties");
        }

        [Test]
        public void Test_ValidateIRecordProperties_ValidSimpleMaterial_ReturnsTrue()
        {
            // Test validation of SimpleMaterial IRecord object
            
            // Arrange
            Type materialType = typeof(SimpleMaterial);
            
            // Act
            bool isValid = BH.Engine.SQLite.Compute.ValidateIRecordProperties(materialType);
            
            // Assert
            isValid.Should().BeTrue("SimpleMaterial should be valid as it contains only primitive properties including enums");
        }

        [Test]
        public void Test_ValidateIRecordProperties_NonIRecordType_ReturnsFalse()
        {
            // Test validation on types that don't implement IRecord
            
            // Arrange
            Type barType = typeof(Bar);
            
            // Act
            bool isValid = BH.Engine.SQLite.Compute.ValidateIRecordProperties(barType);
            
            // Assert
            isValid.Should().BeFalse("Non-IRecord types should not pass IRecord validation");
        }

        [Test]
        public void Test_ValidateIRecordProperties_NullType_ReturnsFalse()
        {
            // Test validation on null type
            
            // Arrange
            Type nullType = null;
            
            // Act
            bool isValid = BH.Engine.SQLite.Compute.ValidateIRecordProperties(nullType);
            
            // Assert
            isValid.Should().BeFalse("Null type should not pass IRecord validation");
        }

        // Create a test class that claims to implement IRecord but has complex properties
        private class InvalidIRecordExample : BHoMObject, IRecord
        {
            public new string Name { get; set; } = "";
            public Point ComplexProperty { get; set; } = new Point(); // This makes it invalid
            public double ValidPrimitive { get; set; } = 0.0;
        }

        [Test]
        public void Test_ValidateIRecordProperties_InvalidIRecordWithComplexProperties_ReturnsFalse()
        {
            // Test validation of IRecord object that incorrectly contains complex properties
            
            // Arrange
            Type invalidType = typeof(InvalidIRecordExample);
            
            // Act
            bool isValid = BH.Engine.SQLite.Compute.ValidateIRecordProperties(invalidType);
            
            // Assert
            isValid.Should().BeFalse("IRecord with complex properties should fail validation");
        }

        [Test]
        public void Test_IRecord_InheritanceChecking()
        {
            // Test that IRecord detection works correctly with inheritance
            
            // Arrange - Create a derived IRecord class
            Type baseIRecordType = typeof(IRecord);
            Type sensorType = typeof(SensorReading);
            Type materialType = typeof(SimpleMaterial);
            Type nonIRecordType = typeof(Bar);
            
            // Act & Assert
            baseIRecordType.IsAssignableFrom(sensorType).Should().BeTrue("SensorReading should be assignable from IRecord");
            baseIRecordType.IsAssignableFrom(materialType).Should().BeTrue("SimpleMaterial should be assignable from IRecord");
            baseIRecordType.IsAssignableFrom(nonIRecordType).Should().BeFalse("Bar should not be assignable from IRecord");
        }

        [Test]
        public void Test_IRecord_InterfaceImplementation()
        {
            // Test that IRecord properly inherits from IBHoMObject
            
            // Arrange
            SensorReading sensor = new SensorReading();
            SimpleMaterial material = new SimpleMaterial();
            
            // Act & Assert
            sensor.Should().BeAssignableTo<IBHoMObject>("IRecord should extend IBHoMObject");
            material.Should().BeAssignableTo<IBHoMObject>("IRecord should extend IBHoMObject");
            sensor.Should().BeAssignableTo<IRecord>("SensorReading should implement IRecord");
            material.Should().BeAssignableTo<IRecord>("SimpleMaterial should implement IRecord");
            
            // Verify BHoM properties are available
            sensor.BHoM_Guid.Should().NotBe(Guid.Empty, "BHoM_Guid should be available on IRecord objects");
            material.BHoM_Guid.Should().NotBe(Guid.Empty, "BHoM_Guid should be available on IRecord objects");
        }

        [Test]
        public void Test_IRecord_PropertyTypes_Validation()
        {
            // Test that all properties in IRecord examples are indeed primitive
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            Type materialType = typeof(SimpleMaterial);
            
            Dictionary<string, Type> sensorProperties = sensorType.GetPrimitiveProperties();
            Dictionary<string, Type> materialProperties = materialType.GetPrimitiveProperties();
            
            // Act & Assert
            sensorProperties.Should().NotBeEmpty("SensorReading should have primitive properties");
            materialProperties.Should().NotBeEmpty("SimpleMaterial should have primitive properties");
            
            // All detected properties should be primitive
            foreach (var kvp in sensorProperties)
            {
                kvp.Value.IsPrimitiveForDatabase().Should().BeTrue($"Property {kvp.Key} should be primitive for database");
            }
            
            foreach (var kvp in materialProperties)
            {
                kvp.Value.IsPrimitiveForDatabase().Should().BeTrue($"Property {kvp.Key} should be primitive for database");
            }
        }

        [Test]
        public void Test_IRecord_ThreeTierStrategy_TierOneDetection()
        {
            // Test that IRecord objects are correctly identified in the three-tier strategy
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            Type materialType = typeof(SimpleMaterial);
            Type complexType = typeof(Bar);
            
            // Act & Assert - Tier 1: IRecord detection
            sensorType.IsIRecord().Should().BeTrue("SensorReading should be detected as IRecord (Tier 1)");
            materialType.IsIRecord().Should().BeTrue("SimpleMaterial should be detected as IRecord (Tier 1)");
            complexType.IsIRecord().Should().BeFalse("Bar should not be IRecord (goes to Tier 2/3)");
        }
    }
}
