var kmUnitMultiplier = function() { return unitsModel.currentSingularUnitsName == 'mile' ? 1 / 1.609344 : 1; };

function MyRouteModel(owner, id, name, distance, lastRunOn, notes, lastRunBy) {
    var self = this;
    self._owner = owner;
    self.routeId = ko.observable(id);
    self.routeName = ko.observable(name);
    self.routeNotes = ko.observable(notes);
    self.distance = ko.observable(distance);
    self.distanceUnits = ko.observable(unitsModel.currentUnitsName);
    self.lastRunOn = ko.observable(lastRunOn);
    self.lastRunBy = ko.observable(lastRunBy);

    self.showLastRun = ko.computed(function() { return self.lastRunOn() != null && self.lastRunOn() != ""; }, this);
    self.lastRunText = ko.computed(function() {
        if (!self.showLastRun()) return "";
        var lastRunTxt = "Last run on " + self.lastRunOn();
        if (self.lastRunBy() != null && self.lastRunBy() != "")
            lastRunTxt += " by " + self.lastRunBy();
        lastRunTxt += ".";
        return lastRunTxt;
    }, this);
}
MyRouteModel.prototype.chooseRoute = function() {
    // make this the selection
    this._owner.route(this.routeId());
    this._owner.distance(this.routeName() + ", " + this.distance() + " " + this.distanceUnits());
    $('#add-run-time').focus().select();
}
MyRouteModel.prototype.loadRoute = function() {
    var self = this;
    self.reload(function(result) {
        $("#routeManagementOptions").accordion({ active: 2 });
        self._owner.reset(result);
    });
}
MyRouteModel.prototype.reload = function(callback) {
    var self = this;
    $.post(Models.urls.loadRoute, { id: self.routeId() },
        function (result) {
            if (!result.Completed) {
                alert('There was a problem loading your route.\n\nMore details:\n' + result.Reason);
                return;
            }
            callback(result.Route);
        }
    );
}

function RouteMapping(model) {
    var self = this;
    self._route = null;
    self._model = model;
    self.routeName = ko.observable("");
    self.routeNotes = ko.observable("");
    self.routePublic = ko.observable(false);
    self.routeModified = ko.observable(false);
    self.routePointCount = ko.observable(0);
    self.distance = ko.observable(0.00);
    self.distanceDisplay = ko.computed(function() { return self.distance() == 0 ? "0" : self.distance().toFixed(4); }, self);
    self.distanceUnits = ko.observable(unitsModel.currentUnitsName);
    self.allowUndo = ko.computed(function() { return self.distanceDisplay() != "0" || (self.routePointCount() > 0); }, self);
    self.routeChange = ko.computed(function() {
        self.updateParent(self._model, self.distance(), self.routeName());
    });
}
RouteMapping.prototype.addRoutePoint = function(p) {
    if (this._route == null) return;
    this._route.addPoint(p);
    this.routePointCount(this._route.pointCount());
    this.routeModified(true);
    this.distance(this._route.totalDistance() * kmUnitMultiplier());
}
RouteMapping.prototype.undoLastPoint = function() {
    if (this._route == null) return;
    this._route.undo();
    this.routePointCount(this._route.pointCount());
    this.routeModified(true);
    this.distance(this._route.totalDistance() * kmUnitMultiplier());
}
RouteMapping.prototype.pointsToJson = function() {
    if (this._route == null) return "[]";
    return this._route.toJson();
}
RouteMapping.prototype.reset = function() {
    this.routeName('');
    this.routeNotes('');
    this.routePublic(false);

    this.distance(0.00);

    if (this._route != null) {
        this._route.clear();
        this.routePointCount(this._route.pointCount());
    }
    this.routeModified(false);
}
RouteMapping.prototype.updateParent = function(model, distance, routeName) {
    if (typeof(model) == "undefined")
        model = this._model;
    if (typeof(distance) == "undefined")
        distance = this.distance();
    if (typeof(routeName) == "undefined")
        routeName = this.routeName();

    if (model == null) return;

    if (distance > 0 && routeName != "") {
        model.distance(distance.toFixed(4));
        model.route(-2);
    } else {
        model.route(0);
    }
    model.routeType('');
}
RouteMapping.prototype.redrawRoute = function() {
	this.distance(0.00);
	if (this._route != null) {
	    var points = this._route.points();
	    this._route.distanceMarkerUnits(1 / kmUnitMultiplier());
	    this._route.clear();
	    for (var i = 0; i < points.length; i++)
	        this.addRoutePoint(points[i].toMapsLocation());
	}
}

