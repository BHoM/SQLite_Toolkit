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
using BH.oM.SQLite;
using System.ComponentModel;

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Represents the definition of a column in a SQLite table including type, constraints, and metadata.")]
    public class Column : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The SQLite data type for this column.")]
        public virtual SqliteDataType DataType { get; set; } = SqliteDataType.TEXT;

        [Description("Whether this column can contain null values.")]
        public virtual bool AllowNull { get; set; } = true;

        [Description("Whether this column is a primary key.")]
        public virtual bool IsPrimaryKey { get; set; } = false;

        [Description("Whether this column auto-increments for integer primary keys.")]
        public virtual bool IsAutoIncrement { get; set; } = false;

        [Description("Whether this column has a unique constraint.")]
        public virtual bool IsUnique { get; set; } = false;

        [Description("The default value for this column as a string. null if no default.")]
        public virtual string DefaultValue { get; set; } = null;

        [Description("The maximum length for text columns. Zero means no limit.")]
        public virtual int MaxLength { get; set; } = 0;

        [Description("The position of this column in the table (0-based).")]
        public virtual int Position { get; set; } = 0;

        [Description("Additional column attributes or constraints as SQL text.")]
        public virtual string AdditionalConstraints { get; set; } = "";

        /***************************************************/
    }

    /***************************************************/
} 