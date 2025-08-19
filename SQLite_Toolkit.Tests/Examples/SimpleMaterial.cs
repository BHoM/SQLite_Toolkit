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
using System;
using System.ComponentModel;

namespace BH.oM.SQLite.Examples
{
    /***************************************************/
    /****               Example Classes              ****/
    /***************************************************/

    [Description("Example IRecord implementation for simple material properties. " +
        "Demonstrates how enums and different numeric types are handled automatically.")]
    public class SimpleMaterial : BHoMObject, IRecord
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Name of the material.")]
        public virtual string MaterialName { get; set; } = "";

        [Description("Density of the material in kg/m³.")]
        public virtual double Density { get; set; } = 0.0;

        [Description("Young's modulus in Pa.")]
        public virtual double YoungModulus { get; set; } = 0.0;

        [Description("Poisson's ratio (dimensionless).")]
        public virtual double PoissonRatio { get; set; } = 0.0;

        [Description("Material type classification.")]
        public virtual MaterialType Type { get; set; } = MaterialType.Unknown;

        [Description("Whether the material is recyclable.")]
        public virtual bool IsRecyclable { get; set; } = false;

        [Description("Cost per unit volume.")]
        public virtual decimal CostPerCubicMeter { get; set; } = 0.0m;

        /***************************************************/
    }

    /***************************************************/
}
