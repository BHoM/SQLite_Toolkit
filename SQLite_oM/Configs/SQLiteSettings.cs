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

using BH.oM.Base.Attributes;
using BH.oM.Adapter;
using System.ComponentModel;

namespace BH.oM.SQLite.Configs
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Configuration settings for SQLite database connections and operations.")]
    public class SQLiteSettings : AdapterSettings
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The database mode determining how the SQLite database is created and accessed.")]
        public virtual DatabaseMode DatabaseMode { get; set; } = DatabaseMode.FileDatabase;

        [Description("The connection timeout in seconds for database operations.")]
        public virtual int ConnectionTimeoutSeconds { get; set; } = 30;

        [Description("Performance optimisation strategy for database operations.")]
        public virtual OptimisationMode OptimisationMode { get; set; } = OptimisationMode.Default;

        [Description("Whether to enable Write-Ahead Logging (WAL) mode for improved concurrency.")]
        public virtual bool EnableWalMode { get; set; } = true;

        [Description("Whether to enable foreign key constraints enforcement.")]
        public virtual bool EnableForeignKeys { get; set; } = true;

        [Description("Cache size in pages for the database. Negative values represent kilobytes.")]
        public virtual int CacheSize { get; set; } = -2000;

        [Description("Whether to create tables automatically when they do not exist during data operations.")]
        public virtual bool AutoCreateTables { get; set; } = true;

        [Description("Whether to automatically initialize system tables (__Types, __Schema) on database connection.")]
        public virtual bool InitialiseSystemTables { get; set; } = true;

        [Description("Defines how NaN (Not a Number) and Infinity values are handled during SQLite conversion operations.")]
        public virtual NaNHandling NaNHandling { get; set; } = NaNHandling.ConvertToNull;

        /***************************************************/
    }

    /***************************************************/
} 