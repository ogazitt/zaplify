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
// private members

DataModel.onDataChangedHandlers = {};


// ---------------------------------------------------------
// public methods

DataModel.Init = function DataModel$Init(jsonConstants, jsonUserData) {
    this.processConstants($.parseJSON(jsonConstants));
    this.processUserData($.parseJSON(jsonUserData));
}

DataModel.AddDataChangedHandler = function (name, handler) {
    this.onDataChangedHandlers[name] = handler;
}

DataModel.RemoveDataChangedHandler = function (name) {
    delete this.onDataChangedHandlers[name];
}

// generic helper for getting local items associated with folder or list item
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

// refreshes datamodel with current state of server
DataModel.Refresh = function DataModel$Refresh() {
    // refresh constants
    Service.GetResource('constants', null,
        function (responseState) {
            DataModel.processConstants(responseState.result);
        });
    // refresh user data
    Service.GetResource('users', null,
        function (responseState) {
            DataModel.processUserData(responseState.result);
            DataModel.fireDataChanged('DataModel');
        });
}

// generic helper for inserting a new folder or item, invokes server and updates local data model
//  newItem must have Name defined
//  containerItem may be null, a folder, or list item
//  adjacentItem may be null, a folder, or item
//  insertBefore will be false by default (insert after adjacentItem)
DataModel.InsertItem = function DataModel$InsertItem(newItem, containerItem, adjacentItem, insertBefore) {
    if (newItem != null && newItem.Name != null) {
        var resource = 'items';
        if (containerItem == null) {                                        // inserting a new folder
            resource = 'folders';
        } else if (containerItem.FolderID == null) {                        // inserting into a folder 
            newItem.FolderID = containerItem.ID;
            newItem.ParentID = null;
            newItem.ItemTypeID = (newItem.ItemTypeID == null) ? containerItem.ItemTypeID : newItem.ItemTypeID;
        } else if (containerItem.IsList != null && containerItem.IsList) {  // inserting into list item
            newItem.FolderID = containerItem.FolderID;
            newItem.ParentID = containerItem.ID;
            newItem.ItemTypeID = (newItem.ItemTypeID == null) ? containerItem.ItemTypeID : newItem.ItemTypeID;
        } else {
            return false;                                                   // do not insert into item that is not a list
        }

        // TODO: sort order

        Service.InsertResource(resource, newItem,
            function (responseState) {                                      // successHandler
                var insertedItem = responseState.result;
                if (containerItem == null) {                                // add to Folders
                    DataModel.addFolder(insertedItem);
                } else {                                                    // add to container
                    containerItem.addItem(insertedItem);
                }
            });
        return true;
    }
    return false;
}

DataModel.InsertFolder = function (newFolder, adjacentFolder, insertBefore) {
    return DataModel.InsertItem(null, newFolder, adjacentFolder, insertBefore);
};

// generic helper for updating a folder or item, invokes server and updates local data model
DataModel.UpdateItem = function DataModel$UpateItem(originalItem, updatedItem) {
    if (originalItem != null && updatedItem != null) {
        var resource = (originalItem.FolderID == null) ? 'folders' : 'items';
        var data = [originalItem, updatedItem];
        Service.UpdateResource(resource, originalItem.ID, data,
            function (responseState) {                                      // successHandler
                var returnedItem = responseState.result;
                var success = originalItem.update(returnedItem);            // update Folder or Item
                // TODO: report failure to update
            });
        return true;
    }
    return false;
}

// generic helper for deleting a folder or item, invokes server and updates local data model
DataModel.DeleteItem = function DataModel$DeleteItem(item) {
    if (item != null) {
        var resource = (item.FolderID == null) ? 'folders' : 'items';
        Service.DeleteResource(resource, item.ID, item,
            function (responseState) {                                      // successHandler
                var deletedItem = responseState.result;
                // delete item from local data model
                if (item.FolderID == null) {
                    delete DataModel.Folders[item.ID];                      // delete Folder
                } else {
                    delete DataModel.Folders[item.FolderID].Items[item.ID]; // delete Item
                }
                DataModel.fireDataChanged('Folders');
            });
        return true;
    }
    return false;
}


// ---------------------------------------------------------
// private methods

DataModel.fireDataChanged = function (type, id) {
    for (var name in DataModel.onDataChangedHandlers) {
        var handler = DataModel.onDataChangedHandlers[name];
        if (typeof (handler) == "function") {
            handler(type, id);
        }
    }
}

