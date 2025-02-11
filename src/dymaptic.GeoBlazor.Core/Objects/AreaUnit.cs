﻿using dymaptic.GeoBlazor.Core.Serialization;
using System.Text.Json.Serialization;


namespace dymaptic.GeoBlazor.Core.Objects;

/// <summary>
/// Units for area measurement. Use one of the possible values listed below or any of the numeric codes for area units.
///  <a target="_blank" href="https://developers.arcgis.com/javascript/latest/api-reference/esri-core-units.htm">
///         ArcGIS
///         JS API
///     </a>
/// Used by Widgets.
/// </summary>
[JsonConverter(typeof(EnumToKebabCaseStringConverter<AreaUnit>))]
public enum AreaUnit
{
#pragma warning disable CS1591
    Metric,
    Imperial,
    Acres,
    Ares,
    Hectares,
    SquareFeet,
    SquareMeters,
    SquareYards,
    SquareKilometers,
    SquareMiles,
    SquareInches,
    SquareUSFeet,
#pragma warning restore CS1591
}