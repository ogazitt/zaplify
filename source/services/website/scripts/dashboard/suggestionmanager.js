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
    switch (suggestion.FieldName) {
        case FieldNames.Contacts: { this.addContact(suggestion); break; }
        // TODO: how to manage keeping Likes and have dependent sub-suggestions 
        case FieldNames.Likes: { this.chooseSuggestion(suggestion); break; }
        case FieldNames.SuggestedLink: { this.navigateLink(suggestion); break; }

        case FieldNames.FacebookConsent: { this.getFacebookConsent(suggestion); break; }
        case FieldNames.CloudADConsent: { this.getCloudADConsent(suggestion); break; }

        default: { this.chooseSuggestion(suggestion); break; }
    }
}

SuggestionManager.prototype.chooseSuggestion = function (suggestion, callback) {
    var dataModel = this.dataModel;
    dataModel.SelectSuggestion(suggestion, Reasons.Chosen,
        function (selected) {                              // success handler
            // chosen suggestions are removed
            if (callback != null) { callback(); }
        });

    this.ignoreSuggestions(suggestion);
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
}

SuggestionManager.prototype.dislikeSuggestion = function (suggestion, callback) {
    var dataModel = this.dataModel;
    dataModel.SelectSuggestion(suggestion, Reasons.Dislike,
        function (selected) {                              // success handler
            if (callback != null) { callback(); }
        });
}

SuggestionManager.prototype.addContact = function (suggestion) {
    var item = this.dataModel.FindItem(suggestion.EntityID);
    if (item != null) {
        if (item.HasField(FieldNames.Contacts)) {
            var contact = $.parseJSON(suggestion.Value);
        }
        var contactsList = item.GetFieldValue(FieldNames.Contacts,
            function (list) {
                if (list != null && list.IsList) {
                    // TODO: get the UserID correct in the suggestion value
                    contact.UserID = list.UserID;
                    list.InsertItem(contact);
                } 
            });
        if (contactsList != null && contactsList.IsList) {
            // TODO: get the UserID correct in the suggestion value
            contact.UserID = contactsList.UserID;
            contactsList.InsertItem(contact);
        }
    }
    this.chooseSuggestion(suggestion);
}

SuggestionManager.prototype.navigateLink = function (suggestion) {
    this.likeSuggestion(suggestion,
        function () {
            window.open(suggestion.Value);
        });
}

SuggestionManager.prototype.getFacebookConsent = function (suggestion) {
    var msg = 'You may be directed to Facebook to give consent.\r Do you want to continue?';
    if (confirm(msg)) {
        Service.GetFacebookConsent();
    }
}

SuggestionManager.prototype.getCloudADConsent = function (suggestion) {
    var msg = 'You may be directed the Cloud Directory to give consent.\r Do you want to continue?';
    if (confirm(msg)) {
        alert('Not yet implemented!');
        //Service.GetCloudADConsent();
    }
}