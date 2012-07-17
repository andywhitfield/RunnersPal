function CalorieCalcModel(calorieCalcUrl, weightCalcUrl) {
    var _self = this;
    this.distance = ko.observable("");
    this.distanceUnits = ko.observable("");
    this.distanceUnitsSingular = ko.observable("");
    this.weightUnits = ko.observable("");
    this.weightKg = ko.observable(0);
    this.weightLbs = ko.observable(0);
    this.weightSt = ko.observable(0);
    this.weightStLbs = ko.observable(0);
    this.calories = ko.observable("0");
    this.priorWeightUnits = ko.observable("");
    this.computedCalories = ko.computed(function() {
        $.post(calorieCalcUrl,
            { distance: _self.distance(), kg: _self.weightKg(), lbs: _self.weightLbs(), st: _self.weightSt(), stLbs: _self.weightStLbs(), units: _self.weightUnits() },
            function (result) { if (result.Result) _self.calories(result.Calories); });
    }, _self).extend({ throttle: 200 });

    this.weightUnits.subscribe(function () {
        $.post(weightCalcUrl,
            { kg: _self.weightKg(), lbs: _self.weightLbs(), st: _self.weightSt(), stLbs: _self.weightStLbs(), units: _self.priorWeightUnits() },
            function (result) {
                if (result.Result) {
                    if (result.Calc.Kg != null) _self.weightKg(result.Calc.Kg.toFixed(4));
                    if (result.Calc.Lbs != null) _self.weightLbs(result.Calc.Lbs.toFixed(4));
                    if (result.Calc.St != null) _self.weightSt(result.Calc.St);
                    if (result.Calc.StLbs != null) _self.weightStLbs(result.Calc.StLbs.toFixed(4));
                }
                _self.priorWeightUnits(_self.weightUnits());
            });
    });
};

CalorieCalcModel.prototype.distanceHalfMarathon = function () {
    this.distance(this.distanceUnits() == 'miles' ? 13.109375 : 21.097494);
}
CalorieCalcModel.prototype.distanceMarathon = function () {
    this.distance(this.distanceUnits() == 'miles' ? 26.21875 : 42.194988);
};
CalorieCalcModel.prototype.updateDistanceUnits = function (url, unitsModel, isMiles) {
    var _self = this;
    this.distanceUnits(unitsModel.currentUnitsName);
    this.distanceUnitsSingular(unitsModel.currentSingularUnitsName);
    if (this.distance() != "") {
        $.post(url, { distanceKm: _self.distance(), distanceM: _self.distance(), calc: unitsModel.currentUnitsName },
            function (result) {
                var newDistance = isMiles ? result.DistanceM : result.DistanceKm;
                if (newDistance == null) return;
                _self.distance(newDistance.toFixed(4));
            }
        );
    }
};

function DistanceCalcModel(url) {
    this._url = url;
    this.distanceM = ko.observable("");
    this.distanceKm = ko.observable("");
};
DistanceCalcModel.prototype.milesChanged = function () {
    var self = this;
    $.post(this._url, { distanceM: self.distanceM(), calc: 'kilometers' },
        function (result) {
            self.distanceKm(result.DistanceKm == null ? "" : result.DistanceKm.toFixed(4));
        }
    );
};
DistanceCalcModel.prototype.kmChanged = function () {
    var self = this;
    $.post(this._url, { distanceKm: self.distanceKm(), calc: 'miles' },
        function (result) {
            self.distanceM(result.DistanceM == null ? "" : result.DistanceM.toFixed(4));
        }
    );
};