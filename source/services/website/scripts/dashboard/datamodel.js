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
    // preserve and restore current ViewState
    var currentViewState = DataModel.UserSettings.ViewState;

    // refresh user data
    Service.GetResource(Service.UsersResource, null,
        function (responseState) {
            DataModel.processUserData(responseState.result);
            DataModel.UserSettings.ViewState = currentViewState;
            DataModel.restoreSelection();
        });
}

// generic helper for finding folder or item for given ID
DataModel.FindItem = function DataModel$FindItem(itemID) {
    if (itemID != null) {
        var folder = DataModel.Folders[itemID];
        if (folder != null) { return folder; }

        for (id in DataModel.Folders) {
            folder = DataModel.Folders[id];
            var item = folder.Items[itemID];
            if (item != null) { return item; }
        }
    }
    return null;
}

// generic helper for finding a Location item that matches given address or latlong
// only looks in folders that have Location ItemType
DataModel.FindLocation = function DataModel$FindLocation(address, latlong) {
    for (id in DataModel.Folders) {
        var folder = DataModel.Folders[id];
        if (folder.ItemTypeID == ItemTypes.Location) {
            for (id in folder.Items) {
                var item = folder.Items[id];
                if (item.ItemTypeID == ItemTypes.Location) {
                    var itemLatLong = item.GetFieldValue(FieldNames.LatLong);
                    if (latlong != null && itemLatLong != null) {       // compare latlong fields
                        if (latlong == itemLatLong) { return item; }
                    } else {                                            // compare address fields
                        var itemAddress = item.GetFieldValue(FieldNames.Address);
                        if (address == itemAddress) { return item; }
                    }
                }
            }
        }
    }
    return null;
}

// generic helper for finding a Contact item that matches given name or facebookID
// only looks in folders that have Contact ItemType
DataModel.FindContact = function DataModel$FindContact(name, facebookID) {
    for (id in DataModel.Folders) {
        var folder = DataModel.Folders[id];
        if (folder.ItemTypeID == ItemTypes.Contact) {
            for (id in folder.Items) {
                var item = folder.Items[id];
                if (item.ItemTypeID == ItemTypes.Contact) {
                    var itemFacebookID = item.GetFieldValue(FieldNames.FacebookID);
                    if (facebookID != null && itemFacebookID != null) {     // compare facebook id 
                        if (facebookID == itemFacebookID) { return item; }
                    } else {                                                // compare name 
                        if (name == item.Name) { return item; }
                    }
                }
            }
        }
    }
    return null;
}

