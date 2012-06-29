//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// SuggestionManager.js

// ---------------------------------------------------------
// SuggestionManager control
function SuggestionManager(dataModel) {
    this.dataModel = dataModel;
}

SuggestionManager.prototype.select = function (suggestion) {
    var refresh = false;        // return true to indicate additional suggestions should be retrieved
    switch (suggestion.SuggestionType) {
        case SuggestionTypes.ChooseOne: { refresh = this.chooseSuggestion(suggestion); break; }
        case SuggestionTypes.ChooseOneSubject: { refresh = this.chooseSuggestion(suggestion); break; }
        case SuggestionTypes.ChooseMany: { refresh = this.chooseMany(suggestion); break; }
        case SuggestionTypes.NavigateLink: { refresh = this.navigateLink(suggestion); break; }

        case SuggestionTypes.GetFBConsent: { refresh = this.getFacebookConsent(suggestion); break; }
        case SuggestionTypes.GetGoogleConsent: { refresh = this.getGoogleConsent(suggestion); break; }
        case SuggestionTypes.GetADConsent: { refresh = this.getCloudADConsent(suggestion); break; }

        default: { refresh = this.chooseSuggestion(suggestion); break; }
    }
    return refresh;
}

SuggestionManager.prototype.chooseSuggestion = function (suggestion, callback) {
    var dataModel = this.dataModel;
    dataModel.SelectSuggestion(suggestion, Reasons.Chosen,
        function (selected) {                              // success handler
            // chosen suggestions are removed
            if (callback != null) { callback(); }
        });

    this.ignoreSuggestions(suggestion);
    return true;
}

// ignore all other suggestions in the same group as this one
SuggestionManager.prototype.ignoreSuggestions = function (suggestion) {
    var dataModel = this.dataModel;
    var group = dataModel.Suggestions[suggestion.GroupID];
    if (group != null) {
        for (var id in group.Suggestions) {
            var s = group.Suggestions[id];
            if (s.ID != suggestion.ID) {
                dataModel.SelectSuggestion(s, Reasons.Ignore);
            }
        }
    }
}

SuggestionManager.prototype.likeSuggestion = function (suggestion, callback) {
    this.dataModel.SelectSuggestion(suggestion, Reasons.Like,
    function (selected) {                              // success handler
        // liked suggestions are not removed
        if (callback != null) { callback(); }
    });
    return false;
}

SuggestionManager.prototype.dislikeSuggestion = function (suggestion, callback) {
    var dataModel = this.dataModel;
    dataModel.SelectSuggestion(suggestion, Reasons.Dislike,
        function (selected) {                              // success handler
            if (callback != null) { callback(); }
        });
    return false;
}

SuggestionManager.prototype.navigateLink = function (suggestion) {
    return this.likeSuggestion(suggestion,
        function () {
            window.open(suggestion.Value);
        });
}

SuggestionManager.prototype.chooseMany = function (suggestion) {
    return this.likeSuggestion(suggestion,
    function () {
        // TODO: filter any child suggestions to this parent
    });
}

SuggestionManager.prototype.getFacebookConsent = function (suggestion) {
    var dataModel = this.dataModel;
    var msg = 'You will be redirected to Facebook to allow this application to access your Facebook information. ' +
    'This application will use information about yourself to help setup your user profile. ' +
    'This application will use information about your friends to help manage your Contacts. ' +
    '<br\><br\>Do you want to continue?';
    Control.confirm(msg, "Facebook Consent?", 
        function () { Service.GetFacebookConsent(); });
    return false;
}

SuggestionManager.prototype.getCloudADConsent = function (suggestion) {
    var dataModel = this.dataModel;
    var msg = 'You will be redirected to the Cloud Directory portal. ' +
    'Login with your Office 365 credentials to allow this application to access your directory information. ' +
    'This application will use information about yourself and other users to help manage your Contacts. ' +
    '<br\><br\>Do you want to continue?';
    Control.confirm(msg, "Cloud Directory Consent?", 
        function () { Service.GetCloudADConsent(); });
    return false;
}

SuggestionManager.prototype.getGoogleConsent = function (suggestion) {
    var dataModel = this.dataModel;
    var msg = 'You will be redirected to Google to allow this application to manage your Google Calendar. ' +
    'This application will interact with your calendar to keep your tasks and appointments synchronized. ' +
    '<br\><br\>Do you want to continue?';
    Control.confirm(msg, "Google Calendar Consent?", 
        function () { Service.GetGoogleConsent(); });
    return false;
}