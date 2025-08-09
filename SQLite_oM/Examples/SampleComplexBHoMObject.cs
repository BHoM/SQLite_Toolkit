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
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.oM.SQLite.Examples
{
    /***************************************************/
    /****               Example Classes              ****/
    /***************************************************/

    [Description("Simple position coordinates class for examples.")]
    public class PositionCoordinates : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("X coordinate.")]
        public virtual double X { get; set; } = 0.0;

        [Description("Y coordinate.")]
        public virtual double Y { get; set; } = 0.0;

        [Description("Z coordinate.")]
        public virtual double Z { get; set; } = 0.0;

        /***************************************************/
    }

    /***************************************************/

    [Description("Simple direction vector class for examples.")]
    public class DirectionVector : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("X component.")]
        public virtual double X { get; set; } = 0.0;

        [Description("Y component.")]
        public virtual double Y { get; set; } = 0.0;

        [Description("Z component.")]
        public virtual double Z { get; set; } = 0.0;

        /***************************************************/
    }

    /***************************************************/

    [Description("Example complex BHoM object representing a structural element. " +
        "Contains nested objects and requires property mapping configuration for database storage.")]
    public class StructuralElement : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Name of the structural element.")]
        public virtual string ElementName { get; set; } = "";

        [Description("Cross-sectional area in m².")]
        public virtual double CrossSectionalArea { get; set; } = 0.0;

        [Description("Length of the element in metres.")]
        public virtual double Length { get; set; } = 0.0;

        [Description("Start position of the element as coordinates.")]
        public virtual PositionCoordinates StartPosition { get; set; } = new PositionCoordinates();

        [Description("End position of the element as coordinates.")]
        public virtual PositionCoordinates EndPosition { get; set; } = new PositionCoordinates();

        [Description("Material properties of the element.")]
        public virtual MaterialProperties Material { get; set; } = new MaterialProperties();

        [Description("Load conditions applied to the element.")]
        public virtual List<LoadCondition> Loads { get; set; } = new List<LoadCondition>();

        [Description("Element classification.")]
        public virtual ElementType ElementType { get; set; } = ElementType.Beam;

        [Description("Date when the element was designed.")]
        public virtual DateTime DesignDate { get; set; } = DateTime.Now;

        [Description("Whether the element is load-bearing.")]
        public virtual bool IsLoadBearing { get; set; } = true;

        /***************************************************/
    }

    /***************************************************/

    [Description("Example nested object representing material properties.")]
    public class MaterialProperties : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Material name.")]
        public virtual string Name { get; set; } = "";

        [Description("Material density in kg/m³.")]
        public virtual double Density { get; set; } = 0.0;

        [Description("Young's modulus in Pa.")]
        public virtual double YoungModulus { get; set; } = 0.0;

        [Description("Yield strength in Pa.")]
        public virtual double YieldStrength { get; set; } = 0.0;

        [Description("Ultimate tensile strength in Pa.")]
        public virtual double UltimateStrength { get; set; } = 0.0;

        [Description("Material thermal properties.")]
        public virtual ThermalProperties Thermal { get; set; } = new ThermalProperties();

        /***************************************************/
    }

    /***************************************************/

    [Description("Example deeply nested object representing thermal properties.")]
    public class ThermalProperties : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Thermal conductivity in W/(m·K).")]
        public virtual double Conductivity { get; set; } = 0.0;

        [Description("Specific heat capacity in J/(kg·K).")]
        public virtual double SpecificHeat { get; set; } = 0.0;

        [Description("Coefficient of thermal expansion in 1/K.")]
        public virtual double ExpansionCoefficient { get; set; } = 0.0;

        /***************************************************/
    }

    /***************************************************/

    [Description("Example object representing a load condition.")]
    public class LoadCondition : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Type of load applied.")]
        public virtual LoadType LoadType { get; set; } = LoadType.Dead;

        [Description("Magnitude of the load in N.")]
        public virtual double Magnitude { get; set; } = 0.0;

        [Description("Direction vector for the load as coordinates.")]
        public virtual DirectionVector Direction { get; set; } = new DirectionVector();

        [Description("Point where the load is applied as coordinates.")]
        public virtual PositionCoordinates ApplicationPoint { get; set; } = new PositionCoordinates();

        /***************************************************/
    }

    /***************************************************/

    [Description("Example enumeration for structural element types.")]
    public enum ElementType
    {
        Beam = 0,
        Column = 1,
        Slab = 2,
        Wall = 3,
        Foundation = 4,
        Truss = 5
    }

    /***************************************************/

    [Description("Example enumeration for load types.")]
    public enum LoadType
    {
        Dead = 0,
        Live = 1,
        Wind = 2,
        Seismic = 3,
        Snow = 4,
        Temperature = 5
    }

    /***************************************************/
}
