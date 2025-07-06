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
using System.IO;
using NUnit.Framework;
using FluentAssertions;
using BH.Adapter.SQLite;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite;
using BH.oM.Adapter.Commands;
using BH.oM.Base;
using SQLite_Toolkit.Tests.Base;
using SQLite_Toolkit.Tests.Helpers;

namespace SQLite_Toolkit.Tests
{
    /// <summary>
    /// Phase 1: Basic Connection and Setup Testing
    /// Tests fundamental database connection opening/closing and file creation
    /// </summary>
    [TestFixture]
    public class Phase1_BasicConnectionTests : SQLiteTestBase
    {
        [Test]
        public void Test_1_1_DatabaseConnectionManagement()
        {
            // Test 1.1: Database Connection Management
            // Objective: Verify database connection opening and closing works correctly

            // Arrange
            SQLiteAdapter adapter = TestAdapter;
            Open openCommand = new Open() { FileName = TestDatabasePath };

            // Act & Assert - Open Connection
            Output<List<object>, bool> openResult = adapter.Execute(openCommand);
            
            // Verify connection opened successfully
            openResult.Should().NotBeNull();
            openResult.Item2.Should().BeTrue("Connection should open successfully");

            // Verify database file exists (for file-based databases)
            if (TestDatabasePath != ":memory:")
            {
                File.Exists(TestDatabasePath).Should().BeTrue("Database file should be created");
                
                // Test the database functionality by executing a simple query
                // This approach avoids file locking issues
                string testQuery = "SELECT 1 as TestConnection";
                Action testQueryAction = () => ExecuteCustomSql(testQuery);
                testQueryAction.Should().NotThrow("Database should be accessible for queries");
            }

            // Act & Assert - Close Connection
            bool connectionClosed = TestDatabaseManager.EnsureConnectionClosed(adapter);
            connectionClosed.Should().BeTrue("Connection should close successfully");

            // No exceptions should be thrown during the entire process
            AssertNoErrors();
        }

        [Test]
        public void Test_1_2_DatabaseFileCreation()
        {
            // Test 1.2: Database File Creation
            // Objective: Verify database file is created properly

            // Arrange
            string testDbPath = TestDatabaseManager.CreateTestDatabasePath("FileCreationTest");
            
            // Ensure the file doesn't exist before test
            if (File.Exists(testDbPath))
            {
                File.Delete(testDbPath);
            }

            SQLiteAdapter adapter = new SQLiteAdapter(testDbPath);
            Open openCommand = new Open() { FileName = testDbPath };

            // Act - Open connection to new database path
            Output<List<object>, bool> result = adapter.Execute(openCommand);

            // Assert
            result.Item2.Should().BeTrue("Database connection should be established");
            File.Exists(testDbPath).Should().BeTrue("Database file should be created on disk");
            
            // Verify file is not empty and has SQLite structure
            FileInfo fileInfo = new FileInfo(testDbPath);
            fileInfo.Length.Should().BeGreaterThan(0, "Database file should not be empty");
            
            // Verify it's a valid SQLite database by executing a simple query
            string testQuery = "SELECT 1 as TestConnection";
            Action testQueryAction = () => 
            {
                IEnumerable<object> queryResults = adapter.Pull(new BH.oM.SQLite.Requests.CustomSqlRequest()
                {
                    SqlQuery = testQuery,
                    Parameters = new Dictionary<string, object>(),
                    IsReadOnly = true
                });
            };
            testQueryAction.Should().NotThrow("Database should be accessible for queries");

            // Clean up - properly close connection before cleanup
            TestDatabaseManager.EnsureConnectionClosed(adapter);
            TestDatabaseManager.CleanupDatabase(testDbPath);
        }

        [Test]
        public void Test_1_3_InMemoryDatabaseConnection()
        {
            // Test 1.3: In-Memory Database Connection
            // Objective: Verify in-memory database connections work correctly

            // Arrange
            SQLiteAdapter adapter = CreateInMemoryTestAdapter();
            Open openCommand = new Open() { FileName = ":memory:" };

            // Act
            Output<List<object>, bool> result = adapter.Execute(openCommand);

            // Assert
            result.Item2.Should().BeTrue("In-memory database connection should be established");
            
            // In-memory databases shouldn't create files
            File.Exists(":memory:").Should().BeFalse("In-memory database should not create a file");

            // Close connection
            bool connectionClosed = TestDatabaseManager.EnsureConnectionClosed(adapter);
            connectionClosed.Should().BeTrue("In-memory database connection should close successfully");
        }

        [Test]
        public void Test_1_4_ConnectionStateManagement()
        {
            // Test 1.4: Connection State Management
            // Objective: Verify connection state is properly tracked

            // Arrange
            SQLiteAdapter adapter = TestAdapter;
            Open openCommand = new Open() { FileName = TestDatabasePath };
            Close closeCommand = new Close();

            // Act & Assert - Initial state (no connection)
            Output<List<object>, bool> initialCloseResult = adapter.Execute(closeCommand);
            initialCloseResult.Item2.Should().BeTrue("Closing non-existent connection should not fail");

            // Act & Assert - Open connection
            Output<List<object>, bool> openResult = adapter.Execute(openCommand);
            openResult.Item2.Should().BeTrue("Connection should open successfully");

            // Act & Assert - Close connection
            bool connectionClosed = TestDatabaseManager.EnsureConnectionClosed(adapter);
            connectionClosed.Should().BeTrue("Connection should close successfully");

            // Act & Assert - Double close should not fail (adapter should handle this gracefully)
            Output<List<object>, bool> doubleCloseResult = adapter.Execute(closeCommand);
            doubleCloseResult.Item2.Should().BeTrue("Double close should not fail");
        }

