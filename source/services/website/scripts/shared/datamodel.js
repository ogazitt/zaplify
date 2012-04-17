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
DataModel.UserSettings;        

// ---------------------------------------------------------
// private members

DataModel.onDataChangedHandlers = {};
DataModel.timeStamp = '/Date(0)/';

// ---------------------------------------------------------
// public methods

DataModel.Init = function DataModel$Init(jsonConstants, jsonUserData) {
    this.processConstants($.parseJSON(jsonConstants));
    this.processUserData($.parseJSON(jsonUserData));
}

DataModel.Close = function DataModel$Close() {
    Service.Close();
    DataModel.UserSettings.Save();
}

DataModel.AddDataChangedHandler = function (name, handler) {
    this.onDataChangedHandlers[name] = handler;
}

DataModel.RemoveDataChangedHandler = function (name) {
    delete this.onDataChangedHandlers[name];
}

// refreshes datamodel with current state of server
DataModel.Refresh = function DataModel$Refresh(itemID) {
    itemID = (itemID == null) ? DataModel.UserSettings.ViewState.SelectedItem : itemID;

    // refresh user data
    Service.GetResource(Service.UsersResource, null,
        function (responseState) {
            DataModel.processUserData(responseState.result);
            DataModel.restoreSelection(itemID);
        });
}

// generic helper for finding folder or item for given ID
DataModel.FindItem = function DataModel$FindItem(itemID) {
    var folder = DataModel.Folders[itemID];
    if (folder != null) { return folder; }

    for (id in DataModel.Folders) {
        folder = DataModel.Folders[id];
        var item = folder.Items[itemID];
        if (item != null) { return item; }
    }
    return null;
}

// generic helper for finding default folder or list for given ItemType
// TODO: support user defaults stored in hidden System folder
DataModel.FindDefault = function DataModel$FindDefault(itemType) {
    for (id in DataModel.Folders) {
        var folder = DataModel.Folders[id];
        if (folder.ItemTypeID == itemType) {
            return folder;
        }
    }
    return null;
}

