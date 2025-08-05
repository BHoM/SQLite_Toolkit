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

using BH.oM.Base;
using BH.oM.Base.Attributes;
using System;
using System.ComponentModel;

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes               ****/
    /***************************************************/

    [Description("Represents a type registration mapping between .NET types and SQLite database tables.")]
    public class TypeRegistration : BHoMObject
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        [Description("The unique identifier for this type registration.")]
        public virtual int Id { get; set; } = 0;

        [Description("The full type name including namespace (e.g., 'BH.oM.Structure.Elements.Bar').")]
        public virtual string FullTypeName { get; set; } = "";

        [Description("The database table name associated with this type.")]
        public virtual string TableName { get; set; } = "";

        [Description("The date and time when this type registration was created.")]
        public virtual DateTime DateCreated { get; set; } = DateTime.UtcNow;

        /***************************************************/
        /**** Constructors                              ****/
        /***************************************************/

        [Description("Create an empty TypeRegistration.")]
        public TypeRegistration()
        {
        }

        /***************************************************/

        [Description("Create a TypeRegistration with specified type name and table name.")]
        public TypeRegistration(string fullTypeName, string tableName)
        {
            FullTypeName = fullTypeName;
            TableName = tableName;
            DateCreated = DateTime.UtcNow;
        }

        /***************************************************/

        [Description("Create a TypeRegistration from a .NET Type.")]
        public TypeRegistration(Type type, string tableName = "")
        {
            if (type != null)
            {
                FullTypeName = type.FullName ?? type.Name;
                TableName = string.IsNullOrWhiteSpace(tableName) ? type.Name : tableName;
                DateCreated = DateTime.UtcNow;
            }
        }

        /***************************************************/
    }

    /***************************************************/
}