DataModel.addFolder = function (newFolder) {
    DataModel.Folders[newFolder.ID] = newFolder;
    DataModel.fireDataChanged('Folders');
};

DataModel.processConstants = function DataModel$processConstants(jsonParsed) {
    var constants = {};
    for (var key in jsonParsed) {
        constants[key] = jsonParsed[key];
    }

    // key ItemTypes by both ID and Name
    var itemTypes = {};
    constants.ItemTypesByName = {};
    for (var i in constants.ItemTypes) {
        var itemType = constants.ItemTypes[i];
        itemTypes[itemType.ID] = itemType;
        constants.ItemTypesByName[itemType.Name] = itemType;
        // sort fields by SortOrder
        var orderedFields = [];
        for (var j in itemType.Fields) {
            var field = itemType.Fields[j];
            orderedFields[field.SortOrder] = field;
        }
        itemType.Fields = orderedFields;
    }
    constants.ItemTypes = itemTypes;

    // key Priorities by Name
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

        // attach helper functions and view state to folders
        DataModel.attachItemFunctions(folder);
        DataModel.attachViewState(folder);

        var items = folder.Items;
        folder.Items = {};
        var listCount = 0;
        for (var j in items) {
            var item = items[j];
            var iid = item.ID;

            // attach helper functions and view state to items
            DataModel.attachItemFunctions(item);
            DataModel.attachViewState(item);

            if (item.IsList) {
                listCount++;
                item.ViewState.Expand = (listCount == 1);
            }
            folder.Items[iid] = item;
        }
        folders[fid] = folder;
    }
    DataModel.Folders = folders;
}

DataModel.attachItemFunctions = function DataModel$attachItemFunctions(item) {
    if (item.FolderID === undefined) {
        // attach helper functions to Folder
        var folder = item;
        folder.GetItems = function () { return DataModel.GetItems(this.ID, null); };
        folder.InsertItem = function (newItem, adjacentItem, insertBefore) { return DataModel.InsertItem(newItem, this, adjacentItem, insertBefore); };
        folder.Update = function (updatedFolder) { return DataModel.UpdateItem(this, updatedFolder); };
        folder.Delete = function () { return DataModel.DeleteItem(this); };
        folder.addItem = function (newItem) {
            DataModel.attachItemFunctions(newItem);
            DataModel.attachViewState(newItem);
            if (this.Items == null) this.Items = {};
            this.Items[newItem.ID] = newItem;
            DataModel.fireDataChanged('Folder', this.ID);
        };
        folder.update = function (updatedFolder) {
            if (this.ID == updatedFolder.ID) {
                DataModel.attachItemFunctions(updatedFolder);
                DataModel.attachViewState(updatedFolder, this.ViewState);
                DataModel.Folders[updatedFolder.ID] = updatedFolder;
                DataModel.fireDataChanged('Folder', this.ID);
                return true;
            }
            return false;
        };
    } else {
        // add helper functions to Item
        item.GetParent = function () { return (this.ParentID == null) ? null : DataModel.Folders[this.FolderID].Items[this.ParentID]; };
        item.GetItems = function () { return DataModel.GetItems(this.FolderID, this.ID); };
        item.InsertItem = function (newItem, adjacentItem, insertBefore) { return DataModel.InsertItem(newItem, this, adjacentItem, insertBefore); };
        item.Update = function (updatedItem) { return DataModel.UpdateItem(this, updatedItem); };
        item.Delete = function () { return DataModel.DeleteItem(this); };
        item.addItem = function (newItem) {
            DataModel.attachItemFunctions(newItem);
            DataModel.attachViewState(newItem);
            var folder = DataModel.Folders[this.FolderID];
            folder.addItem(newItem);
        };
        item.update = function (updatedItem) {
            if (this.ID == updatedItem.ID) {
                DataModel.attachItemFunctions(updatedItem);
                DataModel.attachViewState(updatedItem, this.ViewState);
                if (this.FolderID == updatedItem.FolderID) {
                    DataModel.Folders[this.FolderID].Items[this.ID] = updatedItem;
                    DataModel.fireDataChanged('Folder', this.FolderID);
                } else {
                    delete DataModel.Folders[this.FolderID].Items[this.ID];
                    DataModel.Folders[updatedItem.FolderID].Items[updatedItem.ID];
                    DataModel.fireDataChanged('Folders');
                }
                return true;
            }
            return false;
        };
    }
}

DataModel.attachViewState = function DataModel$attachViewState(item, viewState) {
    viewState = (viewState == null) ? {} : viewState;
    item.ViewState = viewState;
}
