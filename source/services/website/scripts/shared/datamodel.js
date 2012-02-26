//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// DataModel.js

// DataModel object
var DataModel = function DataModel$() { };

// ---------------------------------------------------------
// public members

DataModel.Constants = {};
DataModel.User = {};
DataModel.Folders = {};

// ---------------------------------------------------------
// public methods

DataModel.Init = function DataModel$Init(jsonConstants, jsonUserData) {
    this.processConstants($.parseJSON(jsonConstants));
    this.processUserData($.parseJSON(jsonUserData));
}

DataModel.GetItems = function DataModel$GetItems(folderID, parentID) {
    var items = {};
    var folder = DataModel.Folders[folderID];
    if (folder != undefined) {
        // extract list items first (until order is supported)
        for (var id in folder.Items) {
            var item = folder.Items[id];
            if (item.ParentID == parentID && item.IsList) {
                items[id] = folder.Items[id];
            }
        }
        for (var id in folder.Items) {
            var item = folder.Items[id];
            if (item.ParentID == parentID && !item.IsList) {
                items[id] = folder.Items[id];
            }
        }
    }
    return items;
}

// ---------------------------------------------------------
// private members


// ---------------------------------------------------------
// private methods

DataModel.processConstants = function DataModel$processConstants(jsonParsed) {
    var constants = {};
    for (var key in jsonParsed) {
        constants[key] = jsonParsed[key];
    }

    // replace ListType array with associative arrays (objects)
    var tempListTypes = {};
    constants.ListTypesByName = {};
    for (var i in constants.ListTypes) {
        var listType = constants.ListTypes[i];
        tempListTypes[listType.ID] = listType;
        constants.ListTypesByName[listType.Name] = listType;
        listType.fields = [];
        for (var j in listType.Fields) {
            var field = listType.Fields[j];
            listType.fields[field.SortOrder] = field;
        }
        listType.Fields = listType.fields;
    }
    constants.ListTypes = tempListTypes;

    // add PrioritiesByName array with associative array by name
    constants.PrioritiesByName = {};
    for (var i in constants.Priorities) {
        var priority = constants.Priorities[i];
        constants.PrioritiesByName[priority.Name] = priority;
    }

    DataModel.Constants = constants;
}

DataModel.processUserData = function DataModel$processUserData(jsonParsed) {
    var userData = {};
    for (var key in jsonParsed) {
        userData[key] = jsonParsed[key];
    }

    // process User
    var user = {};
    user.ID = userData.ID;
    user.Name = userData.Name;
    user.Email = userData.Email;
    user.CreateDate = userData.CreateDate;
    DataModel.User = user;

    // process Folders
    DataModel.processFolders(userData.Folders);

    // process custom ItemTypes and Tags (add to constants?)

}

DataModel.processFolders = function DataModel$processFolders(jsonParsed) {
    var folders = {}
    // transform Folder and Item index arrays into named arrays where name is ID
    for (var i in jsonParsed) {
        var folder = jsonParsed[i];
        var fid = folder.ID;

        // add helper functions to folders
        folder.GetItems = function () { return DataModel.GetItems(this.ID, null); };

        var items = folder.Items;
        folder.Items = {};
        var listCount = 0;
        for (var j in items) {
            var item = items[j];
            var iid = item.ID;

            // TODO: add view state to server storage
            item.ViewState = {};
            if (item.IsList) {
                listCount++;
                item.ViewState.Expand = (listCount == 1);

                // add helper functions to list items
                item.GetItems = function () { return DataModel.GetItems(this.FolderID, this.ID); };
            }
            folder.Items[iid] = item;
        }
        folders[fid] = folder;
    }
    DataModel.Folders = folders;
}