// generic helper for getting local items associated with folder or list item
DataModel.GetItems = function DataModel$GetItems(folderID, parentID, excludeListItems) {
    var items = {};
    var folder = (typeof (folderID) == 'object') ? folderID : DataModel.Folders[folderID];
    if (folder != undefined) {
        if (excludeListItems != true) {
            // extract list items first 
            for (var id in folder.Items) {
                var item = folder.Items[id];
                if (item.ParentID == parentID && item.IsList) {
                    items[id] = folder.Items[id];
                }
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
DataModel.InsertItem = function DataModel$InsertItem(newItem, containerItem, adjacentItem, insertBefore, activeItem, callback) {
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

        // add to local DataModel immediately (fire datachanged)
        if (newItem.ID == null) { newItem.ID = Math.uuid(); }           // assign ID if not defined
        delete newItem['Created'];                                      // remove Created field
        if (containerItem == null) {                                    // add new Folder
            newItem = DataModel.addFolder(newItem, activeItem);
        } else {                                                        // add new Item to container
            containerItem.addItem(newItem, activeItem);
        }

        Service.InsertResource(resource, newItem,
            function (responseState) {                                  // successHandler
                var insertedItem = responseState.result;
                newItem.update(insertedItem, null);                     // update local DataModel (do not fire datachanged)
                if (callback != null) {
                    callback(insertedItem);
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
DataModel.UpdateItem = function DataModel$UpdateItem(originalItem, updatedItem, activeItem) {
    if (originalItem != null && updatedItem != null) {
        // update local DataModel immediately (fire datachanged)
        originalItem.update(updatedItem, activeItem)

        updatedItem.LastModified = DataModel.timeStamp;                         // timestamp on server
        var resource = (originalItem.IsFolder()) ? Service.FoldersResource : Service.ItemsResource;
        var data = [originalItem, updatedItem];
        if (resource == Service.FoldersResource) {
            data = [originalItem.Copy(), updatedItem];                          // exclude items from original folder
        }

        Service.UpdateResource(resource, originalItem.ID, data,
            function (responseState) {                                          // successHandler
                var returnedItem = responseState.result;
                var success = originalItem.update(returnedItem, null);          // update local DataModel (do not fire datachanged)
                // TODO: report failure to update
            });
        return true;
    }
    return false;
}

// generic helper for deleting a folder or item, invokes server and updates local data model
DataModel.DeleteItem = function DataModel$DeleteItem(item, activeItem) {
    if (item != null) {
        // delete item from local DataModel (fire datachanged)
        if (item.IsFolder()) {                                      // remove Folder
            DataModel.FoldersMap.remove(item);
            DataModel.fireDataChanged();
        } else {                                                    // remove Item
            var parent = item.GetParent();
            if (parent != null && parent.ItemTypeID == ItemTypes.Reference) {
                // deleting a reference, don't change selection or fire data changed
                item.GetFolder().ItemsMap.remove(item);
            } else if (activeItem != null) {
                // select activeItem
                var activeFolderID = activeItem.IsFolder() ? activeItem.ID : activeItem.FolderID;
                var activeItemID = activeItem.IsFolder() ? null : activeItem.ID;
                item.GetFolder().ItemsMap.remove(item);
                DataModel.deleteReferences(item.ID);
                DataModel.fireDataChanged(activeFolderID, activeItemID);
            } else {
                var nextItem = item.selectNextItem();
                var nextItemID = (nextItem == null) ? null : nextItem.ID;
                item.GetFolder().ItemsMap.remove(item);
                DataModel.deleteReferences(item.ID);
                DataModel.fireDataChanged(item.FolderID, nextItemID);
            }
        }

        var resource = (item.IsFolder()) ? Service.FoldersResource : Service.ItemsResource;
        Service.DeleteResource(resource, item.ID, item,
            function (responseState) {                                      // successHandler
                var deletedItem = responseState.result;
                // TODO: report failure to delete
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
    if (entity.hasOwnProperty('ItemTypeID')) {
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
    // do not fire datachanged event for UserSetting folders
    if (DataModel.UserSettings.GetFolder(folderID) == null) {
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
    if (folder == null) {
        folder = DataModel.UserSettings.GetFolder(folderID);
    }
    return folder;
}

DataModel.addFolder = function (newFolder, activeItem) {
    newFolder = Folder.Extend(newFolder);                       // extend with Folder functions
    if (newFolder.ItemsMap == null) {
        newFolder.ItemsMap = new ItemMap([]);
        newFolder.Items = newFolder.ItemsMap.Items;
    }
    DataModel.FoldersMap.append(newFolder);
    if (activeItem === undefined) {                             // default, fire event with new Folder
        DataModel.fireDataChanged(newFolder.ID);
    } else if (activeItem != null) {                            // fire event with activeItem
        DataModel.fireDataChanged(activeItem.FolderID, activeItem.ID);
    }
    return newFolder;                                           // null, do not fire event
};

DataModel.deleteReferences = function (itemID) {
    // delete all items which Reference given itemID
    for (var fid in DataModel.Folders) {
        var folder = DataModel.Folders[fid];
        for (var iid in folder.Items) {
            var item = folder.Items[iid];
            if (item.ItemTypeID == ItemTypes.Reference) {
                for (var i in item.FieldValues) {
                    var fv = item.FieldValues[i];
                    if (fv.FieldName == FieldNames.EntityRef && fv.Value == itemID) {
                        item.GetFolder().ItemsMap.remove(item);
                    }
                }
            }
        }
    }
}

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
        var itemType = ItemType.Extend(itemTypes[i]);      // extend with ItemType functions
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
    var childSuggestions = [];
    var groupNameMap = {};
    var nGroup = 0;

    for (var i in jsonParsed) {
        var s = jsonParsed[i];
        if (s.ParentID == null) {
            // 2012-04-17 OG: change the key to just the GroupDisplayName
            //var groupKey = s.WorkflowInstanceID + s.GroupDisplayName;
            var groupKey = s.GroupDisplayName;

            var groupID = groupNameMap[groupKey];
            if (groupID === undefined) {
                groupID = (s.GroupDisplayName == SuggestionTypes.RefreshEntity) ? s.GroupDisplayName : 'Group_' + (nGroup++).toString();
                groupNameMap[groupKey] = groupID;
                suggestions[groupID] = { GroupID: groupID, DisplayName: s.GroupDisplayName, Suggestions: {} };
            }
            s.GroupID = groupID;
            suggestions[groupID].Suggestions[s.ID] = s;
        } else {
            childSuggestions.push(s);
        }
    }
    // nest child suggestions under parent suggestions
    for (i in childSuggestions) {
        var child = childSuggestions[i];
        for (groupID in suggestions) {
            var group = suggestions[groupID];
            var parent = group.Suggestions[child.ParentID];
            if (parent != null) {
                if (parent.Suggestions == null) {
                    parent.Suggestions = {};
                }
                parent.Suggestions[child.ID] = child;
                break;
            }
        }
    }
    DataModel.Suggestions = suggestions;
}

DataModel.processFolders = function DataModel$processFolders(folders) {
    // wrap Folders and Items in ItemMap
    // the ItemMap retains original storage array
    // the ItemMap.Items property provides associative array over storage
    var clientFolderIndex, webClientFolderIndex;
    for (var i in folders) {
        folders[i] = Folder.Extend(folders[i]);    // extend with Folder functions
        var items = folders[i].Items;
        for (var j in items) {
            items[j] = Item.Extend(items[j]);      // extend with Item functions
        }
        folders[i].ItemsMap = new ItemMap(items);
        folders[i].Items = folders[i].ItemsMap.Items;

        // extract folders for UserSettings ($Client and $WebClient folders)
        if (folders[i].Name == SystemFolders.Client) { clientFolderIndex = i; }
        if (folders[i].Name == SystemFolders.WebClient) { webClientFolderIndex = i; }
    }
    // assumes $Client folder has already been created
    var clientFolder = folders.splice(clientFolderIndex, 1)[0];
    if (webClientFolderIndex == null) {
        // $WebClient folder does not exist, create it
        DataModel.UserSettings = new UserSettings(clientFolder);
        Service.InsertResource(Service.FoldersResource, { Name: SystemFolders.WebClient, ItemTypeID: ItemTypes.NameValue, SortOrder: 0 },
            function (responseState) {                                      // successHandler
                var webClientFolder = responseState.result;
                webClientFolder = Folder.Extend(webClientFolder);    // extend with Folder functions
                DataModel.UserSettings = new UserSettings(clientFolder, webClientFolder);
            });
    } else {
        // adjust index after removing clientFolder
        if (webClientFolderIndex > clientFolderIndex) { webClientFolderIndex--; }
        var webClientFolder = folders.splice(webClientFolderIndex, 1)[0];
        DataModel.UserSettings = new UserSettings(clientFolder, webClientFolder);
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
            DataModel.fireDataChanged(item.FolderID, item.ID);
            return;
        }
    }
    var folderID = DataModel.UserSettings.ViewState.SelectedFolder;
    if (folderID != null) {
        DataModel.fireDataChanged(folderID);
        return;
    }
    DataModel.fireDataChanged();
}

// ---------------------------------------------------------
// Folder object - provides prototype functions for Folder

function Folder() { };
Folder.Extend = function Folder$Extend(folder) { return $.extend(new Folder(), folder); }   // extend with Folder prototypes

// Folder public functions
// do not deep copy, remove Items collection, copy is for updating Folder entity only
Folder.prototype.Copy = function () { var copy = $.extend(new Folder(), this); copy.Items = {}; copy.ItemsMap = {}; return copy; };                
Folder.prototype.IsFolder = function () { return true; };
Folder.prototype.IsDefault = function () {
    var defaultList = DataModel.UserSettings.GetDefaultList(this.ItemTypeID);
    if (defaultList != null) {
        return (defaultList.IsFolder()) ? (this.ID == defaultList.ID) : (this.ID == defaultList.FolderID);
    }
    return false;
};
Folder.prototype.IsSelected = function () { return DataModel.UserSettings.IsFolderSelected(this.ID); };
Folder.prototype.IsExpanded = function () { return DataModel.UserSettings.IsFolderExpanded(this.ID); };
Folder.prototype.Expand = function (expand) { DataModel.UserSettings.ExpandFolder(this.ID, expand); };
Folder.prototype.GetItemType = function () { return DataModel.Constants.ItemTypes[this.ItemTypeID]; };
Folder.prototype.GetItems = function (excludeListItems) { return DataModel.GetItems(this, null, excludeListItems); };
Folder.prototype.GetItem = function (itemID) { return this.Items[itemID]; }
Folder.prototype.GetItemByName = function (name, parentID) {
    for (id in this.Items) {
        var item = this.Items[id];
        if (item.Name == name) {
            if (parentID === undefined || item.ParentID == parentID) { return item; }
        }
    }
    return null;
}
// assumes the item being looked for is ItemTypes.NameValue or ItemTypes.Reference
Folder.prototype.GetItemByValue = function (value, parentID) {
    for (id in this.Items) {
        var item = this.Items[id];
        if (item.ItemTypeID == ItemTypes.NameValue || item.ItemTypeID == ItemTypes.Reference) {
            if (value == item.GetFieldValue(FieldNames.Value) &&
                (parentID === undefined || item.ParentID == parentID)) {
                    return item; 
                }
        }
    }
    return null;
}
Folder.prototype.GetSelectedItem = function () {
    for (id in this.Items) {
        if (DataModel.UserSettings.IsItemSelected(id) == true) { return this.Items[id]; }
    }
    return null;
}
Folder.prototype.InsertItem = function (newItem, adjacentItem, insertBefore, activeItem) { return DataModel.InsertItem(newItem, this, adjacentItem, insertBefore, activeItem); };
Folder.prototype.Update = function (updatedFolder) { return DataModel.UpdateItem(this, updatedFolder); };
Folder.prototype.Delete = function () { return DataModel.DeleteItem(this); };
// Folder private functions
Folder.prototype.addItem = function (newItem, activeItem) {
    newItem = Item.Extend(newItem);                    // extend with Item functions
    if (this.ItemsMap == null) {
        this.ItemsMap = new ItemMap([newItem]);
        this.Items = this.ItemsMap.Items;
    } else {
        this.ItemsMap.append(newItem);
    }
    if (activeItem === undefined) {                             // default, fire event with new List or parent List
        var itemID = (newItem.IsList) ? newItem.ID : newItem.ID; //.ParentID;
        DataModel.fireDataChanged(this.ID, itemID);
    } else if (activeItem != null) {                            // fire event with activeItem
        DataModel.fireDataChanged(activeItem.FolderID, activeItem.ID);
    }                                                           // null, do not fire event
};
Folder.prototype.update = function (updatedFolder) {
    if (this.ID == updatedFolder.ID) {
        updatedFolder = Folder.Extend(updatedFolder);  // extend with Folder functions
        updatedFolder.ItemsMap = this.ItemsMap;
        updatedFolder.Items = this.ItemsMap.Items;
        updatedFolder.FolderUsers = this.FolderUsers;
        DataModel.FoldersMap.update(updatedFolder);
        DataModel.Folders = DataModel.FoldersMap.Items;
        DataModel.fireDataChanged(this.ID);
        return true;
    }
    return false;
};

// ---------------------------------------------------------
// Item object - provides prototype functions for Item

function Item() { };
Item.Extend = function Item$Extend(item) { return $.extend(new Item(), item); }         // extend with Item prototypes

// Item public functions
Item.prototype.Copy = function () {                                                     // deep copy
    // sanity check, use most current Item in DataModel if it exists
    var folder = this.GetFolder();
    var currentThis = (folder == null) ? this : folder.GetItem(this.ID);
    return $.extend(true, new Item(), (currentThis == null) ? this : currentThis);
};         
Item.prototype.IsFolder = function () { return false; };
Item.prototype.IsDefault = function () {
    var defaultList = DataModel.UserSettings.GetDefaultList(this.ItemTypeID);
    if (defaultList != null) { return (this.ID == defaultList.ID);  }
    return false;
};
Item.prototype.Select = function () { return DataModel.UserSettings.Selection(this.FolderID, this.ID); };
Item.prototype.IsSelected = function (includingChildren) { return DataModel.UserSettings.IsItemSelected(this.ID, includingChildren); };
Item.prototype.GetFolder = function () { return (DataModel.getFolder(this.FolderID)); };
Item.prototype.GetParent = function () { return (this.ParentID == null) ? null : this.GetFolder().Items[this.ParentID]; };
Item.prototype.GetParentContainer = function () { return (this.ParentID == null) ? this.GetFolder() : this.GetParent(); };
Item.prototype.GetItemType = function () { return DataModel.Constants.ItemTypes[this.ItemTypeID]; };
Item.prototype.GetItems = function (excludeListItems) { return DataModel.GetItems(this.FolderID, this.ID, excludeListItems); };
Item.prototype.InsertItem = function (newItem, adjacentItem, insertBefore, activeItem) { return DataModel.InsertItem(newItem, this, adjacentItem, insertBefore, activeItem); };
Item.prototype.Update = function (updatedItem, activeItem) { return DataModel.UpdateItem(this, updatedItem, activeItem); };
Item.prototype.Delete = function (activeItem) { return DataModel.DeleteItem(this, activeItem); };
Item.prototype.HasField = function (name) { return this.GetItemType().HasField(name); };
Item.prototype.GetField = function (name) { return this.GetItemType().Fields[name]; };
Item.prototype.GetFields = function () { return this.GetItemType().Fields; };

Item.prototype.Refresh = function () {
    var thisItem = this;
    Service.GetResource(Service.ItemsResource, this.ID,
        function (responseState) {
            var refreshItem = responseState.result;
            if (refreshItem != null) {
                // do not fire data change
                thisItem.update(refreshItem, null);
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
                if (fv.Value != null && field.FieldType == FieldTypes.Guid) {
                    // automatically attempt to dereference Guid values to a Folder or Item
                    var item = DataModel.FindItem(fv.Value);
                    if (item != null) { return item; }
                }
                // javascript only recognizes lowercase boolean values
                if (field.FieldType == FieldTypes.Boolean) {
                    if (typeof (fv.Value) == 'string') {
                        fv.Value = (fv.Value.toLowerCase() == 'true');
                    }
                }
                return fv.Value;
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
Item.prototype.AddReference = function (field, item, replace) {
    // field parameter can be either field name or field object
    if (typeof (field) == 'string') {
        field = this.GetField(field);
    }
    if (field != null && this.HasField(field.Name) && field.FieldType == FieldTypes.Guid) {

        var refList = this.GetFieldValue(field);
        if (refList != null) {
            if (replace == true) {
                return this.replaceReference(refList, item);
            } else {
                return this.addReference(refList, item);
            }
        } else {
            // create refList and addReference in success handler
            var thisItem = this;
            var newRefList = {
                Name: field.Name, IsList: true, ItemTypeID: ItemTypes.Reference,
                FolderID: thisItem.FolderID, ParentID: thisItem.ID, UserID: thisItem.UserID
            };
            Service.InsertResource(Service.ItemsResource, newRefList,
                function (responseState) {
                    var insertedRefList = responseState.result;
                    thisItem.addItem(insertedRefList, null);
                    insertedRefList = DataModel.FindItem(insertedRefList.ID);
                    thisItem.addReference(insertedRefList, item);
                    var thisUpdatedItem = $.extend({}, thisItem);
                    thisUpdatedItem.SetFieldValue(field.Name, insertedRefList.ID);
                    thisItem.Update(thisUpdatedItem);
                });
        }
        return true;
    }
    return false;       // failed to add reference
};
Item.prototype.RemoveReferences = function (field) {
    // field parameter can be either field name or field object
    if (typeof (field) == 'string') {
        field = this.GetField(field);
    }
    if (field != null && this.HasField(field.Name) && field.FieldType == FieldTypes.Guid) {
        var refList = this.GetFieldValue(field);
        if (refList != null && refList.IsList) {
            // remove all references
            var itemRefs = refList.GetItems();
            for (var id in itemRefs) {
                var itemRef = itemRefs[id];
                itemRef.Delete();
            }
        }
        return true;
    }
    return false;       // failed to remove references
};

// Item private functions
Item.prototype.addItem = function (newItem, activeItem) { this.GetFolder().addItem(newItem, activeItem); };
Item.prototype.update = function (updatedItem, activeItem) {
    if (this.ID == updatedItem.ID) {
        updatedItem = Item.Extend(updatedItem);         // extend with Item functions
        var thisFolder = this.GetFolder();
        if (this.FolderID == updatedItem.FolderID) {
            thisFolder.ItemsMap.update(updatedItem);
            thisFolder.Items = thisFolder.ItemsMap.Items;
        } else {
            thisFolder.ItemsMap.remove(this);
            updatedItem.GetFolder().ItemsMap.append(updatedItem);
        }
        if (activeItem === undefined) {
            DataModel.fireDataChanged(this.FolderID, this.ID);
        } else if (activeItem != null) {
            DataModel.fireDataChanged(this.FolderID, activeItem.ID);
        }
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
        return nextItem;
    } else if (myIndex == 0) {
        if (parent != null) { return parent; }
    } else {
        var prevItem = ItemMap.itemAt(parentItems, myIndex - 1);
        if (prevItem != null) { return prevItem; }
    }
    return null;
}
Item.prototype.addReference = function (refList, itemToRef) {
    if (refList.IsList) {
        // create and insert the item reference
        var itemRef = Item.Extend(
        {
            Name: itemToRef.Name,
            ItemTypeID: ItemTypes.Reference,
            FolderID: refList.FolderID,
            ParentID: refList.ID,
            UserID: refList.UserID
        });
        itemRef.SetFieldValue(FieldNames.EntityRef, itemToRef.ID);
        itemRef.SetFieldValue(FieldNames.EntityType, EntityTypes.Item);
        refList.InsertItem(itemRef, null, null, null);
        return true;
    }
    return false;
}
Item.prototype.replaceReference = function (refList, itemToRef) {
    if (refList.IsList) {
        // replace reference
        var itemRefs = refList.GetItems();
        for (var id in itemRefs) {
            var itemRef = itemRefs[id];
            var updatedItemRef = itemRef.Copy();
            updatedItemRef.SetFieldValue(FieldNames.EntityRef, itemToRef.ID);
            itemRef.Update(updatedItemRef, refList.GetParent());
            return true;
        }
        return this.addReference(refList, itemToRef);
    }
    return false;
}

// ---------------------------------------------------------
// ItemType object - provides prototype functions for ItemType

function ItemType() { }
ItemType.Extend = function ItemType$Extend(itemType) { return $.extend(new ItemType(), itemType); }     // extend with ItemType prototypes
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
    this.updateMap();
}

ItemMap.prototype.updateMap = function () {
    this.Items = {};
    for (var i in this.array) {
        if (this.array[i].hasOwnProperty('ID')) {
            this.Items[this.array[i].ID] = this.array[i];
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
        var index = this.indexOf(item);
        var currentItem = this.itemAt(index);
        if (item.hasOwnProperty('SortOrder') && currentItem.hasOwnProperty('SortOrder') &&
            (item.SortOrder != currentItem.SortOrder)) {
            // move item to correct position in array
            this.array.splice(index, 1);
            for (var i in this.array) {
                if (this.array[i].hasOwnProperty('SortOrder') && item.SortOrder < this.array[i].SortOrder) {
                    this.array.splice(i, 0, item);
                    this.updateMap();
                    return;
                }
            }
            this.array.push(item);
            this.updateMap();
        } else {
            this.array[index] = item;
            this.Items[item.ID] = this.array[index];
        }
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
// LinkArray object - array of Link objects 
// [{Name:"name", Url:"link"}]
function LinkArray(json) {
    this.links = [];
    if (json != null && json.length > 0) {
        this.links = $.parseJSON(json); 
    }
}

LinkArray.prototype.Links = function () { return this.links; }
LinkArray.prototype.ToJson = function () { return JSON.stringify(this.links); }
LinkArray.prototype.Remove = function (index) { this.links.splice(index, 1); }
LinkArray.prototype.Add = function (link, name) {
    if (name != null) {                             
        // both name and link provided explicitly 
        this.links.push({ Name: name, Url: link });
        return this.links;
    }
    // check for name,link syntax
    var split = link.split(',');                    
    if (split.length > 1) {
        name = $.trim(split[0]);
        link = $.trim(split[1]);
        this.links.push({ Name: name, Url: link });
    } else {
        this.links.push({ Url: link });
    }
    return this.links;
}
LinkArray.prototype.ToText = function () {
    var text = '';
    for (var i in this.links) {
        var link = this.links[i];
        if (link.hasOwnProperty('Name')) {
            if (link.Name != null && link.Name.length > 0 && link.Name != link.Url) {
                text += link.Name + ', ';
            }
        }
        text += link.Url + '\r\n';
    }
    return text;
}
LinkArray.prototype.Parse = function (text) {
    var lines = text.split(/\r\n|\r|\n/g);
    for (var i in lines) {
        var parts = lines[i].split(',');
        if (parts.length == 1 && parts[0].length > 0) { this.links.push({ Url: parts[0] }); }
        if (parts.length == 2) { this.links.push({ Name: parts[0], Url: parts[1] }); }
    }
}

// ---------------------------------------------------------
// UserSettings object - provides prototype functions

function UserSettings(clientFolder, webClientFolder) {
    this.clientFolder = clientFolder;
    this.webClientFolder = webClientFolder;

    this.init(UserSettings.viewStateName, UserSettings.viewStateKey);
    this.init(UserSettings.preferencesName, UserSettings.preferencesKey);
}

UserSettings.defaultListsKey = 'DefaultLists';
UserSettings.viewStateName = 'ViewState';
UserSettings.viewStateKey = 'WebViewState';
UserSettings.preferencesName = 'Preferences';
UserSettings.preferencesKey = 'WebPreferences';

UserSettings.prototype.GetFolder = function (folderID) {
    if (this.clientFolder.ID == folderID) { return this.clientFolder; }
    if (this.webClientFolder.ID == folderID) { return this.webClientFolder; }
    return null;
}

UserSettings.prototype.GetDefaultList = function (itemType) {
    var defaultLists = this.clientFolder.GetItemByName(UserSettings.defaultListsKey);
    if (defaultLists != null) {
        var defaultList = this.clientFolder.GetItemByValue(itemType, defaultLists.ID);
        if (defaultList != null) {
            var list = defaultList.GetFieldValue(FieldNames.EntityRef);
            if (typeof (list) == 'object') { return list; }
        }
    }
    // find first folder for itemType
    var folder;
    for (id in DataModel.Folders) {
        folder = DataModel.Folders[id];
        if (folder.ItemTypeID == itemType) {
            return folder;
        }
    }
    // default to last folder
    return folder;
}

UserSettings.prototype.Selection = function (folderID, itemID) {
    this.ViewState.SelectedFolder = folderID;
    this.ViewState.SelectedItem = itemID;
}

UserSettings.prototype.IsFolderSelected = function (folderID) {
    return (this.ViewState.SelectedFolder == folderID);
}

UserSettings.prototype.IsItemSelected = function (itemID, includingChildren) {
    if (includingChildren == true) {
        var item = DataModel.FindItem(this.ViewState.SelectedItem);
        if (item != null && item.ParentID == itemID) { return true; }
    }
    return (this.ViewState.SelectedItem == itemID);
}

UserSettings.prototype.ExpandFolder = function (folderID, expand) {
    if (this.ViewState.ExpandedFolders == null) { this.ViewState.ExpandedFolders = {}; }
    if (expand == true) { this.ViewState.ExpandedFolders[folderID] = true; }
    else { delete this.ViewState.ExpandedFolders[folderID]; }
}

UserSettings.prototype.IsFolderExpanded = function (folderID) {
    return (this.ViewState.ExpandedFolders != null && this.ViewState.ExpandedFolders[folderID] == true);
}

UserSettings.prototype.Save = function () {
    // remove deleted folders from expanded folders list
    var expanded = {};
    for (var id in DataModel.Folders) {
        if (DataModel.Folders[id].IsExpanded()) { expanded[id] = true; }
    }
    this.ViewState.ExpandedFolders = expanded;
    this.update(UserSettings.viewStateName, UserSettings.viewStateKey);
}

UserSettings.prototype.UpdateTheme = function (theme) {
    if (this.Preferences.Theme != theme) {
        this.Preferences.Theme = theme;
        this.update(UserSettings.preferencesName, UserSettings.preferencesKey);
        Service.ChangeTheme(theme);
    }
}

UserSettings.prototype.init = function (name, itemKey) {
    var itemName = 'item' + name;
    if (this.webClientFolder != null) {
        this[itemName] = this.webClientFolder.GetItemByName(itemKey, null);
    }
    if (this[itemName] == null) {
        this[name] = {};
        if (this.webClientFolder != null) {
            this.webClientFolder.InsertItem(Item.Extend({ Name: itemKey, ItemTypeID: ItemTypes.NameValue }), null, null, null);
        }
    } else {
        var value = this[itemName].GetFieldValue(FieldNames.Value);
        this[name] = (value == null) ? {} : $.parseJSON(value);
    }
}

UserSettings.prototype.update = function (name, itemKey) {
    if (this.webClientFolder != null) {
        var itemName = 'item' + name;
        if (this[itemName] == null) {
            this[itemName] = this.webClientFolder.GetItemByName(itemKey, null);
        }
        var updatedItem = this[itemName].Copy();
        updatedItem.SetFieldValue(FieldNames.Value, JSON.stringify(this[name]));
        this[itemName].Update(updatedItem);
    }
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
    Grocery: "00000000-0000-0000-0000-000000000005",
    ShoppingItem: "00000000-0000-0000-0000-000000000008",
    Appointment: "00000000-0000-0000-0000-000000000009",
    // system item types
    System: "00000000-0000-0000-0000-000000000000",
    Reference: "00000000-0000-0000-0000-000000000006",
    NameValue: "00000000-0000-0000-0000-000000000007"
}

// ---------------------------------------------------------
// FieldNames constants

var FieldNames = {
     Name : "Name",                  // String       friendly name (all items have a name)
     Description : "Description",    // String       additional notes or comments
     Priority : "Priority",          // Integer      importance
     Complete : "Complete",          // Boolean      task is complete
     CompletedOn : "CompletedOn",    // DateTime     time at which task is marked complete
     DueDate : "DueDate",            // DateTime     task due or appointment start time
     EndDate : "EndDate",            // DateTime     appointment end time
     Birthday : "Birthday",          // DateTime     user or contact birthday
     Address : "Address",            // Address      address of a location
     WebLink : "WebLink",            // Url          single web links (TODO: NOT BEING USED)
     WebLinks : "WebLinks",          // Json         list of web links [{Name:"name", Url:"link"}, ...] 
     Email : "Email",                // Email        email address 
     Phone : "Phone",                // Phone        phone number (cell phone)
     HomePhone : "HomePhone",        // Phone        home phone 
     WorkPhone : "WorkPhone",        // Phone        work phone
     Amount : "Amount",              // String       quantity (need format for units, etc.)
     Cost : "Cost",                  // Currency     price or cost (need format for different currencies)
     ItemTags : "ItemTags",          // TagIDs       extensible list of tags for marking items
     EntityRef : "EntityRef",        // Guid         id of entity being referenced
     EntityType : "EntityType",      // String       type of entity (User, Folder, or Item)
     Contacts : "Contacts",          // Guid         id of list being referenced which contains contact items
     Locations : "Locations",        // Guid         id of list being referenced which contains location items
     Value : "Value",                // String       value for NameValue items
     Category : "Category",          // String       category (to organize item types e.g. Grocery)
     LatLong : "LatLong",            // String       comma-delimited geo-location lat,long
     FacebookID : "FacebookID",      // String       facebook id for user or contact
     Sources : "Sources",            // String       comma-delimited list of sources of information (e.g. Facebook) 

     Gender : "Gender",              // String       male or female
     Picture : "Picture"             // Url          link to an image
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
    DateTime: "DateTime",
    Phone: "Phone",
    Email : "Email",
    Url : "Url",
    Address : "Address",
    Currency : "Currency",
    TagIDs : "TagIDs",
    Guid : "Guid",
    JSON : "JSON"
}

// ---------------------------------------------------------
// DisplayTypes constants

var DisplayTypes = {
    Hidden: "Hidden",
    Text : "Text",
    TextArea : "TextArea",
    Checkbox : "Checkbox",
    DatePicker : "DatePicker",
    DateTimePicker: "DateTimePicker",
    Phone: "Phone",
    Email : "Email",
    Link : "Link",
    Currency : "Currency",
    Address : "Address",
    Priority : "Priority",
    TagList : "TagList",
    Reference : "Reference",
    Contact: "Contact",
    ContactList: "ContactList",
    LocationList: "LocationList",
    LinkArray : "LinkArray"
}

// ---------------------------------------------------------
// SuggestionTypes constants

var SuggestionTypes = {
    ChooseOne: "ChooseOne",  
    ChooseOneSubject: "ChooseOneSubject",
    ChooseMany: "ChooseMany",
    ChooseManyWithChildren: "ChooseManyWithChildren",
    GetFBConsent: "GetFBConsent",
    GetGoogleConsent: "GetGoogleConsent",
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
    Client: "$Client",
    WebClient: "$WebClient"
}

// ---------------------------------------------------------
// SystemUsers constants

var SystemUsers = {
    // built-in system users
    System: "00000000-0000-0000-0000-000000000001",
    User: "00000000-0000-0000-0000-000000000002"
}

