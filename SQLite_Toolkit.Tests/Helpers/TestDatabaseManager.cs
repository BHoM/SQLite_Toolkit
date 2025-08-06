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
using System.IO;
using System.Collections.Generic;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite;

namespace SQLite_Toolkit.Tests.Helpers
{
    /// <summary>
    /// Helper class for managing test databases including creation, cleanup, and path management
    /// </summary>
    public static class TestDatabaseManager
    {
        private static readonly string TestDatabaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestDatabases");
        private static readonly List<string> CreatedDatabases = new List<string>();

        /// <summary>
        /// Creates a unique test database file path
        /// </summary>
        /// <param name="testName">Name of the test for unique identification</param>
        /// <returns>Full path to the test database file</returns>
        public static string CreateTestDatabasePath(string testName)
        {
            EnsureTestDirectoryExists();
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string fileName = $"{testName}_{timestamp}.db";
            string fullPath = Path.Combine(TestDatabaseDirectory, fileName);
            
            // Track created databases for cleanup
            CreatedDatabases.Add(fullPath);
            
            return fullPath;
        }

        /// <summary>
        /// Creates a test database with in-memory mode
        /// </summary>
        /// <returns>In-memory database connection string</returns>
        public static string CreateInMemoryDatabasePath()
        {
            return ":memory:";
        }

        /// <summary>
        /// Creates SQLite settings for testing
        /// </summary>
        /// <param name="mode">Database mode to use</param>
        /// <param name="filePath">File path for file-based databases</param>
        /// <returns>Configured SQLite settings</returns>
        public static SQLiteSettings CreateTestSettings(DatabaseMode mode = DatabaseMode.FileDatabase, string filePath = "")
        {
            return new SQLiteSettings()
            {
                DatabaseMode = mode,
                ConnectionTimeoutSeconds = 30,
                OptimisationMode = OptimisationMode.Default,
                EnableWalMode = false, // Disable WAL for test simplicity
                EnableForeignKeys = true,
                CacheSize = -1000,
                AutoCreateTables = true
            };
        }

        /// <summary>
        /// Verifies that a database file exists and is valid
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        /// <returns>True if file exists and appears to be a valid SQLite database</returns>
        public static bool VerifyDatabaseExists(string databasePath)
        {
            if (string.IsNullOrEmpty(databasePath) || databasePath == ":memory:")
                return true; // In-memory databases don't have files

            if (!File.Exists(databasePath))
                return false;

            try
            {
                // Check if file has basic SQLite header
                using (var fs = new FileStream(databasePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Length < 16)
                        return false;

                    var header = new byte[16];
                    fs.Read(header, 0, 16);
                    
                    // SQLite databases start with "SQLite format 3\0"
                    var expectedHeader = System.Text.Encoding.UTF8.GetBytes("SQLite format 3\0");
                    for (int i = 0; i < expectedHeader.Length; i++)
                    {
                        if (header[i] != expectedHeader[i])
                            return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying database: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cleans up a specific test database
        /// </summary>
        /// <param name="databasePath">Path to the database to clean up</param>
        public static void CleanupDatabase(string databasePath)
        {
            if (string.IsNullOrEmpty(databasePath) || databasePath == ":memory:")
                return;

            try
            {
                // Only attempt cleanup if the file is available for deletion
                if (File.Exists(databasePath) && IsFileAvailableForDeletion(databasePath))
                {
                    File.Delete(databasePath);
                }
                
                // Remove from tracking list regardless
                CreatedDatabases.Remove(databasePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete test database {databasePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures that a database connection is fully closed and file handles are released
        /// </summary>
        /// <param name="adapter">The SQLite adapter to close</param>
        /// <param name="timeoutMs">Maximum time to wait for closure in milliseconds</param>
        /// <returns>True if connection was successfully closed</returns>
        public static bool EnsureConnectionClosed(BH.Adapter.SQLite.SQLiteAdapter adapter, int timeoutMs = 1000)
        {
            if (adapter == null)
                return true;

            // Close the connection
            var closeCommand = new BH.oM.Adapter.Commands.Close();
            var result = adapter.Execute(closeCommand);
            
            if (!result.Item2)
            {
                Console.WriteLine("Warning: Close command returned false");
                return false;
            }

            // Force garbage collection to help release any remaining references
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Give SQLite a moment to release file handles
            System.Threading.Thread.Sleep(50);
            
            return true;
        }

        /// <summary>
        /// Verifies that a database file can be accessed for deletion (i.e., no file locks)
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        /// <returns>True if file can be accessed for deletion</returns>
        public static bool IsFileAvailableForDeletion(string databasePath)
        {
            if (string.IsNullOrEmpty(databasePath) || databasePath == ":memory:")
                return true;

            if (!File.Exists(databasePath))
                return true;

            try
            {
                // Try to open the file exclusively - this will fail if SQLite still has it locked
                using (var fs = new FileStream(databasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cleans up all created test databases
        /// </summary>
        public static void CleanupAllDatabases()
        {
            foreach (string databasePath in CreatedDatabases.ToArray())
            {
                CleanupDatabase(databasePath);
            }
        }

        /// <summary>
        /// Ensures the test database directory exists
        /// </summary>
        private static void EnsureTestDirectoryExists()
        {
            if (!Directory.Exists(TestDatabaseDirectory))
            {
                Directory.CreateDirectory(TestDatabaseDirectory);
            }
        }

        /// <summary>
        /// Creates test data SQL for a simple test table
        /// </summary>
        /// <returns>SQL statements to create and populate a test table</returns>
        public static string CreateTestDataSql()
        {
            return @"
CREATE TABLE TestTable (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Value REAL,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO TestTable (Name, Value) VALUES 
    ('Test Record 1', 100.5),
    ('Test Record 2', 200.75),
    ('Test Record 3', 300.25),
    ('Test Record 4', 400.0),
    ('Test Record 5', 500.5);
";
        }

        /// <summary>
        /// Clears all database files from the test databases folder.
        /// This ensures a clean state for each test run.
        /// </summary>
        public static void ClearTestDatabasesFolder()
        {
            try
            {
                string testDatabasesFolder = Path.Combine(GetSolutionRoot(), "TestDatabases");
                
                if (Directory.Exists(testDatabasesFolder))
                {
                    foreach (string file in Directory.GetFiles(testDatabasesFolder, "*.db"))
                    {
                        try
                        {
                            if (IsFileAvailableForDeletion(file))
                            {
                                File.Delete(file);
                                Console.WriteLine($"Deleted database file: {Path.GetFileName(file)}");
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Could not delete database file (in use): {Path.GetFileName(file)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Failed to delete database file {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to clear test databases folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the root directory of the solution.
        /// This is needed to find the TestDatabases folder relative to the project root.
        /// </summary>
        /// <returns>The root directory of the solution.</returns>
        private static string GetSolutionRoot()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            while (currentDir != null && !File.Exists(Path.Combine(currentDir, "SQLite_Toolkit.sln")))
            {
                currentDir = Path.GetDirectoryName(currentDir);
            }
            return currentDir ?? AppDomain.CurrentDomain.BaseDirectory;
        }
    }
} 