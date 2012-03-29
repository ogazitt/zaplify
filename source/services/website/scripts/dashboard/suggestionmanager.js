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
    switch (suggestion.FieldName) {
        case FieldNames.Contacts: { refresh = this.addContact(suggestion); break; }
            // TODO: how to manage keeping Likes and have dependent sub-suggestions 
        case FieldNames.Likes: { refresh = this.chooseSuggestion(suggestion); break; }
        case FieldNames.SuggestedLink: { refresh = this.navigateLink(suggestion); break; }

        case FieldNames.FacebookConsent: { refresh = this.getFacebookConsent(suggestion); break; }
        case FieldNames.CloudADConsent: { refresh = this.getCloudADConsent(suggestion); break; }

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

SuggestionManager.prototype.addContact = function (suggestion) {
    var dataModel = this.dataModel;
    var item = dataModel.FindItem(suggestion.EntityID);
    if (item != null) {
        if (item.HasField(FieldNames.Contacts)) {
            var contact = $.parseJSON(suggestion.Value);
        }
        // local function for adding Contact 
        var addContactFunc = function (list) {
            if (list != null && list.IsList) {
                // create and insert the contact reference
                var contactRef = $.extend(new Item(), {
                    Name: contact.Name,
                    ItemTypeID: ItemTypes.Reference,
                    FolderID: contact.FolderID,
                    ParentID: list.ID,
                    UserID: contact.UserID
                });
                contactRef.SetFieldValue(FieldNames.ItemRef, contact.ID);
                list.InsertItem(contactRef, null, null, null);

                // create and insert the contact itself into default 'Contacts' folder
                var contactFolder = dataModel.FindDefault(ItemTypes.Contact);
                contact.FolderID = contactFolder.ID;
                contactFolder.InsertItem(contact, null, null, item);
            }
        }

        // will return null if addContactFunc is called, otherwise need to call it
        var contactsList = item.GetFieldValue(FieldNames.Contacts, addContactFunc);
        if (contactsList != null && contactsList.IsList) {
            addContactFunc(contactsList);
        }
    }
    return this.chooseSuggestion(suggestion);
}

SuggestionManager.prototype.navigateLink = function (suggestion) {
    return this.likeSuggestion(suggestion,
        function () {
            window.open(suggestion.Value);
        });
}

SuggestionManager.prototype.getFacebookConsent = function (suggestion) {
    var msg = 'You will be redirected to Facebook to allow Zaplify to access your Facebook contacts.\r\rDo you want to continue?';
    if (confirm(msg)) {
        Service.GetFacebookConsent();
    }
    return false;
}

SuggestionManager.prototype.getCloudADConsent = function (suggestion) {
    var msg = 'Once you are redirected, simply login with your Office 365 credentials to allow Zaplify to access your contacts.\r\rDo you want to continue?';
    if (confirm(msg)) {
        Service.GetCloudADConsent();
    }
    return false;
}