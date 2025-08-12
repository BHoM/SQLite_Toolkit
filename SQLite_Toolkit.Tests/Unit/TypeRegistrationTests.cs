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
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Examples;
using Microsoft.Data.Sqlite;
using SQLite_Toolkit.Tests.Base;

namespace SQLite_Toolkit.Tests.Unit
{
    /// <summary>
    /// Unit tests for type registration and lookup functionality
    /// </summary>
    [TestFixture]
    public class TypeRegistrationTests : SQLiteTestBase
    {
        private SqliteConnection testConnection;

        [SetUp]
        public void SetUp()
        {
            // Create a fresh in-memory database for each test
            testConnection = new SqliteConnection("Data Source=:memory:");
            testConnection.Open();
            
            // Create system tables
            if (!testConnection.TableExists("__Types"))
            {
                BH.Engine.SQLite.Create.TypesTable(testConnection);
            }
        }

        [TearDown]
        public new void TearDown()
        {
            testConnection?.Close();
            testConnection?.Dispose();
        }

        [Test]
        public void Test_RegisterType_NewType_Success()
        {
            // Test registering a new type
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            
            // Act
            TypeRegistration registration = testConnection.RegisterType(sensorType);
            
            // Assert
            registration.Should().NotBeNull("Type registration should succeed");
            registration.FullTypeName.Should().Be(sensorType.FullName);
            registration.TableName.Should().NotBeNullOrEmpty("Table name should be generated");
            registration.TableName.Should().Be("SensorReading", "Table name should match class name");
            registration.Id.Should().BeGreaterThan(0, "Registration should have a valid ID");
        }

        [Test]
        public void Test_RegisterType_DuplicateType_ReturnsSameRegistration()
        {
            // Test that registering the same type twice returns the same registration
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            
            // Act
            TypeRegistration firstRegistration = testConnection.RegisterType(sensorType);
            TypeRegistration secondRegistration = testConnection.RegisterType(sensorType);
            
            // Assert
            firstRegistration.Should().NotBeNull();
            secondRegistration.Should().NotBeNull();
            firstRegistration.Id.Should().Be(secondRegistration.Id, "Should return same registration for duplicate type");
            firstRegistration.FullTypeName.Should().Be(secondRegistration.FullTypeName);
            firstRegistration.TableName.Should().Be(secondRegistration.TableName);
        }

        [Test]
        public void Test_RegisterType_ConflictingTableNames_GeneratesUniqueNames()
        {
            // Test that conflicting table names are resolved with unique suffixes
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            Type materialType = typeof(SimpleMaterial);
            
            // Manually insert a registration with a conflicting table name
            string conflictingSql = "INSERT INTO __Types (FullTypeName, TableName) VALUES (@typeName, @tableName)";
            using (SqliteCommand command = new SqliteCommand(conflictingSql, testConnection))
            {
                command.Parameters.AddWithValue("@typeName", "SomeOtherType");
                command.Parameters.AddWithValue("@tableName", "SimpleMaterial"); // This will conflict
                command.ExecuteNonQuery();
            }
            
            // Act
            TypeRegistration materialRegistration = testConnection.RegisterType(materialType);
            
            // Assert
            materialRegistration.Should().NotBeNull("Type registration should succeed despite conflict");
            materialRegistration.TableName.Should().NotBe("SimpleMaterial", "Should generate unique table name to avoid conflict");
            materialRegistration.TableName.Should().StartWith("SimpleMaterial", "Should start with original class name");
            materialRegistration.TableName.Should().MatchRegex(@"SimpleMaterial_\d+", "Should append numeric suffix for uniqueness");
        }

        [Test]
        public void Test_IsTypeRegistered_RegisteredType_ReturnsTrue()
        {
            // Test checking if a type is registered
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            testConnection.RegisterType(sensorType);
            
            // Act
            bool isRegistered = testConnection.IsTypeRegistered(sensorType);
            
            // Assert
            isRegistered.Should().BeTrue("Registered type should be detected as registered");
        }

        [Test]
        public void Test_IsTypeRegistered_UnregisteredType_ReturnsFalse()
        {
            // Test checking if an unregistered type is registered
            
            // Arrange
            Type materialType = typeof(SimpleMaterial);
            
            // Act
            bool isRegistered = testConnection.IsTypeRegistered(materialType);
            
            // Assert
            isRegistered.Should().BeFalse("Unregistered type should not be detected as registered");
        }