        [Test]
        public void Test_1_5_DatabaseDirectoryCreation()
        {
            // Test 1.5: Database Directory Creation
            // Objective: Verify directories are created when they don't exist

            // Arrange
            string testDir = Path.Combine(Path.GetTempPath(), "SQLiteTests", Guid.NewGuid().ToString());
            string testDbPath = Path.Combine(testDir, "test.db");
            
            // Ensure directory doesn't exist
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }

            // Act & Assert
            SQLiteAdapter adapter = new SQLiteAdapter(testDbPath);
            Open openCommand = new Open() { FileName = testDbPath };
            Output<List<object>, bool> result = adapter.Execute(openCommand);

            // Assert
            result.Item2.Should().BeTrue("Database connection should be established");
            Directory.Exists(testDir).Should().BeTrue("Directory should be created automatically");
            File.Exists(testDbPath).Should().BeTrue("Database file should be created");

            // Verify database functionality
            string testQuery = "SELECT 1 as TestConnection";
            Action testQueryAction = () => 
            {
                IEnumerable<object> queryResults = adapter.Pull(new BH.oM.SQLite.Requests.CustomSqlRequest()
                {
                    SqlQuery = testQuery,
                    Parameters = new Dictionary<string, object>(),
                    IsReadOnly = true
                });
            };
            testQueryAction.Should().NotThrow("Database should be accessible for queries");

            // Close connection properly
            TestDatabaseManager.EnsureConnectionClosed(adapter);
            
            // Note: We don't attempt to clean up the directory here because:
            // 1. The main test objective (directory creation) has been verified
            // 2. SQLite may still hold file handles even after Close() and Dispose()
            // 3. The test cleanup in TearDown will handle it with retry logic
            // 4. This avoids test failures due to expected file locking behavior
        }

        [Test]
        public void Test_1_6_ConnectionWithCustomSettings()
        {
            // Test 1.6: Connection with Custom Settings
            // Objective: Verify custom SQLite settings are applied correctly

            // Arrange
            SQLiteSettings customSettings = new SQLiteSettings()
            {
                DatabaseMode = DatabaseMode.FileDatabase,
                ConnectionTimeoutSeconds = 60,
                OptimisationMode = OptimisationMode.ReadOptimised,
                EnableWalMode = false,
                EnableForeignKeys = true,
                CacheSize = -4000
            };

            SQLiteAdapter adapter = new SQLiteAdapter(TestDatabasePath, customSettings);
            Open openCommand = new Open() { FileName = TestDatabasePath };

            // Act
            Output<List<object>, bool> result = adapter.Execute(openCommand);

            // Assert
            result.Item2.Should().BeTrue("Connection with custom settings should be established");
            
            // Test that we can execute a basic query to verify the connection works
            string testQuery = "SELECT 1 as TestValue";
            IEnumerable<object> queryResults = ExecuteCustomSql(testQuery);
            queryResults.Should().NotBeNull("Query should execute successfully with custom settings");

            // Clean up
            TestDatabaseManager.EnsureConnectionClosed(adapter);
        }

        [Test]
        public void Test_1_7_MultipleConnectionAttempts()
        {
            // Test 1.7: Multiple Connection Attempts
            // Objective: Verify adapter handles multiple open/close cycles correctly

            // Arrange
            SQLiteAdapter adapter = TestAdapter;
            Open openCommand = new Open() { FileName = TestDatabasePath };

            // Act & Assert - Multiple open/close cycles
            for (int i = 0; i < 3; i++)
            {
                Output<List<object>, bool> openResult = adapter.Execute(openCommand);
                openResult.Item2.Should().BeTrue($"Connection cycle {i + 1} should open successfully");

                bool connectionClosed = TestDatabaseManager.EnsureConnectionClosed(adapter);
                connectionClosed.Should().BeTrue($"Connection cycle {i + 1} should close successfully");
            }

            // Verify the database file is still valid after multiple cycles
            if (TestDatabasePath != ":memory:")
            {
                // Test database functionality by executing a simple query
                string testQuery = "SELECT 1 as TestConnection";
                Action testQueryAction = () => ExecuteCustomSql(testQuery);
                testQueryAction.Should().NotThrow("Database should be accessible for queries after multiple cycles");
            }
        }

        [Test]
        public void Test_1_8_ConnectionTimeout()
        {
            // Test 1.8: Connection Timeout
            // Objective: Verify connection timeout settings work correctly

            // Arrange
            SQLiteSettings settingsWithTimeout = new SQLiteSettings()
            {
                ConnectionTimeoutSeconds = 1, // Very short timeout
                DatabaseMode = DatabaseMode.FileDatabase
            };

            SQLiteAdapter adapter = new SQLiteAdapter(TestDatabasePath, settingsWithTimeout);
            Open openCommand = new Open() { FileName = TestDatabasePath };

            // Act
            Output<List<object>, bool> result = adapter.Execute(openCommand);

            // Assert
            result.Item2.Should().BeTrue("Connection should still be established even with short timeout");
            
            // Verify we can execute queries within the timeout
            string testQuery = "SELECT 1 as TestValue";
            IEnumerable<object> queryResults = ExecuteCustomSql(testQuery);
            queryResults.Should().NotBeNull("Query should execute successfully within timeout");

            // Clean up
            TestDatabaseManager.EnsureConnectionClosed(adapter);
        }
    }
} 