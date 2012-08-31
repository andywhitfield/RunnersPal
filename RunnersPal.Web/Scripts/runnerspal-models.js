function Models() {

};

function LoginAccountModel(logoutUrl) {
    this._initdElements = false;
    this.loginSection = null;
    this.loggedInSection = null;
    this.logoutAccountDialog = null;
    this.logoutSection = null;
    this.loginError = false;
    this.returnPage = '';
    this.openId = '';
    this.isLoggedIn = false;
    this.loginCreateAccountDialog = null;
    this.loginUsingOpenId = null;
    this.logoutUrl = logoutUrl;
};
LoginAccountModel.prototype.initLoginDialog = function () {
    var self = this;
    this.initElements();
    this.hideLoginDialog();
    this.loginSection = $('.login');
    this.loginSection.css('cursor', 'pointer');
    this.loginSection.click(function () { self.showLoginDialog(); });

    var customOpenIdUrl = $("#loginForm input[name='loginWithOpenId']");
    $("#loginForm input[name='loginWithOpenIdGo']").click(function () {
        self.openId = customOpenIdUrl.val();
        self.login();
    });
    $('#loginForm').submit(function () {
        if (self.openId == "") self.openId = customOpenIdUrl.val();
        if (self.returnPage == "") self.returnPage = window.location.pathname;
        var loginForm = $('#loginForm');
        loginForm.find("input[name='openid_identifier']").val(self.openId);
        loginForm.find("input[name='return_page']").val(self.returnPage);
    });

    if (this.isLoggedIn) this.loginSection.hide();
    else this.loggedInSection.hide();
};
LoginAccountModel.prototype.login = function() {
    var loginForm = $('#loginForm');
    loginForm.find("input[name='openid_identifier']").val(this.openId);
    loginForm.submit();
};
LoginAccountModel.prototype.initLogoutDialog = function () {
    this.initElements();
    this.logoutAccountDialog = $('#logoutAccount');
    this.logoutSection = $('.logout');

    this.hideLogoutDialog();

    var self = this;
    this.logoutSection.css('cursor', 'pointer');
    this.logoutSection.click(function () { self.showLogoutDialog(); });

    $('#confirmLogout').click(function () {
        self.hideLogoutDialog();
        self.logout();
    });
};
LoginAccountModel.prototype.logout = function() {
    var self = this;
    $.post(self.logoutUrl, function(logoutData) {
        self.isLoggedIn = false;
        self.loggedInSection.hide();
        self.loginSection.show();
        window.location.reload();
    });
};
LoginAccountModel.prototype.showLoginDialog = function() {
    $(window).scrollTop(0);
    var loginLogoutSection = $('.loginLogout');
    loginLogoutSection.hide();

    if (this.loginCreateAccountDialog == null) this.initElements();

    this.loginUsingOpenId.hide();
    this.loginCreateAccountDialog.show();
    this.loginCreateAccountDialog.children().first().show();
};
LoginAccountModel.prototype.hideLoginDialog = function() {
    if (this.loginCreateAccountDialog == null) this.initElements();
    this.loginCreateAccountDialog.hide();
    var loginLogoutSection = $('.loginLogout');
    loginLogoutSection.show();
};
LoginAccountModel.prototype.showLogoutDialog = function() {
    $(window).scrollTop(0);
    this.logoutSection.hide();
    this.logoutAccountDialog.show();
};
LoginAccountModel.prototype.hideLogoutDialog = function() {
    this.logoutSection.show();
    this.logoutAccountDialog.hide();
};
LoginAccountModel.prototype.initElements = function () {
    if (this._initdElements) return;
    this.loggedInSection = $('.loggedIn');
    this.loginCreateAccountDialog = $('#loginCreateAccount');
    this.loginUsingOpenId = this.loginCreateAccountDialog.find("#loginCustomOpenId");

    var self = this;
    this.loginCreateAccountDialog.find("input[name='loginOther']").click(function () {
        self.loginUsingOpenId.show();
        self.loginCreateAccountDialog.children().first().hide();
    });
    this.loginCreateAccountDialog.find("input[data-url]").click(function () {
        self.openId = $(this).attr('data-url');
        self.login();
    });

    $('.loginCancel').click(function () {
        self.hideLoginDialog();
        self.hideLogoutDialog();
    });

    var loginHelpDlg = $("#loginHelpDialog").dialog({
        height: 350,
        width: 320,
        modal: true,
        buttons: { OK: function () { $(this).dialog("close"); } },
        autoOpen: false
    });
    $('.loginHelp').click(function () {
        loginHelpDlg.dialog('open');
    });

    var loginErrorDialog = $('#loginErrorDialog').dialog({
        height: 350,
        width: 320,
        modal: true,
        buttons: { OK: function () { $(this).dialog("close"); self.showLoginDialog(); } },
        autoOpen: false
    });

    if (this.loginError) {
        loginErrorDialog.dialog('open');
    }

    this._initdElements = true;
};
LoginAccountModel.prototype.initDialogs = function () {
    this.initLoginDialog();
    this.initLogoutDialog();
};

