﻿using dymaptic.Blazor.GIS.API.Core.Components.Geometries;
using dymaptic.Blazor.GIS.API.Core.Components.Layers;
using dymaptic.Blazor.GIS.API.Core.Components.Popups;
using dymaptic.Blazor.GIS.API.Core.Components.Symbols;
using dymaptic.Blazor.GIS.API.Core.Components.Widgets;
using dymaptic.Blazor.GIS.API.Core.Exceptions;
using dymaptic.Blazor.GIS.API.Core.Objects;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;


// ReSharper disable RedundantCast

namespace dymaptic.Blazor.GIS.API.Core.Components.Views;

public partial class MapView : MapComponent
{
    public HashSet<Widget> Widgets { get; set; } = new();
    public List<Graphic> Graphics { get; set; } = new();

    [Parameter]
    public string Style { get; set; } = string.Empty;

    [Parameter]
    public string Class { get; set; } = string.Empty;

    [Inject]
    public IConfiguration Configuration { get; set; } = default!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter]
    public double Latitude
    {
        get => _latitude;
        set
        {
            if (Math.Abs(_latitude - value) > 0.0000000000001)
            {
                _latitude = value;
                _newPropertyValues[nameof(Latitude)] = value;
            }
        }
    }

    [Parameter]
    public double Longitude
    {
        get => _longitude;
        set
        {
            if (Math.Abs(_longitude - value) > 0.0000000000001)
            {
                _longitude = value;
                _newPropertyValues[nameof(Longitude)] = value;
            }
        }
    }

    [Parameter]
    public double Zoom
    {
        get => _zoom;
        set
        {
            if (Math.Abs(_zoom - value) > 0.0000000000001)
            {
                _zoom = value;
                _newPropertyValues[nameof(Zoom)] = value;
            }
        }
    }

    [Parameter]
    public double? Scale
    {
        get => _scale;
        set
        {
            if (_scale is null || value is null || (Math.Abs(_scale!.Value - value.Value) > 0.0000000000001))
            {
                _scale = value;

                if (value is not null)
                {
                    _newPropertyValues[nameof(Scale)] = value;
                }
            }
        }
    }

    [Parameter]
    public double Rotation
    {
        get => _rotation;
        set
        {
            if (Math.Abs(_rotation - value) > 0.0000000000001)
            {
                _rotation = value;
                _newPropertyValues[nameof(Rotation)] = value;
            }
        }
    }

    [Parameter]
    public Func<Point, Task>? OnClickAsyncHandler { get; set; }

    [Parameter]
    public Func<Point, Task>? OnPointerMoveHandler { get; set; }

    [Parameter]
    public Func<Task>? OnMapRenderedHandler { get; set; }
    
    [Parameter]
    public Func<SpatialReference, Task>? OnSpatialReferenceChangedHandler { get; set; }

    public Map? Map { get; set; }

    public WebMap? WebMap { get; set; }

    public SpatialReference? SpatialReference
    {
        get => _spatialReference;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_spatialReference?.Wkid != value?.Wkid)
            {
                _spatialReference = value;
                NeedsRender = true;
            }
        }
    }
    
    public Constraints? Constraints { get; set; }

    public string? ErrorMessage { get; set; }
    protected string? ApiKey
    {
        get => _apiKey;
        set
        {
            _apiKey = value;

            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                Configuration["ArcGISApiKey"] = value;
            }
        }
    }
    protected DotNetObjectReference<MapView> DotNetObjectReference =>
        Microsoft.JSInterop.DotNetObjectReference.Create(this);

    public override void Refresh()
    {
        NeedsRender = true;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnJavascriptError(string error)
    {
#if DEBUG
        ErrorMessage = error.Replace("\n", "<br>");
        StateHasChanged();
#endif
        throw new JavascriptException(error);
    }

    [JSInvokable]
    public void OnJavascriptClick(Point mapPoint)
    {
        OnClickAsyncHandler?.Invoke(mapPoint);
    }

    [JSInvokable]
    public void OnJavascriptPointerMove(Point mapPoint)
    {
        OnPointerMoveHandler?.Invoke(mapPoint);
    }

    [JSInvokable]
    public void OnViewRendered()
    {
        OnMapRenderedHandler?.Invoke();
    }

    [JSInvokable]
    public void OnSpatialReferenceChanged(SpatialReference spatialReference)
    {
        OnSpatialReferenceChangedHandler?.Invoke(spatialReference);
    }

    public override async Task UpdateComponent()
    {
        if (!MapRendered)
        {
            return;
        }

        foreach (KeyValuePair<string, object> kvp in _newPropertyValues)
        {
            await ViewJsModule!.InvokeVoidAsync("updateView", kvp.Key, kvp.Value);
        }

        _newPropertyValues.Clear();
    }

    public override async Task RegisterChildComponent(MapComponent child)
    {
        switch (child)
        {
            case Map m:
                if (!m.Equals(Map))
                {
                    Map = m;
                    await UpdateComponent();
                }

                break;
            case WebMap webMap:
                if (!webMap.Equals(WebMap))
                {
                    WebMap = webMap;
                    await UpdateComponent();
                }

                break;
            case Widget widget:
                if (!Widgets.Contains(widget))
                {
                    Widgets.Add(widget);

                    if (MapRendered)
                    {
                        await UpdateWidgets();
                    }
                }

                break;
            case Graphic graphic:
                if (!Graphics.Contains(graphic))
                {
                    Graphics.Add(graphic);

                    if (MapRendered)
                    {
                        await AddGraphic(graphic);
                    }
                }

                break;
            case SpatialReference spatialReference:
                if (!spatialReference.Equals(SpatialReference))
                {
                    SpatialReference = spatialReference;
                    await UpdateComponent();
                }

                break;
            case Constraints constraints:
                if (!constraints.Equals(Constraints))
                {
                    Constraints = constraints;
                    await UpdateComponent();
                }

                break;
            default:
                await base.RegisterChildComponent(child);

                break;
        }
    }

    public override async Task UnregisterChildComponent(MapComponent child)
    {
        switch (child)
        {
            case Map _:
                Map = null;
                await UpdateComponent();

                break;
            case WebMap _:
                WebMap = null;
                await UpdateComponent();

                break;
            case Widget widget:
                if (Widgets.Contains(widget))
                {
                    Widgets.Remove(widget);
                    await UpdateWidgets();
                }

                break;
            case Graphic graphic:
                if (Graphics.Contains(graphic))
                {
                    Graphics.Remove(graphic);
                    await RemoveGraphic(graphic, null);
                }

                break;
            case Constraints _:
                Constraints = null;
                await UpdateComponent();

                break;
            default:
                await base.UnregisterChildComponent(child);

                break;
        }
    }

    public async Task QueryFeatures(Query query, FeatureLayer featureLayer, Symbol displaySymbol,
        PopupTemplate popupTemplate)
    {
        await ViewJsModule!.InvokeVoidAsync("queryFeatureLayer", (object)query,
            (object)featureLayer, (object)displaySymbol, (object)popupTemplate);
    }

    public async Task FindPlaces(AddressQuery query, Symbol displaySymbol,
        PopupTemplate popupTemplate)
    {
        await ViewJsModule!.InvokeVoidAsync("findPlaces", (object)query,
            (object)displaySymbol, (object)popupTemplate);
    }

    public async Task ShowPopup(PopupTemplate template, Point location)
    {
        await ViewJsModule!.InvokeVoidAsync("showPopup", (object)template,
            (object)location);
    }

    public async Task ShowPopupWithGraphic(Graphic graphic, PopupOptions options)
    {
        await ViewJsModule!.InvokeVoidAsync("showPopupWithGraphic", (object)graphic, (object)options);
    }

    public async Task<int> AddGraphic(Graphic graphic, int? layerIndex = null)
    {
        return await ViewJsModule!.InvokeAsync<int>("addGraphic", (object)graphic, layerIndex);
    }

    public async Task ClearGraphics()
    {
        await ViewJsModule!.InvokeVoidAsync("clearViewGraphics");
    }

    /// <summary>
    /// </summary>
    /// <param name="routeSymbol"></param>
    /// <param name="routeUrl"></param>
    /// <returns>Returns the directions of the route</returns>
    public async Task<Direction[]> DrawRouteAndGetDirections(Symbol routeSymbol, string routeUrl)
    {
        return await ViewJsModule!.InvokeAsync<Direction[]?>("drawRouteAndGetDirections",
            routeUrl, (object)routeSymbol) ?? Array.Empty<Direction>();
    }

    public async Task SolveServiceArea(string serviceAreaUrl, double[] driveTimeCutOffs, Symbol serviceAreaSymbol)
    {
        await ViewJsModule!.InvokeVoidAsync("solveServiceArea",
            serviceAreaUrl, driveTimeCutOffs, serviceAreaSymbol);
    }

    public async Task<Graphic[]> GetAllGraphics(int? layerIndex)
    {
        return await ViewJsModule!.InvokeAsync<Graphic[]>("getAllGraphics", layerIndex);
    }

    public async Task RemoveGraphic(Graphic graphic, int? layerIndex = 0)
    {
        await ViewJsModule!.InvokeVoidAsync("removeGraphic", (object)graphic, layerIndex);
    }

    public async Task RemoveGraphicAtIndex(int index, int? layerIndex = null)
    {
        await ViewJsModule!.InvokeVoidAsync("removeGraphicAtIndex", index, layerIndex);
    }

    public async Task RemoveGraphicsAtIndexes(int[] indexes, int? layerIndex = null)
    {
        await ViewJsModule!.InvokeVoidAsync("removeGraphicsAtIndexes", indexes, layerIndex);
    }

    public async Task CreateGeodesicBuffer(Geometry geometry, double distance, LinearUnit unit, Graphic bufferGraphic)
    {
        await ViewJsModule!.InvokeVoidAsync("createGeodesicBuffer", (object)geometry, distance, unit,
            (object)bufferGraphic);
    }

    public async Task DrawWithGeodesicBufferOnPointer(Symbol cursorSymbol, Symbol bufferSymbol, double bufferDistance,
        LinearUnit bufferUnit)
    {
        await ViewJsModule!.InvokeVoidAsync("drawWithGeodesicBufferOnPointer", (object)cursorSymbol,
            (object)bufferSymbol, bufferDistance, bufferUnit);
    }

    public async Task<int> UpdateGraphic(Graphic graphic, int? layerIndex)
    {
        return await ViewJsModule!.InvokeAsync<int>("updateGraphic", (object)graphic, layerIndex);
    }

    public async Task<Point> GetCenter()
    {
        return await ViewJsModule!.InvokeAsync<Point>("getCenter");
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        ApiKey = Configuration["ArcGISApiKey"];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        ViewJsModule = await JsRuntime
            .InvokeAsync<IJSObjectReference>("import",
                "./_content/dymaptic.Blazor.GIS.API.Core/arcGisJsInterop.js");

        if (firstRender)
        {
            // the first render never has all the child components registered
            Rendering = false;
            StateHasChanged();

            return;
        }

        if (NeedsRender)
        {
            await RenderView();
        }
        else if (_newPropertyValues.Any())
        {
            await UpdateComponent();
        }
    }

    protected override async Task RenderView(bool forceRender = false)
    {
        if (!NeedsRender && !forceRender)
        {
            return;
        }

        if (Rendering || (Map is null && WebMap is null) || ViewJsModule is null ||
            string.IsNullOrWhiteSpace(ApiKey)) return;

        Rendering = true;

        await InvokeAsync(async () =>
        {
            Console.WriteLine("Rendering View");
            string mapType = Map is null ? "webmap" : "map";
            object? map = Map is null ? WebMap : Map;

            if (map is null)
            {
                throw new MissingMapException();
            }

            NeedsRender = false;

            await ViewJsModule!.InvokeVoidAsync("buildMapView",
                DotNetObjectReference, Longitude, Latitude, Rotation, map, Zoom, Scale,
                ApiKey, mapType, Widgets, Graphics, SpatialReference);
            Rendering = false;
            _newPropertyValues.Clear();
            MapRendered = true;
        });
    }

    private async Task UpdateWidgets()
    {
        await InvokeAsync(async () =>
        {
            await ViewJsModule!.InvokeVoidAsync("updateWidgets", Widgets);
        });
    }

    protected IJSObjectReference? ViewJsModule;
    protected bool Rendering;
    private Dictionary<string, object> _newPropertyValues = new();

    protected bool NeedsRender = true;
    private double _longitude = -118.805;
    private double _zoom = 11;
    private double? _scale;
    private double _rotation;
    private double _latitude = 34.027;
    private string? _apiKey;
    private SpatialReference? _spatialReference;
}