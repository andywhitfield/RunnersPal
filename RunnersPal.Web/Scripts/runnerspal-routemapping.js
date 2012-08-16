Date.prototype.yyyymmdd = function() { 
    var yyyy = this.getFullYear().toString();
    var mm = (this.getMonth()+1).toString();
    var dd  = this.getDate().toString();
    return yyyy + '-' + (mm[1]?mm:"0"+mm[0]) + '-' + (dd[1]?dd:"0"+dd[0]);
};

var kmUnitMultiplier = function() { return unitsModel.currentSingularUnitsName == 'mile' ? 1 / 1.609344 : 1; };

function MyRouteModel(owner, id, name, distance, lastRunOn, notes) {
    var self = this;
    self._owner = owner;
    self.routeId = ko.observable(id);
    self.routeName = ko.observable(name);
    self.routeNotes = ko.observable(notes);
    self.distance = ko.observable(distance);
    self.distanceUnits = ko.observable(unitsModel.currentUnitsName);
    self.lastRunOn = ko.observable(lastRunOn);

    self.showLastRun = ko.computed(function() { return self.lastRunOn() != null && self.lastRunOn() != ""; }, this);
    self.lastRunText = ko.computed(function() {
        if (!self.showLastRun()) return "";
        return "Last run on " + self.lastRunOn() + ".";
    }, this);
}
MyRouteModel.prototype.chooseRoute = function() {
    // make this the selection
    this._owner.route(this.routeId());
    this._owner.distance(this.routeName() + ", " + this.distance() + " " + this.distanceUnits());
    $('#add-run-time').focus().select();
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
    this.runDate = ko.computed(function () { return self.eventDate() ? self.eventDate().toLocaleDateString() : ""; });
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
        $.post(AddRunModel.urls.calcPace, { route: this.route(), distance: this.distance(), time: this.time(), calc: 'pace' },
            function (result) {
                self.pace(result.Pace);
            }
        );
    }, this).extend({ throttle: 200 });
    this.calories = ko.observable("0");
    this.caloriesCalc = ko.computed(function () {
        if (!this.eventDate()) return;
        $.post(AddRunModel.urls.autoCalcCalories, { date: this.eventDate().toUTCString(), route: this.route(), distance: this.distance() },
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
    $.post(AddRunModel.urls.myRoutes,
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
    var routeId = self.route();
    $.post(AddRunModel.urls.addRun, { date: eventDate.toUTCString(), distance: self.distance(),
        route: routeId, time: self.time(), comment: self.comment(),
        self: self.mapping.routeName(), newRouteNotes: self.mapping.routeNotes(),
        self: self.mapping.routePublic(), newRoutePoints: self.mapping.pointsToJson()
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
    $.post(AddRunModel.urls.updateRun, { runLogId: self.runLogId(), date: eventDate.toUTCString(), distance: self.distance(), route: self.route(), time: self.time(), comment: self.comment() },
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
    var eventDate = self.eventDate();
    $.post(AddRunModel.urls.deleteRun, { runLogId: self.runLogId() },
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
        $.post(AddRunModel.urls.calcDist, { distanceKm: self.distance(), distanceM: self.distance(), calc: unitsModel.currentUnitsName },
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
        navigation: true
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
AddRunModel.createLoginPromptDialog = function (elementId) {
    return $(elementId).dialog({
        height: 280,
        width: 400,
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
        select: function (start, end, allDay) {
            if (!loginAccountModel.isLoggedIn) {
                loginPromptDialog.dialog('open');
                loginPromptDialog.bind('dialogclose', function () {
                    loginAccountModel.showLoginDialog();
                    loginAccountModel.returnPage = AddRunModel.urls.runLog + start.yyyymmdd();
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
        eventClick: function (evt) {
            $.post(AddRunModel.urls.viewRunLog, { runlogid: evt.id },
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
                            addRunModel.eventDate(new Date(result.date));
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
        events: AddRunModel.urls.runLogEvents
    });
}
