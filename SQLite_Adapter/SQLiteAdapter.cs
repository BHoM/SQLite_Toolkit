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

using BH.Adapter;
using BH.oM.Base.Attributes;
using BH.oM.SQLite;
using BH.oM.SQLite.Configs;
using BH.Engine.SQLite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLitePCL;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter : BHoMAdapter
    {
        /***************************************************/
        /**** Constructors                              ****/
        /***************************************************/

        [Description("Adapter for SQLite.")]
        [Input("filepath", "File path to the SQLite database. Use empty string for in-memory database.")]
        [Input("settings", "SQLite-specific settings for database operations. If null, default settings will be used.")]
        [Input("active", "Whether the adapter should be active and ready for operations.")]
        [Output("adapter", "The created SQLite adapter.")]
        public SQLiteAdapter(string filepath = "", SQLiteSettings settings = null, bool active = false)
        {
            if (active)
            {
                // Set the SQLite-specific settings
                if (settings != null)
                    m_sqliteSettings = settings;

                // Store the file path and active state
                m_FilePath = filepath;
                m_AdapterSettings.UseAdapterId = false;

                ExecuteCommand(new oM.Adapter.Commands.Open() { FileName = m_FilePath });
            }
            else
                GC.Collect();
        }

        ~SQLiteAdapter()
        {
            ExecuteCommand(new oM.Adapter.Commands.Close());
        }

        /***************************************************/
        /**** Private  Fields                           ****/
        /***************************************************/

        private string m_FilePath = "";
        private SqliteConnection m_Connection = null;

        // Connection state and diagnostics
        private System.Data.ConnectionState m_ConnectionState = System.Data.ConnectionState.Closed;
        private string m_ConnectionString = "";
        private string m_SqliteVersion = "";
        private DateTime m_ConnectedAt = DateTime.MinValue;
        private DateTime m_LastUsed = DateTime.MinValue;

        // Database configuration state (actual vs requested)
        private SQLiteSettings m_sqliteSettings;
        private bool m_WalModeEnabled = false;
        private bool m_ForeignKeysEnabled = false;
        private int m_PageSize = 4096;
        private int m_CacheSize = -2000;



        /***************************************************/
    }
}


