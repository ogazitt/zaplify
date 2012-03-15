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
DataModel.Suggestions = {};

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
            DataModel.fireDataChanged();
        });
}

// generic helper for getting local items associated with folder or list item
DataModel.GetItems = function DataModel$GetItems(folderID, parentID) {
    var items = {};
    var folder = DataModel.Folders[folderID];
    if (folder != undefined) {
        // extract list items first 
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
        } else if (containerItem.IsFolder()) {                              // inserting into a folder 
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

        // TODO: support insertions (always appends)
        if (adjacentItem == null) {                                         // append to end
            if (containerItem == null) {                                    // append new Folder to end
                var lastFolder = DataModel.FoldersMap.itemAt(-1);
                newItem.SortOrder = (lastFolder == null) ? 1000 : lastFolder.SortOrder + 1000;
            } else {                                                        // append new Item to end
                var lastItem = ItemMap.itemAt(containerItem.GetItems(), -1);
                newItem.SortOrder = (lastItem == null) ? 1000 : lastItem.SortOrder + 1000;
            }
        }

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

DataModel.InsertFolder = function DataModel$InsertFolder(newFolder, adjacentFolder, insertBefore) {
    return DataModel.InsertItem(newFolder, null, adjacentFolder, insertBefore);
};

// generic helper for updating a folder or item, invokes server and updates local data model
DataModel.UpdateItem = function DataModel$UpdateItem(originalItem, updatedItem) {
    if (originalItem != null && updatedItem != null) {
        var resource = (originalItem.IsFolder()) ? 'folders' : 'items';
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
        var resource = (item.IsFolder()) ? 'folders' : 'items';
        Service.DeleteResource(resource, item.ID, item,
            function (responseState) {                                      // successHandler
                var deletedItem = responseState.result;
                // delete item from local data model
                if (item.IsFolder()) {                                      // remove Folder
                    DataModel.FoldersMap.remove(item);
                    DataModel.fireDataChanged();
                } else {                                                    // remove Item
                    item.selectNextItem();
                    item.GetFolder().ItemsMap.remove(item); 
                    DataModel.fireDataChanged(item.FolderID, item.ID);
                }
            });
        return true;
    }
    return false;
}

// helper for retrieving suggestions, invokes server and updates local data model
DataModel.GetSuggestions = function DataModel$GetSuggestions(handler, entity, fieldName) {
    var filter = {};
    if (entity == null) { entity = DataModel.User; }
    filter.EntityID = entity.ID;
    filter.FieldName = fieldName;
    filter.EntityType = 'User';
    if (entity.hasOwnProperty('UserID')) {
        filter.EntityType = (entity.hasOwnProperty('FolderID')) ? 'Item' : 'Folder';
    }

    // using POST to query for suggestions
    Service.InsertResource('suggestions', filter,
        function (responseState) {                                      // successHandler
            var suggestions = responseState.result;
            DataModel.processSuggestions(suggestions);
            if (handler != null) {
                handler(DataModel.Suggestions);
            }
        });
}

// ---------------------------------------------------------
// private methods

DataModel.fireDataChanged = function (folderID, itemID) {
    for (var name in DataModel.onDataChangedHandlers) {
        var handler = DataModel.onDataChangedHandlers[name];
        if (typeof (handler) == "function") {
            handler(folderID, itemID);
        }
    }
}

DataModel.addFolder = function (newFolder) {
    DataModel.attachFolderFunctions(newFolder);
    DataModel.attachViewState(newFolder);
    DataModel.FoldersMap.append(newFolder);
    DataModel.fireDataChanged(newFolder.ID);
};

DataModel.processConstants = function DataModel$processConstants(jsonParsed) {
    var constants = {};
    for (var key in jsonParsed) {
        constants[key] = jsonParsed[key];
    }

    // attach ItemMap objects for ItemTypes
    // the ItemMap retains original storage array
    // the ItemMap.Items property provides associative array over storage
    var itemTypes = constants.ItemTypes;
    for (var i in itemTypes) {
        var itemType = itemTypes[i];
        // lookup Fields by Name
        var fieldsByName = {};
        for (var j in itemType.Fields) {
            var field = itemType.Fields[j];
            field.ClassName = 'fn-' + field.Name.toLowerCase();
            field.Class = field.ClassName + ' dt-' + field.DisplayType.toLowerCase();
            fieldsByName[field.Name] = field;
        }
        itemType.Fields = fieldsByName;
        DataModel.attachItemTypeFunctions(itemType);
    }
    constants.ItemTypesMap = new ItemMap(itemTypes);
    constants.ItemTypes = constants.ItemTypesMap.Items;

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


DataModel.processSuggestions = function DataModel$processSuggestions(jsonParsed) {
    var suggestions = {};
    var groupNameMap = {};
    var nGroup = 0;

    for (var i in jsonParsed) {
        var s = jsonParsed[i];
        var groupKey = s.WorkflowInstanceID + s.State;
        var groupID = groupNameMap[groupKey];
        if (groupID === undefined) {
            groupID = 'Group_' + (nGroup++).toString();
            groupNameMap[groupKey] = groupID;
            suggestions[groupID] = { GroupID: groupID, DisplayName: s.State, Suggestions: {} };
        }
        s.GroupID = groupID;
        suggestions[groupID].Suggestions[s.ID] = s;
    }
    DataModel.Suggestions = suggestions;
}

DataModel.processFolders = function DataModel$processFolders(folders) {
    // attach ItemMap objects for Folders and Items
    // the ItemMap retains original storage array
    // the ItemMap.Items property provides associative array over storage
    for (var i in folders) {
        var items = folders[i].Items;
        for (var j in items) {
            DataModel.attachItemFunctions(items[j]);
            DataModel.attachViewState(items[j]);
        }
        folders[i].ItemsMap = new ItemMap(items);
        folders[i].Items = folders[i].ItemsMap.Items;
        DataModel.attachFolderFunctions(folders[i]);
        DataModel.attachViewState(folders[i]);
        // TODO: mark 'default' folders in database
        folders[i].IsDefault = (i < 4);
    }
    DataModel.FoldersMap = new ItemMap(folders);
    DataModel.Folders = DataModel.FoldersMap.Items;
}

DataModel.attachFolderFunctions = function DataModel$attachFolderFunctions(folder) {
    // attach Folder helper functions
    // public helpers
    folder.IsFolder = function () { return true; };
    folder.GetItemType = function () { return DataModel.Constants.ItemTypes[this.ItemTypeID]; };
    folder.GetItems = function () { return DataModel.GetItems(this.ID, null); };
    folder.InsertItem = function (newItem, adjacentItem, insertBefore) { return DataModel.InsertItem(newItem, this, adjacentItem, insertBefore); };
    folder.Update = function (updatedFolder) { return DataModel.UpdateItem(this, updatedFolder); };
    folder.Delete = function () { return DataModel.DeleteItem(this); };

    // private helpers
    folder.addItem = function (newItem) {
        DataModel.attachItemFunctions(newItem);
        DataModel.attachViewState(newItem);
        newItem.ViewState.Select = newItem.IsList;
        if (this.ItemsMap == null) {
            this.ItemsMap = new ItemMap([newItem]);
            this.Items = this.ItemsMap.Items;
        } else {
            this.ItemsMap.append(newItem);
        }
        DataModel.fireDataChanged(this.ID, newItem.ID);
    };
    folder.update = function (updatedFolder) {
        if (this.ID == updatedFolder.ID) {
            DataModel.attachFolderFunctions(updatedFolder);
            DataModel.attachViewState(updatedFolder, this.ViewState);
            DataModel.FoldersMap.update(updatedFolder);
            DataModel.fireDataChanged(this.ID);
            return true;
        }
        return false;
    };
}

DataModel.attachItemFunctions = function DataModel$attachItemFunctions(item) {
    // add Item helper functions
    // public helpers
    item.IsFolder = function () { return false; };
    item.GetFolder = function () { return (DataModel.Folders[this.FolderID]); };
    item.GetParent = function () { return (this.ParentID == null) ? null : this.GetFolder().Items[this.ParentID]; };
    item.GetItemType = function () { return DataModel.Constants.ItemTypes[this.ItemTypeID]; };
    item.GetItems = function () { return DataModel.GetItems(this.FolderID, this.ID); };
    item.InsertItem = function (newItem, adjacentItem, insertBefore) { return DataModel.InsertItem(newItem, this, adjacentItem, insertBefore); };
    item.Update = function (updatedItem) { return DataModel.UpdateItem(this, updatedItem); };
    item.Delete = function () { return DataModel.DeleteItem(this); };

    // private helpers
    item.addItem = function (newItem) {
        this.GetFolder().addItem(newItem);
    };
    item.update = function (updatedItem) {
        if (this.ID == updatedItem.ID) {
            DataModel.attachItemFunctions(updatedItem);
            DataModel.attachViewState(updatedItem, this.ViewState);
            if (this.FolderID == updatedItem.FolderID) {
                this.GetFolder().ItemsMap.update(updatedItem);
            } else {
                this.GetFolder().ItemsMap.remove(this);
                updatedItem.GetFolder().ItemsMap.append(updatedItem);
            }
            DataModel.fireDataChanged(this.FolderID, this.ID);
            return true;
        }
        return false;
    };
    item.selectNextItem = function () {
        var parent = this.GetParent();
        var parentItems = (parent == null) ? this.GetFolder().GetItems() : parent.GetItems();
        var myIndex = ItemMap.indexOf(parentItems, this.ID);
        var nextItem = ItemMap.itemAt(parentItems, myIndex + 1);
        if (nextItem != null) {
            nextItem.ViewState.Select = true;
        } else if (myIndex == 0) {
            if (parent != null) { parent.ViewState.Select = true; }
        } else {
            var prevItem = ItemMap.itemAt(parentItems, myIndex - 1);
            if (prevItem != null) { prevItem.ViewState.Select = true; }
        }
    }
}

DataModel.attachItemTypeFunctions = function DataModel$attachItemTypeFunctions(itemType) {
    // add Item helper functions
    // public helpers
    itemType.HasField = function (name) { return (this.Fields.hasOwnProperty(name)); };
}

DataModel.attachViewState = function DataModel$attachViewState(item, viewState) {
    viewState = (viewState == null) ? {} : viewState;
    item.ViewState = viewState;
}

// ---------------------------------------------------------
// ItemMap object - provides associative array over array
// (all items in array MUST have an ID property)
//
// NOTE: 
// Maintains original storage of array
// Items property is associative array with ID access
//
function ItemMap(array) {
    this.array = array;
    this.Items = {};
    for (var i in array) {
        if (array[i].hasOwnProperty('ID')) {
            this.Items[array[i].ID] = array[i];
        } else {
            throw ItemMap.errorMustHaveID;
        }
    }
}

ItemMap.prototype.indexOf = function (item) {
    for (var i in this.array) {
        if (this.array[i].ID == item.ID) {
            return i;
        }
    }
    return -1;
}

ItemMap.prototype.itemAt = function (index) {
    if (index < 0) {                            // return last item for negative index
        return this.array[this.array.length - 1];
    }
    if (index < this.array.length) {           
        return this.array[index];
    }
    return null;                                // return null if index out of range
}

ItemMap.prototype.append = function (item) {
    if (item.hasOwnProperty('ID')) {
        this.array = this.array.concat(item);
        this.Items[item.ID] = this.array[this.array.length - 1];
    } else {
        throw ItemMap.errorMustHaveID;
    }
}

ItemMap.prototype.update = function (item) {
    if (item.hasOwnProperty('ID')) {
        // TODO: check SortOrder and reorder if necessary
        var index = this.indexOf(item);
        this.array[index] = item;
        this.Items[item.ID] = this.array[index];
    } else {
        throw ItemMap.errorMustHaveID;
    }
}

ItemMap.prototype.remove = function (item) {
    if (item.hasOwnProperty('ID')) {
        var index = this.indexOf(item);
        if (index >= 0) {
            this.array.splice(index, 1);
            delete this.Items[item.ID];
        }
    } else {
        throw ItemMap.errorMustHaveID;
    }
}

// ---------------------------------------------------------
// static members

ItemMap.count = function (map) {
    var i = 0, key;
    for (key in map) {
        if (map.hasOwnProperty(key)) i++;
    }
    return i;
}

ItemMap.indexOf = function (map, id) {
    var index = -1, i = 0;
    for (var key in map) {
        if (key == id) { index = i; break; }
        i++;
    }
    return index;
}

ItemMap.itemAt = function (map, index) {
    for (var key in map) {
        var item = map[key];
        if (index-- == 0) { return item; }
    }
    // negative index will return last item
    return (index < 0) ? item : null;
}

ItemMap.errorMustHaveID = 'ItemMap requires all items in array to have an ID property.';

// ---------------------------------------------------------
// FieldNames constants

var FieldNames = {
    Name : "Name",                  // String
    Description : "Description",    // String
    Priority : "Priority",          // Integer
    Complete : "Complete",          // Boolean 
    DueDate : "DueDate",            // DateTime
    ReminderDate : "ReminderDate",  // DateTime
    Birthday : "Birthday",          // DateTime
    Address : "Address",            // Address
    WebLink : "WebLink",            // Url
    Email : "Email",                // Email
    Phone : "Phone",                // Phone
    HomePhone : "HomePhone",        // Phone
    WorkPhone : "WorkPhone",        // Phone
    Amount : "Amount",              // String
    Cost : "Cost",                  // Currency
    ItemTags : "ItemTags",          // TagIDs
    ItemRef : "ItemRef",            // ItemID
    Locations : "Locations",        // ItemID
    Contacts: "Contacts",           // ItemID


    FacebookConsent: "FacebookConsent",
    CloudADConsent: "CloudADConsent"
};

// ---------------------------------------------------------
// DisplayTypes constants

var DisplayTypes = {
    Text : "Text",
    TextArea : "TextArea",
    Checkbox : "Checkbox",
    DatePicker : "DatePicker",
    Phone : "Phone",
    Email : "Email",
    Link : "Link",
    Currency : "Currency",
    Address : "Address",
    Priority : "Priority",
    TagList : "TagList",
    Reference : "Reference",
    LocationList : "LocationList",
    ContactList : "ContactList"
};


