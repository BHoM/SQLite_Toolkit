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
 * The Free Software Foundation, either version 3.0 of the License, or          
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

using System.ComponentModel;
using BH.oM.Base.Attributes;

namespace BH.oM.SQLite
{
    /***************************************************/
    /****               Public Enums               ****/
    /***************************************************/

    [Description("Defines how NaN (Not a Number) and Infinity values are handled when converting to SQLite storage.")]
    public enum NaNHandling
    {
        [Description("Convert NaN and Infinity values to NULL in SQLite. When reading back, NULL values are converted back to NaN.")]
        [DisplayText("Convert to NULL")]
        ConvertToNull,

        [Description("Convert NaN and Infinity values to zero (0.0) in SQLite.")]
        [DisplayText("Convert to Zero")]
        ConvertToZero
    }

    /***************************************************/
}