function AddRunModel() {
    var self = this;
    this.runLogId = ko.observable(-1);
    this.eventDate = ko.observable();
    this.runDate = ko.computed(function () { return self.eventDate() ? self.eventDate().format("dddd, Do MMMM YYYY") : ""; });
    this.route = ko.observable(0);
    this.routeType = ko.observable("");
    this.distance = ko.observable("0");
    this.distanceDescription = ko.computed(function () {
        if (this.route() < 0)
            return this.distance() + " " + (this.distance() != 1 ? this.distanceUnits() : this.distanceUnitsSingular());
        if (this.route() > 0)
            return this.distance();
        return "Nothing selected - please choose a distance from below";
    }, this);
    this.distanceUnits = ko.observable(unitsModel.currentUnitsName);
    this.distanceUnitsSingular = ko.observable(unitsModel.currentSingularUnitsName);
    this.time = ko.observable("");
    this.pace = ko.observable("0");
    this.paceCalc = ko.computed(function () {
        $.post(Models.urls.calcPace, { route: this.route(), distance: this.distance(), time: this.time(), calc: 'pace' },
            function (result) {
                self.pace(result.Pace);
            }
        );
    }, this).extend({ throttle: 200 });
    this.calories = ko.observable("0");
    this.caloriesCalc = ko.computed(function () {
        if (!this.eventDate()) return;
        $.post(Models.urls.autoCalcCalories, { date: this.eventDate().toDate().toUTCString(), route: this.route(), distance: this.distance() },
            function (result) {
                if (!result.Result) return;
                self.calories(result.Calories);
            }
        );
    }, this).extend({ throttle: 200 });
    this.comment = ko.observable("");
    this.beginEdit = false;
    this.showAdd = ko.observable(true);
    this.showEdit = ko.observable(false);
    this.showDelete = ko.observable(false);
    this.showViewRoute = ko.computed(function() { return this.route() > 0 && this.routeType() != 'common'; }, this);
    this.myRoutes = ko.observableArray([]);
    this.foundRoutes = ko.observableArray([]);
    this.foundRouteText = ko.observable("");
    this.mapping = new RouteMapping(this);
}
AddRunModel.prototype.applyOpenRouteBtn = function () {
    $('.openRoute').button({ icons: { primary: "ui-icon-document" }, text: false });
}
AddRunModel.prototype.fetchMyRoutes = function () {
    var self = this;
    $.post(Models.urls.myRoutes,
        function (result) {
            if (!result.Completed) {
                return;
            }
            self.myRoutes.removeAll();
            for (var i = 0; i < result.Routes.length; i++)
                self.myRoutes.push(new MyRouteModel(self, result.Routes[i].Id, result.Routes[i].Name, result.Routes[i].Distance, result.Routes[i].LastRun, result.Routes[i].Notes, result.Routes[i].LastRunBy));
        }
    );
}
AddRunModel.prototype.addRun = function (calendar, addRunDialog) {
    var self = this;
    var eventDate = self.eventDate();

    // format the date as: ddd, d MMM yyyy HH:mm:ss UTC
    var formattedEventDate = eventDate.format("ddd, D MMM YYYY") + ' 00:00:00 UTC';

    var routeId = self.route();
    $.post(Models.urls.addRun, { date: formattedEventDate, distance: self.distance(),
        route: routeId, time: self.time(), comment: self.comment(),
        newRouteName: self.mapping.routeName(), newRouteNotes: self.mapping.routeNotes(),
        newRoutePublic: self.mapping.routePublic(), newRoutePoints: self.mapping.pointsToJson()
        },
        function (result) {
            if (!result.Completed) {
                alert('Could not add event.\n\n' + result.Reason);
                return;
            }

            addRunDialog.slideUp('fast');
            calendar.fullCalendar('refetchEvents');
            if (routeId < 0)
                self.fetchMyRoutes();
        });
}
AddRunModel.prototype.updateRun = function (calendar, addRunDialog) {
    var self = this;
    var eventDate = self.eventDate();
    $.post(Models.urls.updateRun, { runLogId: self.runLogId(), date: eventDate.toDate().toUTCString(), distance: self.distance(), route: self.route(), time: self.time(), comment: self.comment() },
        function (result) {
            if (!result.Completed) {
                alert('Could not update this event\n\n' + result.Reason);
                return;
            }

            addRunDialog.slideUp('fast');
            calendar.fullCalendar('refetchEvents');
        });
}
AddRunModel.prototype.deleteRun = function (calendar, addRunDialog) {
    var self = this;
    $.post(Models.urls.deleteRun, { runLogId: self.runLogId() },
        function (result) {
            if (!result.Completed) {
                alert('Could not delete this event.\n\n' + result.Reason);
                return;
            }

            addRunDialog.slideUp('fast');
            calendar.fullCalendar('refetchEvents');
        });
}
AddRunModel.prototype.updateFromUnitsModel = function () {
    var self = this;
    self.distanceUnits(unitsModel.currentUnitsName);
    self.distanceUnitsSingular(unitsModel.currentSingularUnitsName);

    if (self.route() < 0)
        $.post(Models.urls.calcDist, { distanceKm: self.distance(), distanceM: self.distance(), calc: unitsModel.currentUnitsName },
            function (result) {
                var newDistance = unitsModel.isCurrentUnitsMiles() ? result.DistanceM : result.DistanceKm;
                if (newDistance == null) return;
                self.distance(newDistance.toFixed(4));
            }
        );

    if (self.route() > 0)
        self.distance(self.distance() + " ");
            
    self.fetchMyRoutes();
    self.mapping.distanceUnits(unitsModel.currentUnitsName);
    self.mapping.redrawRoute();
}
AddRunModel.prototype.createDistanceSelection = function (accordianEl, commonRoutesEl) {
    var self = this;
    accordianEl.accordion({
        autoHeight: false,
        navigation: true,
        heightStyle: "content"
    }).bind('accordionchange', function (event, ui) {
        if (self.beginEdit) {
            self.beginEdit = false;
            return;
        }

        // if enter manual distance, auto focus text box
        if (ui.newHeader.attr('id') == "enterManualDistance") {
            self.distance("0");
            self.route(-1);
            self.routeType("");
            ui.newContent.find('input').focus().select();
        } else if (ui.newHeader.attr('id') == "tabMapNewRoute") {
            self.route(-2);
            self.routeType("");
            self.distance("0");
            self.mapping.updateParent();
        } else {
            self.route(0);
            self.routeType("");
            if (ui.newHeader.attr('id') == "tabFindARoute")
                $('#q').focus().select();
        }
    });
    commonRoutesEl
            .text(function () { return $(this).attr('data-distancedesc'); })
            .click(function () {
                self.route($(this).attr('data-route'));
                self.routeType("common");
                self.distance($(this).attr('data-distancedesc'));
                $('#add-run-time').focus().select();
            });
}
AddRunModel.prototype.startNew = function () {
    if (this.mapping.allowSave() || this.mapping.routePointCount() > 0) {
        if (!window.confirm('This will clear the current route - are you sure you want to continue?')) return;
    }
    this.mapping.reset();
}
AddRunModel.prototype.undo = function () {
    this.mapping.undoLastPoint();
}
AddRunModel.prototype.find = function (findEl) {
    var self = this;
    var query = $(findEl).val();
    $.post(Models.urls.find, { q: query },
                function (result) {
                    if (!result.Completed) {
                        self.foundRouteText('Could not search for routes: ' + result.Reason);
                        return;
                    }
                    self.foundRoutes.removeAll();
                    for (var i = 0; i < result.Routes.length; i++)
                        self.foundRoutes.push(new MyRouteModel(self, result.Routes[i].Id, result.Routes[i].Name, result.Routes[i].Distance, result.Routes[i].LastRun, result.Routes[i].Notes, result.Routes[i].LastRunBy));
                    if (self.foundRoutes().length == 0)
                        self.foundRouteText('No routes found matching your search string. Try modifying your search and try again.');
                }
            );
    return false;
}
AddRunModel.prototype.createMap = function (mapEl) {
    var self = this;
    if (typeof (Microsoft) != "undefined") {
        var map = new Microsoft.Maps.Map(mapEl[0], { credentials: 'AtLqRCQQxDJwOrx97DYR_g9vQn2jgCO6doHIgnpNK13kHPzjLPigtEPjNDzv4Uuh' });
        Microsoft.Maps.Events.addHandler(map, 'click', function (e) {
            self.mapping.addRoutePoint(map.tryPixelToLocation(new Microsoft.Maps.Point(e.getX(), e.getY())));
        });
        map.setView({ zoom: 5, center: new Microsoft.Maps.Location(55, 0) });

        if (typeof (navigator) != "undefined" && typeof (navigator.geolocation) != "undefined" && typeof (navigator.geolocation.getCurrentPosition) == "function") {
            navigator.geolocation.getCurrentPosition(
                  function (pos) {
                      map.setView({ zoom: 10, center: new Microsoft.Maps.Location(pos.coords.latitude, pos.coords.longitude) });
                  },
                  function () { }
                );
        }

        var theRoute = new MapRoute(map);
        theRoute.distanceMarkerUnits(1 / kmUnitMultiplier());
        self.mapping._route = theRoute;
    } else {
        self.mapping._route = new function () {
            this._points = [];
            this.toJson = function () { return '[{ "latitude": 51.51106837017983, "longitude": -0.09557971954345268 },{ "latitude": 51.51104166123068, "longitude": -0.09682426452636283 },{ "latitude": 51.51204166123068, "longitude": -0.09662426452636283 }]'; };
            this.clear = function () { this._points = []; };
            this.addPoint = function (p) { this._points.push(p); };
            this.pointCount = function () { return this._points.length; };
            this.totalDistance = function () { return this.pointCount() * 2; };
            this.distanceMarkerUnits = function (u) { return 1; };
            this.points = function () { return this._points; };
        };
        $('#mapDiv').html('<p id="fakeMap">Maps not available.</p>');
        $('#fakeMap').click(function () {
            self.mapping.routeModified(true);
            self.mapping.distance(7.7);
        });
    }
}
AddRunModel.createLoginPromptDialog = function (elementId) {
    return $(elementId).dialog({
        height: 280,
        width: 320,
        modal: true,
        buttons: { OK: function () { $(this).dialog("close"); } },
        autoOpen: false
    });
}
AddRunModel.createCalendar = function (elementId, addRunModel, loginPromptDialog, addRunDialog) {
    return $(elementId).fullCalendar({
        header: {
            left: 'today',
            center: 'title',
            right: 'prev,next'
        },
        firstDay: 1,
        selectable: true,
        selectHelper: false,
        displayEventTime: false,
        dayClick: function (start, evt, view) {
            if (!loginAccountModel.isLoggedIn) {
                loginPromptDialog.dialog('open');
                loginPromptDialog.bind('dialogclose', function () {
                    loginAccountModel.showLoginDialog();
                    loginAccountModel.returnPage = Models.urls.runLog + start.format("YYYY-MM-DD");
                    loginPromptDialog.unbind('dialogclose');
                });
            } else {
                addRunModel.runLogId(-1);
                addRunModel.eventDate(start);
                addRunModel.distance("0");
                addRunModel.pace("0");
                addRunModel.time("");
                addRunModel.route(0);
                addRunModel.routeType("");
                addRunModel.comment("");
                addRunModel.calories("0");
                addRunModel.showAdd(true);
                addRunModel.showEdit(false);
                addRunModel.showDelete(false);
                $(window).scrollTop(0);
                addRunDialog.slideDown(function () { $("#distanceSelection").accordion('option', 'active', 0); });
            }
            $(this).fullCalendar('unselect');
        },
        eventClick: function (evt, jsEvt, view) {
            $.post(Models.urls.viewRunLog, { runlogid: evt.id },
                        function (result) {
                            if (!result.Completed) {
                                alert('Could not open event.\n\n' + result.Reason);
                                return;
                            }

                            addRunDialog.slideDown(function () {
                                var currentAccordionTab = $("#distanceSelection").accordion('option', 'active');
                                var newAccordionTab = result.routeType == 'common' ? 0 : (result.route == -1 ? 4 : 1);
                                if (currentAccordionTab != newAccordionTab) {
                                    addRunModel.beginEdit = true;
                                    $("#distanceSelection").accordion('option', 'active', newAccordionTab);
                                }
                                $(window).scrollTop(0);
                            });
                            addRunModel.runLogId(result.id);
                            addRunModel.eventDate(moment(new Date(result.date)));
                            addRunModel.distance(result.distance);
                            addRunModel.pace(result.pace);
                            addRunModel.time(result.time);
                            addRunModel.route(result.route);
                            addRunModel.routeType(result.routeType);
                            addRunModel.comment(result.comment);
                            addRunModel.showAdd(false);
                            addRunModel.showEdit(true);
                            addRunModel.showDelete(true);
                        });
        },
        editable: false,
        events: Models.urls.runLogEvents
    });
}
AddRunModel.checkAddEvent = function (loginAccountModel, calendar) {
    var hashItem = window.location.hash;
    if (hashItem.indexOf("#addEvent=") == 0) {
        if (!loginAccountModel.loginError) {
            var eventDate = moment(new Date(parseInt(hashItem.substring(10, 14)), parseInt(hashItem.substring(15, 17), 10) - 1, parseInt(hashItem.substring(18, 20), 10)));
            calendar.fullCalendar('select', eventDate, eventDate);
        } else {
            loginAccountModel.returnPage = Models.urls.runLogBase + hashItem;
        }
    }
}

