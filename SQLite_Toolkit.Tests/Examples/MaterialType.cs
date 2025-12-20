/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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
using System;
using System.ComponentModel;

namespace BH.oM.SQLite.Examples
{
    /***************************************************/
    /****               Example Classes              ****/
    /***************************************************/

    [Description("Example IRecord implementation for simple sensor data. " +
        "Contains only primitive data types and can be automatically mapped to database tables.")]
    /***************************************************/
    /**** Properties                              ****/
    /***************************************************/

    public enum MaterialType
    {
        Unknown = 0,
        Steel = 1,
        Concrete = 2,
        Wood = 3,
        Aluminium = 4,
        Glass = 5,
        Plastic = 6

    }

    /***************************************************/

}

