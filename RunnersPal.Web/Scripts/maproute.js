/**
* Creates a MapPoint which represents the location on the route.
* A MapPoint can identify a user-entered point - that is a point
* the user added by double-clicking the map for example - of an
* 'auto-generated' point - for example a km marker pushpin.
* @param {number} lat: the latitude of the point
* @param {number} lon: the longitude of the point
* @param {string} pointType: a value indicating the type of point:
*   's': starting point pushpin
*   'e': end point pushpin
*   'm': distance marker pushpin (e.g. km markers)
*   'p': a 'regular' point on the route
* @param {string} text: the text to display on the point (optional)
*/
function MapPoint(lat, lon, pointType, text) {
    this._lat = lat;
    this._lon = lon;
    this._pointType = pointType;
    this._text = (typeof text == 'undefined') ? '' : text.toString();
}
/**
* @returns {number} The latitude represented by this point.
*/
MapPoint.prototype.latitude = function () { return this._lat; }
/**
* @returns {number} The longitude represented by this point.
*/
MapPoint.prototype.longitude = function () { return this._lon; }
/**
* @returns {Microsoft.Maps.Location} A Location object initialised to the lat, long of this MapPoint.
*/
MapPoint.prototype.toMapsLocation = function () { return new Microsoft.Maps.Location(this._lat, this._lon); }
/**
* @returns {LatLon} A LatLon object initialised to the lat, long of this MapPoint.
*/
MapPoint.prototype.toLatLon = function () { return new LatLon(this._lat, this._lon); }
/**
* @returns {Microsoft.Maps.Pushpin} The appropriate pushpin for this type of MapPoint (e.g. a green pin
*   for the start of the route). A null is returned if the type does not correspond to a valid pushpin type:
*   e.g. if this is a 'regular' map point.
*/
MapPoint.prototype.createPushpin = function () {
    if (this._pointType == 's') // start
        return new Microsoft.Maps.Pushpin(this.toMapsLocation(), { icon: 'Content/pushpin-green.png', width: 23, height: 36 });
    if (this._pointType == 'e') // end
        return new Microsoft.Maps.Pushpin(this.toMapsLocation(), { icon: 'Content/pushpin-red.png', width: 23, height: 36 });
    if (this._pointType == 'm') // distance marker
        return new Microsoft.Maps.Pushpin(this.toMapsLocation(), { icon: 'Content/pushpin-white.png', width: 23, height: 36, text: this._text, typeName: 'distanceMarker' })
    // this._pointType == 'p' - a point: no pushpin to create
    return null;
}
/**
* @param {string} type: Optional type to restrict the types of pushpins to check.
* @returns {boolean} true if this MapPoint is a pushpin type: e.g. a 'start' pushpin; or if type
*   is specified, will only return true if this MapPoint is of the same type as the argument.
*/
MapPoint.prototype.isPushpin = function (type) {
    if (typeof type == 'undefined') return this._pointType == 's' || this._pointType == 'e' || this._pointType == 'm';
    return this._pointType == type;
}

