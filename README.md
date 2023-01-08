
# EmbodiedFlightSimVR
 Educational-cooperative VR flight simulator. UoA postgraduate dissertation.

### Developer notes
When running on Oculus (not in editor), Mapbox fails to load due to a `NullReferenceException` in `MapboxConfiguration.GetMapsSkuToken()` (file `Assets\Mapbox\Unity\MapboxAccess.cs:342`). A workaround is to modify it to directly return your Mapbox API token (either hardcoded or fetched from another source).