function RouteDisplayModel() {
    this._route = null;
    this.routeId = ko.observable(0);
    this.routeName = ko.observable("");
    this.originalRouteName = ko.observable("");
    this.routeNotes = ko.observable("");
    this.originalRouteNotes = ko.observable("");
    this.routePublic = ko.observable(false);
    this.originalRoutePublic = ko.observable(false);
    this.routeModified = ko.observable(false);
    this.routePointCount = ko.observable(0);
    this.routePublicOther = ko.observable(false);
    this.routeDeleted = ko.observable(false);
    this.allowSave = ko.computed(function() {
        if (this.routeName() == "" || this.routeDeleted()) return false;
        if (this.route != null && this.routePointCount() == 0) return false;
        if (this.routeModified()) return true;
        if (this.routeId() > 0) {
            if (this.routeName() != this.originalRouteName()) return true;
            if (this.routeNotes() != this.originalRouteNotes()) return true;
            if (this.routePublic() != this.originalRoutePublic()) return true;
        }
        return false;
    }, this);
    this.allowDelete = ko.computed(function() {
        return this.routeId() > 0 && !this.routePublicOther() && !this.routeDeleted();
    }, this);
    this.distance = ko.observable(0.00);
    this.distanceDisplay = ko.computed(function() { return this.distance() == 0 ? "0" : this.distance().toFixed(4); }, this);
    this.distanceUnits = ko.observable(unitsModel.currentUnitsName);
    this.allowUndo = ko.computed(function() { return this.distanceDisplay() != "0" || (this.routePointCount() > 0); }, this);

    this.myRoutes = ko.observableArray([]);
    this.foundRoutes = ko.observableArray([]);
    this.foundRouteText = ko.observable("");
}
RouteDisplayModel.getParameterByName = function(name) {
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regexS = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexS);
    var results = regex.exec(window.location.search);
    if (results == null)
        return "";
    else
        return decodeURIComponent(results[1].replace(/\+/g, " "));
}
RouteDisplayModel.prototype.addRoutePoint = function(p) {
    if (this._route == null) return;
    this._route.addPoint(p);
    this.routePointCount(this._route.pointCount());
    this.routeModified(true);
    this.distance(this._route.totalDistance() * kmUnitMultiplier());
}
RouteDisplayModel.prototype.undoLastPoint = function() {
    if (this._route == null) return;
    this._route.undo();
    this.routePointCount(this._route.pointCount());
    this.routeModified(true);
    this.distance(this._route.totalDistance() * kmUnitMultiplier());
}
RouteDisplayModel.prototype.pointsToJson = function() {
    if (this._route == null) return "[]";
    return this._route.toJson();
}
RouteDisplayModel.prototype.reset = function(json) {
    if (typeof(json) == "undefined" || json == null)
        json = { Id: 0, Name: "", Notes: "", Public: false, Points: [] };
            
    this.routeId(json.Id);
    this.routeName(json.Name);
    this.originalRouteName(json.Name);
    this.routeNotes(json.Notes);
    this.originalRouteNotes(json.Notes);
    this.routePublic(json.Public);
    this.originalRoutePublic(json.Public);
    this.routePublicOther(json.PublicOther);
    this.routeDeleted(json.Deleted);
    this.distance(0.00);

    if (this._route != null) {
        this._route.clear();
        this.routePointCount(this._route.pointCount());
        if (typeof(json.Points) == "string") {
            var mapPoints = $.parseJSON(json.Points);
            for (var i = 0; i < mapPoints.length; i++)
                this.addRoutePoint(mapPoints[i]);
            if (mapPoints.length > 0)
                this._route.setView(mapPoints[0], 14);
        }
    }
    this.routeModified(false);
}
RouteDisplayModel.prototype.redrawRoute = function() {
    this.distance(0.00);
    if (this._route != null) {
        var points = this._route.points();
        this._route.distanceMarkerUnits(1 / kmUnitMultiplier());
        this._route.clear();
        for (var i = 0; i < points.length; i++)
            this.addRoutePoint(points[i].toMapsLocation());
    }
}
RouteDisplayModel.prototype.refreshMyRoute = function(json) {
    var myRoute = this.currentRoute();
    if (myRoute == null) return;
    myRoute.routeId(json.Id);
    myRoute.routeName(json.Name);
    myRoute.routeNotes(json.Notes);
    myRoute.lastRunOn(json.LastRun);
    myRoute.lastRunBy("");
    myRoute.distance(json.Distance.toFixed(2));
}
RouteDisplayModel.prototype.currentRoute = function() {
    if (this.routeId() < 1) return null;
    for (var i = 0; i < this.myRoutes().length; i++) {
        var route = this.myRoutes()[i];
        if (route.routeId() == this.routeId()) return route;
    }
    for (var i = 0; i < this.foundRoutes().length; i++) {
        var route = this.foundRoutes()[i];
        if (route.routeId() == this.routeId()) return route;
    }
    return null;
}
RouteDisplayModel.prototype.createMap = function (mapDiv) {
    var self = this;
    if (typeof (Microsoft) != "undefined") {
        var map = new Microsoft.Maps.Map($(mapDiv)[0], { credentials: 'AtLqRCQQxDJwOrx97DYR_g9vQn2jgCO6doHIgnpNK13kHPzjLPigtEPjNDzv4Uuh' });
        Microsoft.Maps.Events.addHandler(map, 'click', function (e) {
            self.addRoutePoint(map.tryPixelToLocation(new Microsoft.Maps.Point(e.getX(), e.getY())));
        });
        map.setView({ zoom: 5, center: new Microsoft.Maps.Location(55, 0) });

        if (typeof (navigator) != "undefined" && typeof (navigator.geolocation) != "undefined" && typeof (navigator.geolocation.getCurrentPosition) == "function") {
            navigator.geolocation.getCurrentPosition(
                  function (pos) {
                      map.setView({ zoom: 10, center: new Microsoft.Maps.Location(pos.coords.latitude, pos.coords.longitude) });
                  },
                  function () { }
                );
        }

        var theRoute = new MapRoute(map);
        theRoute.distanceMarkerUnits(1 / kmUnitMultiplier());
        self._route = theRoute;
    } else {
        self._route = new function () {
            this._points = [];
            this.toJson = function () { return '[{ "latitude": 51.51106837017983, "longitude": -0.09557971954345268 },{ "latitude": 51.51104166123068, "longitude": -0.09682426452636283 },{ "latitude": 51.51204166123068, "longitude": -0.09662426452636283 }]'; };
            this.clear = function () { this._points = []; };
            this.addPoint = function (p) { this._points.push(p); };
            this.pointCount = function () { return this._points.length; };
            this.totalDistance = function () { return this.pointCount() * 2; };
            this.distanceMarkerUnits = function (u) { return 1; };
            this.points = function () { return this._points; };
            this.setView = function () { };
            this.undo = function () { };
        };
        $(mapDiv).html('<p id="fakeMap">Maps not available.</p>');
        $('#fakeMap').click(function () {
            self.routeModified(true);
            self.distance(7.7);
        });
    }
}
RouteDisplayModel.prototype.newRoute = function () {
    if (this.allowSave() || this.routePointCount() > 0) {
        if (!window.confirm('This will clear the current route - are you sure you want to continue?')) return;
    }
    this.reset();
}
RouteDisplayModel.prototype.resetRoute = function () {
    var self = this;
    if (self.allowSave()) {
        if (!window.confirm('This will undo all the changes you\'ve made to the current route - are you sure you want to continue?')) return;
    }
    var curRoute = self.currentRoute();
    if (curRoute != null) curRoute.reload(function (result) { self.reset(result); });
    else new MyRouteModel(self, self.routeId(), '', 0, '', '', '').loadRoute();
}
RouteDisplayModel.prototype.saveRoute = function (loginPromptDialog) {
    var self = this;
    // if a public route, prompt that a copy will be created
    if (self.routePublicOther()) {
        if (!window.confirm('This is a public route created by someone else. Saving the route will create a copy under your account - do you want to continue?')) return;
    }

    if (!loginAccountModel.isLoggedIn) {
        loginPromptDialog.dialog('open');
        loginPromptDialog.bind('dialogclose', function () {
            $.post(Models.urls.routeBeforeLogin, { id: self.routeId(), name: self.routeName(), notes: self.routeNotes(), public: self.routePublic(), points: self.pointsToJson(), distance: self.distance() },
                            function (result) {
                                if (!result.Completed) {
                                    alert('There was a problem saving the changes to your route.\n\nMore details:\n' + result.Reason);
                                    return;
                                }
                                loginAccountModel.showLoginDialog();
                                loginAccountModel.returnPage = Models.url.routeNew;
                                loginPromptDialog.unbind('dialogclose');
                            });
        });
    } else {
        $.post(Models.urls.routeSave, { id: self.routeId(), name: self.routeName(), notes: self.routeNotes(), public: self.routePublic(), points: self.pointsToJson(), distance: self.distance() },
                        function (result) {
                            if (!result.Completed) {
                                alert('There was a problem saving the changes to your route.\n\nMore details:\n' + result.Reason);
                                return;
                            }
                            var priorRouteId = self.routeId();
                            var priorPublicOther = self.routePublicOther();
                            var isSavedRoute = priorRouteId > 0;
                            self.reset(result.Route);
                            if (isSavedRoute && !priorPublicOther) {
                                if (priorRouteId != self.routeId()) {
                                    for (var i = 0; i < self.myRoutes().length; i++)
                                        if (self.myRoutes()[i].routeId() == priorRouteId)
                                            self.myRoutes()[i].routeId(self.routeId());
                                }
                                self.refreshMyRoute(result.Route);
                            } else {
                                self.myRoutes.push(new MyRouteModel(self, result.Route.Id, result.Route.Name, result.Route.Distance.toFixed(2), result.Route.LastRun, result.Route.Notes, ''));
                            }
                        }
                    );
    }
}
RouteDisplayModel.prototype.deleteRoute = function () {
    if (!window.confirm('This will delete the route - are you sure you want to continue?')) return;

    var self = this;
    $.post(Models.urls.routeDelete, { id: self.routeId() },
                    function (result) {
                        if (!result.Completed) {
                            alert('There was a problem deleting this route.\n\nMore details:\n' + result.Reason);
                            return;
                        }
                        for (var i = 0; i < self.myRoutes().length; i++)
                            if (self.myRoutes()[i].routeId() == self.routeId())
                                self.myRoutes.remove(self.myRoutes()[i]);
                        self.reset();
                    }
                );
}
RouteDisplayModel.prototype.findRoute = function (query) {
    var self = this;
    $.post(Models.urls.find, { q: query },
                function (result) {
                    if (!result.Completed) {
                        self.foundRouteText('Could not search for routes: ' + result.Reason);
                        return;
                    }
                    self.foundRoutes.removeAll();
                    for (var i = 0; i < result.Routes.length; i++)
                        self.foundRoutes.push(new MyRouteModel(self, result.Routes[i].Id, result.Routes[i].Name, result.Routes[i].Distance, result.Routes[i].LastRun, result.Routes[i].Notes, result.Routes[i].LastRunBy));
                    if (self.foundRoutes().length == 0)
                        self.foundRouteText('No routes found matching your search string. Try modifying your search and try again.');
                }
            );
    return false;
}
RouteDisplayModel.prototype.loadMyRoutes = function (callback) {
    var self = this;
    $.post(Models.urls.myRoutes,
            function (result) {
                if (!result.Completed) {
                    return;
                }
                self.myRoutes.removeAll();
                for (var i = 0; i < result.Routes.length; i++)
                    self.myRoutes.push(new MyRouteModel(self, result.Routes[i].Id, result.Routes[i].Name, result.Routes[i].Distance, result.Routes[i].LastRun, result.Routes[i].Notes, result.Routes[i].LastRunBy));

                if (typeof (callback) == 'function') callback();
            }
        );
}
RouteDisplayModel.prototype.loadRequestedRoute = function () {
    var routeId = parseInt(RouteDisplayModel.getParameterByName('route'));
    if (!isNaN(routeId)) {
        for (var i = 0; i < this.myRoutes().length; i++)
            if (this.myRoutes()[i].routeId() == routeId) {
                this.myRoutes()[i].loadRoute();
                return;
            }
        // not one of my routes, perhaps it's a public route, so load it up:
        new MyRouteModel(this, routeId, '', 0, '', '', '').loadRoute();

        if (routeId == 0 && loginAccountModel.loginError)
            loginAccountModel.returnPage = Models.urls.routeNew;
    }
}
RouteDisplayModel.prototype.updateFromUnitsModel = function () {
    this.distanceUnits(unitsModel.currentUnitsName);
    this.redrawRoute();
    this.loadMyRoutes();
}