        [Test]
        public void Test_GetTypeRegistration_ExistingType_ReturnsRegistration()
        {
            // Test looking up an existing type registration
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            TypeRegistration originalRegistration = testConnection.RegisterType(sensorType);
            
            // Act
            TypeRegistration lookupResult = testConnection.GetTypeRegistration(sensorType.FullName);
            
            // Assert
            lookupResult.Should().NotBeNull("Should find existing type registration");
            lookupResult.Id.Should().Be(originalRegistration.Id);
            lookupResult.FullTypeName.Should().Be(originalRegistration.FullTypeName);
            lookupResult.TableName.Should().Be(originalRegistration.TableName);
        }

        [Test]
        public void Test_GetTypeRegistration_NonExistentType_ReturnsNull()
        {
            // Test looking up a non-existent type registration
            
            // Arrange
            string nonExistentTypeName = "Some.NonExistent.Type";
            
            // Act
            TypeRegistration lookupResult = testConnection.GetTypeRegistration(nonExistentTypeName);
            
            // Assert
            lookupResult.Should().BeNull("Should return null for non-existent type");
        }

        [Test]
        public void Test_GetTableName_RegisteredType_ReturnsTableName()
        {
            // Test getting table name for a registered type
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            TypeRegistration registration = testConnection.RegisterType(sensorType);
            
            // Act
            string tableName = testConnection.GetTableName(sensorType.FullName);
            
            // Assert
            tableName.Should().NotBeNullOrEmpty("Should return table name for registered type");
            tableName.Should().Be(registration.TableName);
        }

        [Test]
        public void Test_GetTableName_UnregisteredType_ReturnsNull()
        {
            // Test getting table name for an unregistered type
            
            // Arrange
            Type materialType = typeof(SimpleMaterial);
            
            // Act
            string tableName = testConnection.GetTableName(materialType.FullName);
            
            // Assert
            tableName.Should().BeNull("Should return null for unregistered type");
        }

        [Test]
        public void Test_GetTypeName_ExistingTable_ReturnsTypeName()
        {
            // Test reverse lookup: getting type name from table name
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            TypeRegistration registration = testConnection.RegisterType(sensorType);
            
            // Act
            string typeName = testConnection.GetTypeName(registration.TableName);
            
            // Assert
            typeName.Should().NotBeNullOrEmpty("Should return type name for existing table");
            typeName.Should().Be(sensorType.FullName);
        }

        [Test]
        public void Test_GetTypeName_NonExistentTable_ReturnsNull()
        {
            // Test reverse lookup for non-existent table
            
            // Arrange
            string nonExistentTableName = "NonExistentTable";
            
            // Act
            string typeName = testConnection.GetTypeName(nonExistentTableName);
            
            // Assert
            typeName.Should().BeNull("Should return null for non-existent table");
        }

        [Test]
        public void Test_TypeRegistrations_MultipleTypes_ReturnsAllRegistrations()
        {
            // Test getting all type registrations
            
            // Arrange
            Type sensorType = typeof(SensorReading);
            Type materialType = typeof(SimpleMaterial);
            
            TypeRegistration sensorRegistration = testConnection.RegisterType(sensorType);
            TypeRegistration materialRegistration = testConnection.RegisterType(materialType);
            
            // Act
            List<TypeRegistration> allRegistrations = testConnection.TypeRegistrations();
            
            // Assert
            allRegistrations.Should().NotBeNull("Should return list of registrations");
            allRegistrations.Should().HaveCount(2, "Should return all registered types");
            
            allRegistrations.Should().Contain(r => r.FullTypeName == sensorType.FullName, "Should include sensor type");
            allRegistrations.Should().Contain(r => r.FullTypeName == materialType.FullName, "Should include material type");
        }

        [Test]
        public void Test_TypeRegistrations_EmptyDatabase_ReturnsEmptyList()
        {
            // Test getting all type registrations from empty database
            
            // Act
            List<TypeRegistration> allRegistrations = testConnection.TypeRegistrations();
            
            // Assert
            allRegistrations.Should().NotBeNull("Should return empty list, not null");
            allRegistrations.Should().BeEmpty("Should return empty list for database with no registered types");
        }
    }
}