function UnitsModel(onChangeUrl, unitsName, singularUnitsName, jqEl) {
    var self = this;
    self._changeUrl = onChangeUrl;
    self._radioElements = jqEl;
    self.callbacks = [];
    self.currentUnitsName = unitsName;
    self.currentSingularUnitsName = singularUnitsName;
    self.milesId = 0;
    self.milesName = '';
    self.milesSingular = '';
    self.kmId = 0;
    self.kmName = '';
    self.kmSingular = '';
    self.isCurrentUnitsMiles = function () { return self.currentUnitsName == self.milesName; }
    self.isCurrentUnitsKm = function () { return self.currentUnitsName == self.kmName; }
    self.updateUnits = function () {
        var newUnit = self._radioElements.filter("input:checked").val();
        if (self.unitsNameFor(newUnit) == self.currentUnitsName) return;
        $.post(self._changeUrl, { distanceUnit: newUnit }, function () {
            self.currentUnitsName = self.unitsNameFor(newUnit);
            self.currentSingularUnitsName = self.singularUnitsNameFor(newUnit);
            jQuery.each(self.callbacks, function (i, c) { if (jQuery.isFunction(c)) c(newUnit); });
        });
    };
    self._radioElements.click(self.updateUnits);
    self.change = function (callback) {
        self.callbacks.push(callback);
    };
    self.unitsNameFor = function (unitId) {
        if (unitId == self.milesId)
            return self.milesName;
        if (unitId == self.kmId)
            return self.kmName;
        return '';
    };
    self.singularUnitsNameFor = function (unitId) {
        if (unitId == self.milesId)
            return self.milesSingular;
        if (unitId == this.kmId)
            return self.kmSingular;
        return '';
    };
    self.toggle = function () {
        self._radioElements.filter("input:not(:checked)").attr("checked", "checked");
        self.updateUnits();
    };
};

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
    this.computedCalories = ko.computed(function () {
        if (calorieCalcUrl == "") return;
        $.post(calorieCalcUrl,
            { distance: _self.distance(), kg: _self.weightKg(), lbs: _self.weightLbs(), st: _self.weightSt(), stLbs: _self.weightStLbs(), units: _self.weightUnits() },
            function (result) { if (result.Result) _self.calories(result.Calories); });
    }, _self).extend({ throttle: 200 });

    this.weightUnits.subscribe(function () {
        if (weightCalcUrl == "") return;
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
};
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

function PaceCalcModel(url) {
    this._url = url;
    this.distance = ko.observable("");
    this.distanceUnits = ko.observable("");
    this.distanceUnitsSingular = ko.observable("");
    this.time = ko.observable("");
    this.pace = ko.observable("");
};
PaceCalcModel.prototype.calculate = function(field) {
    var self = this;
    $.post(self._url,
               { distance: self.distance(), time: self.time(), pace: self.pace(), calc: field },
               function (result) {
                   self.distance(result.Distance == null ? "" : result.Distance.BaseDistance.toFixed(4));
                   self.time(result.Time == null ? "" : result.Time);
                   self.pace(result.Pace == null ? "" : result.Pace);
               });
};
PaceCalcModel.prototype.distanceHalfMarathon = function () {
    this.distance(this.distanceUnits() == 'miles' ? 13.109375 : 21.097494);
}
PaceCalcModel.prototype.distanceMarathon = function () {
    this.distance(this.distanceUnits() == 'miles' ? 26.21875 : 42.194988);
};
PaceCalcModel.prototype.updateDistanceUnits = function (url, unitsModel, isMiles) {
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
    if (this.pace() != "") {
        $.post(_self._url, { pace: _self.pace(), calc: 'pace' + unitsModel.currentUnitsName + 'units' },
                function (result) {
                    if (result.Pace != null) _self.pace(result.Pace);
                });
    }
};