/**
* Create a new MapRoute which will record the points a given route is map up from.
* @param {Microsoft.Maps.Map} map: The map object.
*/
function MapRoute(map) {
    this._map = map;
    this._points = [];
    this._curDist = 0;
    this._lastDistanceMarker = 0;
    this._distanceMarkerNumber = 0;
    this._distanceMarkerUnits = 1;

    this._map.entities.clear();
}
/**
* This property is the number of km between distance markers (defaults to 1 - i.e. every km).
* Calculations are performed entirely in kilometers, so use this property to space distance
* markers at a custom distance: for example, set this to 1.609344 to space the markers at
* mile points.
* @param {number} units: The distance in km between each marker. (Optional)
* @returns {number} The distance in km between each marker.
*/
MapRoute.prototype.distanceMarkerUnits = function (units) {
    if (typeof units == 'undefined') return this._distanceMarkerUnits;
    this._distanceMarkerUnits = units;
    return units;
}
/**
* Calculates the distance from 'from' to 'to' in kilometers.
* @param {MapPoint} from: The starting point.
* @param {MapPoint} to: The end point.
* @returns {number} The distance in km between the two given points.
*/
MapRoute.prototype.distanceBetween = function (from, to) {
    return parseFloat(from.toLatLon().distanceTo(to.toLatLon()));
}
/**
* Get the last point. If ignoreDistMarkers is not provided or false,
* the last point is returned; if ignoreDistMarkers is true, any distance
* marker points are skipped over until a point is found.
* @param {boolean} ignoreDistMarkers: If set to true, the returned last
*   point will skip any distance markers.
* @returns {MapPoint} The last point added to the route.
*/
MapRoute.prototype.lastPoint = function (ignoreDistMarkers) {
    if (typeof ignoreDistMarkers == 'undefined')
        ignoreDistMarkers = false;

    for (var i = this._points.length - 1; i >= 0; i--) {
        if (!ignoreDistMarkers || !this._points[i].isPushpin('m'))
            return this._points[i];
    }
    return null;

}
/**
* Remove the last point that was added to the route and return that point.
* @returns The last point that had been added to the route.
*/
MapRoute.prototype.removeLastPoint = function () {
    var lastPoint = this._points.pop();
    this._map.entities.pop();
    return lastPoint;
}
/**
* Add a point to the route. If this is the first point, a start pushpin is added.
* Depending on the distance, additional distance marker points will be added.
* Finally, if this is not the first point, an end pushpin will be added.
* @param {Microsoft.Maps.Location} location: The location of the point to add.
*/
MapRoute.prototype.addPoint = function (location) {
    if (this._points.length == 0) {
        // add start point...
        var startPoint = new MapPoint(location.latitude, location.longitude, 's');
        this._map.entities.push(startPoint.createPushpin());
        this._points.push(startPoint);
        return;
    }

    if (this.lastPoint().isPushpin('e')) this.removeLastPoint();
    var loc = new MapPoint(location.latitude, location.longitude, 'p');

    var priorDistance = this._curDist;
    this._curDist += this.distanceBetween(this.lastPoint(), loc);
    var priorPoint = this.lastPoint();

    while (this._curDist > this._lastDistanceMarker + this._distanceMarkerUnits) {
        this._lastDistanceMarker += this._distanceMarkerUnits;
        var bearing = this.lastPoint().toLatLon().bearingTo(loc.toLatLon());
        var kmPoint = this.lastPoint().toLatLon().destinationPoint(bearing, this._lastDistanceMarker - priorDistance);
        priorDistance = this._lastDistanceMarker;

        var distMarker = new MapPoint(kmPoint.lat(), kmPoint.lon(), 'm', ++this._distanceMarkerNumber);
        this._map.entities.push(distMarker.createPushpin());
        this._points.push(distMarker);
    }

    this._map.entities.push(new Microsoft.Maps.Polyline([priorPoint.toMapsLocation(), location], null));
    this._points.push(loc);

    loc = new MapPoint(location.latitude, location.longitude, 'e');
    this._map.entities.push(loc.createPushpin());
    this._points.push(loc);
}
/**
* Remove the last 'logical' point. The last point will usually be the
* end pushpin - this method removes the point prior to the auto-added
* end pushpin. If there is only one point, it will be the start pushpin,
* in which case it will be removed, clearing the map.
*/
MapRoute.prototype.undo = function () {
    if (this._points.length <= 1) {
        this.clear();
        return;
    }
    var lastPoint = this.removeLastPoint();
    if (lastPoint.isPushpin('e'))
        lastPoint = this.removeLastPoint();

    if (lastPoint.isPushpin('s')) this._curDist = 0;
    else this._curDist -= Math.abs(this.distanceBetween(lastPoint, this.lastPoint(true)));

    // remove any distance markers
    while (this.lastPoint().isPushpin('m')) {
        this.removeLastPoint();
        this._lastDistanceMarker -= this._distanceMarkerUnits;
        this._distanceMarkerNumber--;
    }

    if (!this.lastPoint().isPushpin('s')) {
        var loc = new MapPoint(this.lastPoint().latitude(), this.lastPoint().longitude(), 'e');
        this._map.entities.push(loc.createPushpin());
        this._points.push(loc);
    }
}
/**
* @returns {number} The total distance of the route, in km.
*/
MapRoute.prototype.totalDistance = function () { return this._curDist; }
/**
* @returns {string} A json representation of the points.
*/
MapRoute.prototype.toJson = function () {
    var json = '[';
    var pts = this.points();
    for (var i = 0; i < pts.length; i++) {
        var ent = pts[i];
        json += '{ "latitude": ' + ent.latitude() + ', "longitude": ' + ent.longitude() + ' }';
        if (i + 1 < pts.length) json += ',';
        json += '\n';
    }
    json += '\n]';
    return json;
}
/**
* Clear the route.
*/
MapRoute.prototype.clear = function () {
    this._points = [];
    this._curDist = 0;
    this._lastDistanceMarker = 0;
    this._distanceMarkerNumber = 0;
    this._map.entities.clear();
}
/**
* @returns {MapPoint[]} The array of points for the current route. Note: the points
*   for the distance markers and other pushpins are not included in the returned array.
*/
MapRoute.prototype.points = function () {
    var pts = [];
    for (var i = 0; i < this._points.length; i++) {
        if (this._points[i].isPushpin('f') || this._points[i].isPushpin('m'))
            continue;
        pts.push(this._points[i]);
    }
    return pts;
}
/**
* @returns {int} The number of points for the current route, including start, end
* and any distance marker pins.
*/
MapRoute.prototype.pointCount = function () {
    return this._points.length;
}