// generic helper for getting local items associated with folder or list item
DataModel.GetItems = function DataModel$GetItems(folderID, parentID) {
    var items = {};
    var folder = (typeof(folderID) == 'object') ? folderID : DataModel.Folders[folderID];
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
//  activeItem will be used when firing data changed event (indicating which item to select)
//      undefined will result in default behavior
//      null will result in the data changed event NOT being fired
DataModel.InsertItem = function DataModel$InsertItem(newItem, containerItem, adjacentItem, insertBefore, activeItem) {
    if (newItem != null && newItem.Name != null) {
        var resource = Service.ItemsResource;
        if (containerItem == null) {                                        // inserting a new folder
            resource = Service.FoldersResource;
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
                if (containerItem == null) {                                // add new Folder
                    DataModel.addFolder(insertedItem, activeItem);
                } else {                                                    // add new Item to container
                    containerItem.addItem(insertedItem, activeItem);
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
        updatedItem.LastModified = DataModel.timeStamp;                     // timestamp on server
        var resource = (originalItem.IsFolder()) ? Service.FoldersResource : Service.ItemsResource;        
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
        var resource = (item.IsFolder()) ? Service.FoldersResource : Service.ItemsResource;
        Service.DeleteResource(resource, item.ID, item,
            function (responseState) {                                      // successHandler
                var deletedItem = responseState.result;
                // delete item from local data model
                if (item.IsFolder()) {                                      // remove Folder
                    DataModel.FoldersMap.remove(item);
                    DataModel.fireDataChanged();
                } else {                                                    // remove Item
                    var nextItem = item.selectNextItem();
                    var nextItemID = (nextItem == null) ? null : nextItem.ID;
                    item.GetFolder().ItemsMap.remove(item);
                    DataModel.fireDataChanged(item.FolderID, nextItemID);
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
    filter.EntityType = EntityTypes.User;
    if (entity.hasOwnProperty('UserID')) {
        filter.EntityType = (entity.hasOwnProperty('FolderID')) ? EntityTypes.Item : EntityTypes.Folder;
    }

    // using POST to query for suggestions
    Service.InsertResource(Service.SuggestionsResource, filter,
        function (responseState) {                                      // successHandler
            var suggestions = responseState.result;
            if (suggestions.length > 0) {
                DataModel.SuggestionsRetrieved = new Date();            // timestamp
            }
            DataModel.processSuggestions(suggestions);
            if (handler != null) {
                handler(DataModel.Suggestions);
            }
        });
}

// helper for selecting a suggestion, invokes server and updates local data model
DataModel.SelectSuggestion = function DataModel$SelectSuggestion(suggestion, reason, handler) {
    reason = (reason == null) ? Reasons.Chosen : reason;
    var selected = $.extend({}, suggestion);
    selected.TimeSelected = DataModel.timeStamp;     // timestamp on server
    selected.ReasonSelected = reason;
    var data = [suggestion, selected];
    Service.UpdateResource(Service.SuggestionsResource, suggestion.ID, data,
        function (responseState) {                                      // successHandler
            var suggestion = responseState.result;
            if (handler != null) {
                handler(selected);
            }
        });
    }

// helper for removing a suggestion from local suggestions
DataModel.RemoveSuggestion = function DataModel$RemoveSuggestion(suggestion) {
    var group = DataModel.Suggestions[suggestion.GroupID];
    if (group != null) {
        delete group.Suggestions[suggestion.ID];
    }
}

// ---------------------------------------------------------
// private methods

DataModel.fireDataChanged = function (folderID, itemID) {
    if (folderID != DataModel.UserSettings.Folder.ID) {
        for (var name in DataModel.onDataChangedHandlers) {
            var handler = DataModel.onDataChangedHandlers[name];
            if (typeof (handler) == "function") {
                handler(folderID, itemID);
            }
        }
    }
}

DataModel.getFolder = function (folderID) {
    var folder = DataModel.Folders[folderID];
    if (folder == null && DataModel.UserSettings.Folder.ID == folderID) {
        folder = DataModel.UserSettings.Folder;
    }
    return folder;
}

DataModel.addFolder = function (newFolder, activeItem) {
    newFolder = $.extend(new Folder(), newFolder);              // extend with Folder functions
    DataModel.FoldersMap.append(newFolder);
    if (activeItem === undefined) {                             // default, fire event with new Folder
        DataModel.fireDataChanged(newFolder.ID);
    } else if (activeItem != null) {                            // fire event with activeItem
        DataModel.fireDataChanged(activeItem.FolderID, activeItem.ID);
    }                                                           // null, do not fire event
};

DataModel.processConstants = function DataModel$processConstants(jsonParsed) {
    var constants = {};
    for (var key in jsonParsed) {
        constants[key] = jsonParsed[key];
    }

    // wrap ItemTypes in ItemMap
    // the ItemMap retains original storage array
    // the ItemMap.Items property provides associative array over storage
    var itemTypes = constants.ItemTypes;
    for (var i in itemTypes) {
        var itemType = $.extend(new ItemType(), itemTypes[i]);      // extend with ItemType functions
        // lookup Fields by Name
        var fieldsByName = {};
        for (var j in itemType.Fields) {
            var field = itemType.Fields[j];
            field.ClassName = 'fn-' + field.Name.toLowerCase();
            field.Class = field.ClassName + ' dt-' + field.DisplayType.toLowerCase();
            fieldsByName[field.Name] = field;
        }
        itemType.Fields = fieldsByName;
        itemTypes[i] = itemType;
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
        var groupKey = s.WorkflowInstanceID + s.GroupDisplayName;
        var groupID = groupNameMap[groupKey];
        if (groupID === undefined) {
            groupID = (s.GroupDisplayName == SuggestionTypes.RefreshEntity) ? s.GroupDisplayName : 'Group_' + (nGroup++).toString();
            groupNameMap[groupKey] = groupID;
            suggestions[groupID] = { GroupID: groupID, DisplayName: s.GroupDisplayName, Suggestions: {} };
        }
        s.GroupID = groupID;
        suggestions[groupID].Suggestions[s.ID] = s;
    }
    DataModel.Suggestions = suggestions;
}

DataModel.processFolders = function DataModel$processFolders(folders) {
    // wrap Folders and Items in ItemMap
    // the ItemMap retains original storage array
    // the ItemMap.Items property provides associative array over storage
    var settingsIndex;
    for (var i in folders) {
        folders[i] = $.extend(new Folder(), folders[i]);    // extend with Folder functions
        var items = folders[i].Items;
        for (var j in items) {
            items[j] = $.extend(new Item(), items[j]);      // extend with Item functions
        }
        folders[i].ItemsMap = new ItemMap(items);
        folders[i].Items = folders[i].ItemsMap.Items;
        // TODO: mark 'default' folders in database
        folders[i].IsDefault = (i < 4);

        // extract folder for UserSettings
        if (folders[i].Name == SystemFolders.ClientSettings) { settingsIndex = i; }
    }
    if (settingsIndex == null) {
        // UserSettings folder does not exist, create it
        Service.InsertResource(Service.FoldersResource, { Name: SystemFolders.ClientSettings, ItemTypeID: ItemTypes.NameValue, SortOrder: 0 },
            function (responseState) {                                      // successHandler
                var settingsFolder = responseState.result;
                settingsFolder = $.extend(new Folder(), settingsFolder);    // extend with Folder functions
                DataModel.UserSettings = new UserSettings(settingsFolder);
            });
    } else {
        var settingsFolder = folders.splice(settingsIndex, 1)[0];
        DataModel.UserSettings = new UserSettings(settingsFolder);
    }
    DataModel.FoldersMap = new ItemMap(folders);
    DataModel.Folders = DataModel.FoldersMap.Items;
}

DataModel.restoreSelection = function DataModel$restoreSelection(itemID) {
    if (itemID === undefined && DataModel.UserSettings != null) {
        itemID = DataModel.UserSettings.ViewState.SelectedItem;
    }
    if (itemID != null) {
        var item = DataModel.FindItem(itemID);
        if (item != null) {
            DataModel.Folders[item.FolderID].ViewState.Select = true;
            item.ViewState.Select = true;
            DataModel.fireDataChanged(item.FolderID, item.ID);
            return;
        }
    }
    DataModel.fireDataChanged();
}

// ---------------------------------------------------------
// Folder object - provides prototype functions for Folder

function Folder(viewstate) { this.ViewState = (viewstate == null) ? {} : viewstate; }
// Folder public functions
Folder.prototype.IsFolder = function () { return true; };
Folder.prototype.GetItemType = function () { return DataModel.Constants.ItemTypes[this.ItemTypeID]; };
Folder.prototype.GetItems = function () { return DataModel.GetItems(this, null); };
Folder.prototype.GetItemByName = function (name, parentID) {
    for (id in this.Items) {
        var item = this.Items[id];
        if (item.Name == name) {
            if (parentID === undefined || item.ParentID == parentID) { return item; }
        }
    }
    return null;
}
Folder.prototype.GetSelectedItem = function () {
    for (id in this.Items) {
        if (this.Items[id].ViewState.Select == true) { return this.Items[id]; }
    }
    return null;
}
Folder.prototype.InsertItem = function (newItem, adjacentItem, insertBefore, activeItem) { return DataModel.InsertItem(newItem, this, adjacentItem, insertBefore, activeItem); };
Folder.prototype.Update = function (updatedFolder) { return DataModel.UpdateItem(this, updatedFolder); };
Folder.prototype.Delete = function () { return DataModel.DeleteItem(this); };
// Folder private functions
Folder.prototype.addItem = function (newItem, activeItem) {
    newItem = $.extend(new Item(), newItem);        // extend with Item functions
    newItem.ViewState.Select = newItem.IsList;
    if (this.ItemsMap == null) {
        this.ItemsMap = new ItemMap([newItem]);
        this.Items = this.ItemsMap.Items;
    } else {
        this.ItemsMap.append(newItem);
    }
    if (activeItem === undefined) {                     // default, fire event with new List or parent List
        var itemID = (newItem.IsList) ? newItem.ID : newItem.ParentID;
        DataModel.fireDataChanged(this.ID, itemID);
    } else if (activeItem != null) {                    // fire event with activeItem
        DataModel.fireDataChanged(activeItem.FolderID, activeItem.ID);
    }                                                   // null, do not fire event
};
Folder.prototype.update = function (updatedFolder) {
    if (this.ID == updatedFolder.ID) {
        updatedFolder = $.extend(new Folder(this.ViewState), updatedFolder);    // extend with Folder functions
        DataModel.FoldersMap.update(updatedFolder);
        DataModel.fireDataChanged(this.ID);
        return true;
    }
    return false;
};

// ---------------------------------------------------------
// Item object - provides prototype functions for Item

function Item(viewstate) { this.ViewState = (viewstate == null) ? {} : viewstate; }
// Item public functions
Item.prototype.IsFolder = function () { return false; };
Item.prototype.GetFolder = function () { return (DataModel.getFolder(this.FolderID)); };
Item.prototype.GetParent = function () { return (this.ParentID == null) ? null : this.GetFolder().Items[this.ParentID]; };
Item.prototype.GetItemType = function () { return DataModel.Constants.ItemTypes[this.ItemTypeID]; };
Item.prototype.GetItems = function () { return DataModel.GetItems(this.FolderID, this.ID); };
Item.prototype.InsertItem = function (newItem, adjacentItem, insertBefore, activeItem) { return DataModel.InsertItem(newItem, this, adjacentItem, insertBefore, activeItem); };
Item.prototype.Update = function (updatedItem) { return DataModel.UpdateItem(this, updatedItem); };
Item.prototype.Delete = function () { return DataModel.DeleteItem(this); };
Item.prototype.HasField = function (name) { return this.GetItemType().HasField(name); };
Item.prototype.GetField = function (name) { return this.GetItemType().Fields[name]; };

Item.prototype.Refresh = function () {
    var thisItem = this;
    Service.GetResource(Service.ItemsResource, this.ID,
        function (responseState) {
            var refreshItem = responseState.result;
            if (refreshItem != null) {
                // REVIEW: not calling update to avoid firing data change
                //thisItem.update(refreshItem);
                refreshItem = $.extend(new Item(thisItem.ViewState), refreshItem);
                thisItem.GetFolder().ItemsMap.update(refreshItem);
            }
        });
}

Item.prototype.GetFieldValue = function (field, handler) {
    // field parameter can be either field name or field object
    if (typeof (field) == 'string') {
        field = this.GetField(field);
    }
    if (field != null && this.HasField(field.Name)) {
        if (field.Name == FieldNames.Name) {
            return this.Name;
        }
        for (var i in this.FieldValues) {
            var fv = this.FieldValues[i];
            if (fv.FieldName == field.Name) {
                if (fv.Value != null && field.FieldType == FieldTypes.ItemID) {
                    var item = DataModel.FindItem(fv.Value);
                    if (item != null) { return item; }
                    // try to fetch referenced item from server
                    Service.GetResource('items', fv.Value,
                        function (responseState) {
                            var newItem = responseState.result;
                            if (newItem != null) {
                                newItem = $.extend(new Item(), newItem);        // extend with Item functions
                                DataModel.Folders[newItem.FolderID].ItemsMap.append(newItem);
                                if (handler != null) { handler(newItem); }
                            }
                        });
                } else {
                    return fv.Value;
                }
            }
        }
        //TODO: return default value based on FieldType
        if (field.FieldType == "Boolean") { return false; }
        return null;
    }
    return undefined;       // item does not have the field
};
Item.prototype.SetFieldValue = function (field, value) {
    // field parameter can be either field name or field object
    if (typeof (field) == 'string') {
        field = this.GetField(field);
    }
    if (field != null && this.HasField(field.Name)) {
        if (field.Name == FieldNames.Name) {
            this.Name = value;
            return true;
        }
        if (this.FieldValues == null) {
            this.FieldValues = [];
        }
        var updated = false;
        for (var i in this.FieldValues) {
            var fv = this.FieldValues[i];
            if (fv.FieldName == field.Name) {   // set existing field value
                fv.Value = value;
                updated = true;
                break;
            }
        }
        if (!updated) {                     // add field value
            this.FieldValues = this.FieldValues.concat(
                { FieldName: field.Name, ItemID: this.ID, Value: value });
        }
        return true;
    }
    return false;                           // item does not have the field
};

// Item private functions
Item.prototype.addItem = function (newItem, activeItem) { this.GetFolder().addItem(newItem, activeItem); };
Item.prototype.update = function (updatedItem) {
    if (this.ID == updatedItem.ID) {
        updatedItem = $.extend(new Item(this.ViewState), updatedItem);      // extend with Item functions
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
Item.prototype.selectNextItem = function () {
    var parent = this.GetParent();
    var parentItems = (parent == null) ? this.GetFolder().GetItems() : parent.GetItems();
    var myIndex = ItemMap.indexOf(parentItems, this.ID);
    var nextItem = ItemMap.itemAt(parentItems, myIndex + 1);
    if (nextItem != null) {
        nextItem.ViewState.Select = true; return nextItem;
    } else if (myIndex == 0) {
        if (parent != null) { parent.ViewState.Select = true; return parent; }
    } else {
        var prevItem = ItemMap.itemAt(parentItems, myIndex - 1);
        if (prevItem != null) { prevItem.ViewState.Select = true; return prevItem; }
    }
    return null;
}

// ---------------------------------------------------------
// ItemType object - provides prototype functions for ItemType

function ItemType() { }
ItemType.prototype.HasField = function (name) { return (this.Fields.hasOwnProperty(name)); };

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
// UserSettings object - provides prototype functions

function UserSettings(settingsFolder) {
    this.Folder = settingsFolder;
    
    this.viewStateItem = this.Folder.GetItemByName(UserSettings.ViewStateKey, null);
    if (this.viewStateItem == null) {
        this.ViewState = {};
        this.Folder.InsertItem({ Name: UserSettings.ViewStateKey, ItemTypeID: ItemTypes.NameValue }, null, null, null);
    } else {
        var value = this.viewStateItem.GetFieldValue(FieldNames.Value);
        this.ViewState = (value == null) ? {} : $.parseJSON(value);
    }
}

UserSettings.ViewStateKey = 'WebViewState';

UserSettings.prototype.Save = function () {
    this.UpdateViewState();
}

UserSettings.prototype.UpdateViewState = function () { 
    if (this.viewStateItem == null) {
        this.viewStateItem = this.Folder.GetItemByName(UserSettings.ViewStateKey, null);
    }
    var updatedItem = $.extend({}, this.viewStateItem);
    updatedItem.SetFieldValue(FieldNames.Value, JSON.stringify(this.ViewState));
    this.viewStateItem.Update(updatedItem);
}

UserSettings.prototype.Selection = function (folderID, itemID) {
    this.ViewState.SelectedFolder = folderID;
    this.ViewState.SelectedItem = itemID;
}


// ---------------------------------------------------------
// DATAMODEL CONSTANTS (keep in sync with EntityConstants.cs)

// ---------------------------------------------------------
// ItemTypes constants

var ItemTypes = {
    // standard item types
    Task : "00000000-0000-0000-0000-000000000001",
    Location : "00000000-0000-0000-0000-000000000002",
    Contact : "00000000-0000-0000-0000-000000000003",
    ListItem : "00000000-0000-0000-0000-000000000004",
    ShoppingItem : "00000000-0000-0000-0000-000000000005",
    // system item types
    Reference : "00000000-0000-0000-0000-000000000006",
    NameValue : "00000000-0000-0000-0000-000000000007"
}

// ---------------------------------------------------------
// FieldNames constants

var FieldNames = {
    Name : "Name",                          // String
    Description : "Description",            // String
    Priority : "Priority",                  // Integer
    Complete : "Complete",                  // Boolean 
    DueDate : "DueDate",                    // DateTime
    ReminderDate : "ReminderDate",          // DateTime
    Birthday : "Birthday",                  // DateTime
    Address : "Address",                    // Address
    WebLink : "WebLink",                    // Url
    WebLinks : "WebLinks",                  // ItemID
    Email : "Email",                        // Email
    Phone : "Phone",                        // Phone
    HomePhone : "HomePhone",                // Phone
    WorkPhone : "WorkPhone",                // Phone
    Amount : "Amount",                      // String
    Cost : "Cost",                          // Currency
    ItemTags : "ItemTags",                  // TagIDs
    ItemRef : "ItemRef",                    // ItemID
    Locations : "Locations",                // ItemID
    Contacts: "Contacts",                   // ItemID
    Value: "Value",                         // String (value of NameValue - e.g. SuggestionID)

    FacebookID: "FacebookID",               // String
    Intent: "Intent",                       // String
    Sources: "Sources"                      // String
}

// ---------------------------------------------------------
// EntityTypes constants

var EntityTypes = {
    User: "User",
    Folder: "Folder",
    Item: "Item"
}

// ---------------------------------------------------------
// FieldTypes constants

var FieldTypes = {
    String: "String",
    Boolean: "Boolean",
    Integer : "Integer",
    DateTime : "DateTime",
    Phone : "Phone",
    Email : "Email",
    Url : "Url",
    Address : "Address",
    Currency : "Currency",
    TagIDs : "TagIDs",
    ItemID : "ItemID"
}

// ---------------------------------------------------------
// DisplayTypes constants

var DisplayTypes = {
    Hidden: "Hidden",
    Text : "Text",
    TextArea : "TextArea",
    Checkbox : "Checkbox",
    DatePicker : "DatePicker",
    DateTimePicker : "DateTimePicker",
    Phone : "Phone",
    Email : "Email",
    Link : "Link",
    Currency : "Currency",
    Address : "Address",
    Priority : "Priority",
    TagList : "TagList",
    Reference : "Reference",
    ContactList : "ContactList",
    LocationList : "LocationList",
    UrlList : "UrlList"
}

// ---------------------------------------------------------
// SuggestionTypes constants

var SuggestionTypes = {
    ChooseOne: "ChooseOne",  
    ChooseOneSubject: "ChooseOneSubject",  
    ChooseMany: "ChooseMany",  
    GetFBConsent: "GetFBConsent",  
    GetADConsent: "GetADConsent",
    NavigateLink: "NavigateLink",
    RefreshEntity: "RefreshEntity"
}

// ---------------------------------------------------------
// Reasons constants

var Reasons = {
    Chosen : "Chosen",
    Ignore: "Ignore",
    Like: "Like",
    Dislike: "Dislike"
}

// ---------------------------------------------------------
// Sources constants

var Sources = {
    Directory : "Directory",
    Facebook : "Facebook",
    Local : "Local"
}

// ---------------------------------------------------------
// SystemFolders constants

var SystemFolders = {
    ClientSettings: "$ClientSettings",
    User: "$User"
}
