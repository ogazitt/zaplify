//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// FolderManager.js

// ---------------------------------------------------------
// FolderManager control
function FolderManager(dataModel) {
    this.dataModel = dataModel;
    this.currentFolder = null;
    this.currentItem = null;

    // keep this in-sync with div generated in render
    // used by children to delay-render on first show
    this.container = '.manager-region';
    this.$managerRegion = null;

    this.listEditor = new ListEditor(this);
    this.managerHelp = new ManagerHelp(this);
    this.managerSettings = new ManagerSettings(this);
}

FolderManager.prototype.render = function (container) {
    if (this.$managerRegion == null) {
        $(container).empty();

        // manager region
        this.$managerRegion = $('<div class="manager-region"></div>').appendTo($(container));

        // list editor
        this.listEditor.render(this.container);
    }
}

FolderManager.prototype.selectFolder = function (folder) {
    this.currentFolder = folder;
    this.currentItem = null;
    if (this.currentFolder != null) {
        this.managerHelp.hide();
        this.managerSettings.hide();
        this.listEditor.render(this.container);
        this.listEditor.show();
    } else {
        this.listEditor.hide();
        this.managerSettings.hide();
        this.managerHelp.show();
    }
}

FolderManager.prototype.selectItem = function (item) {
    this.currentItem = item;
    if (this.currentItem != null) {
        this.currentFolder = this.currentItem.GetFolder();
        this.managerHelp.hide();
        this.managerSettings.hide();
        this.listEditor.render(this.container);
        this.listEditor.show();
    } else {
        this.selectFolder(this.currentFolder);
    }
}

FolderManager.prototype.selectSettings = function () {
    this.listEditor.hide();
    this.managerHelp.hide();
    this.managerSettings.show();
}

// ---------------------------------------------------------
// ManagerHelp control
function ManagerHelp(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
}

ManagerHelp.prototype.render = function (container) {
    this.$element = $('<div class="manager-help"><h1>Welcome to Zaplify!</h1></div>').appendTo($(container));
}

ManagerHelp.prototype.hide = function () {
    if (this.$element != null) {
        this.$element.hide();
    }
}

ManagerHelp.prototype.show = function () {
    if (this.$element == null) {
        this.render(this.parentControl.container);
    }
    this.$element.show();
}

// ---------------------------------------------------------
// ManagerSettings control
function ManagerSettings(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
}

ManagerSettings.prototype.render = function (container) {
    this.$element = $('<div class="manager-settings"></div>').appendTo($(container));
    this.$element.append('<div class="manager-header ui-state-active"><span>User Preferences</span></div>');
    var $settings = $('<div class="manager-panel ui-widget-content"></div>').appendTo(this.$element);
    this.renderThemePicker($settings);
}

ManagerSettings.prototype.hide = function () {
    if (this.$element != null) {
        this.$element.hide();
    }
}

ManagerSettings.prototype.show = function () {
    if (this.$element == null) {
        this.render(this.parentControl.container);
    }
    this.$element.show();
}

ManagerSettings.prototype.renderThemePicker = function (container) {
    var dataModel = Control.findParent(this, 'dataModel').dataModel;
    var themes = dataModel.Constants.Themes;
    var currentTheme = dataModel.UserSettings.Preferences.Theme;
    var $wrapper = $('<div class="ui-widget setting"><label>Theme </label></div>').appendTo(container);

    var $themePicker = $('<select></select>').appendTo($wrapper);
    for (var i in themes) {
        var $option = $('<option value="' + themes[i] + '">' + themes[i] + '</option>').appendTo($themePicker);
    }
    $themePicker.val(currentTheme);
    $themePicker.combobox({ selected: function () {
            var theme = $(this).val();
            dataModel.UserSettings.UpdateTheme(theme);
        } 
    });
}

// ---------------------------------------------------------
// ListEditor control
function ListEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;

    this.itemPath = new ItemPath(this);
    this.itemEditor = new ItemEditor(this);
}

ListEditor.prototype.render = function (container) {
    var folder = this.parentControl.currentFolder;
    var item = this.parentControl.currentItem;
    if (this.$element == null) {
        this.$element = $('<div class="list-editor"></div>').appendTo(container);
    }

    this.itemPath.render(this.$element, folder, item);
    this.itemEditor.render((item == null) ? folder : item, this.$element);
}

ListEditor.prototype.hide = function () {
    this.$element.hide();
}

ListEditor.prototype.show = function () {
    this.$element.show();
}

ListEditor.prototype.activeItem = function () {
    // TODO: track active editor
    var item = this.itemEditor.activeItem();
    return item;
}

ListEditor.prototype.addFolder = function () {
    var newFolder = this.activeItem();

    // TODO: let user define ItemType (temporary for test purposes)
    var dataModel = this.parentControl.dataModel;
    newFolder.ItemTypeID = dataModel.FoldersMap.array[1].ItemTypeID;
    dataModel.InsertFolder(newFolder);
}

ListEditor.prototype.addItem = function (isList) {
    var folder = this.parentControl.currentFolder;
    var item = this.parentControl.currentItem;
    var newItem = this.activeItem();
    newItem.IsList = isList;

    if (item != null) {
        item.InsertItem(newItem);
    } else if (folder != null) {
        folder.InsertItem(newItem);
    }
}

ListEditor.prototype.updateItem = function () {
    var item = this.parentControl.currentItem;
    var updatedItem = this.activeItem();
    if (item != null) {
        // TEMPORARY: until List editor is added
        if (item.IsList) {
            var updatedItem = $.extend({}, item);
            updatedItem.Name = this.$element.find('.fn-name').val();
        }

        item.Update(updatedItem);
    }
}

ListEditor.prototype.deleteItem = function () {
    var item = this.parentControl.currentItem;
    if (item != null) {
        // WARN USER when deleting a List
        if (item.IsList && !confirm('Are you sure?\n\nThis will delete the list and all items contained within!')) {
            return;
        }
        item.Delete();
    } else {
        if (this.parentControl.currentFolder.IsDefault) {
            alert('This is a default folder and cannot be deleted.');
        } else if (confirm('Are you sure?\n\nThis will delete the folder and all items contained within!')) {
            // WARN USER when deleting a Folder
            this.parentControl.currentFolder.Delete();
        }
    }
}

// ---------------------------------------------------------
// ItemPath control
function ItemPath(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
}

ItemPath.prototype.render = function (container, folder, item) {
    if (this.$element == null) {
        this.$element = $('<div class="manager-header ui-widget ui-state-active"></div>').appendTo(container);
    }

    var addBtnTitle;
    var deleteBtnTitle;
    this.$element.empty();
    if (folder != null) {
        this.$element.append('<span>' + folder.Name + '</span>');
        if (!folder.IsDefault) { deleteBtnTitle = 'Delete Folder'; }
        if (item == null) { addBtnTitle = 'Add List'; }
    }
    if (item != null) {
        deleteBtnTitle = 'Delete Item';
        if (item.IsList) {
            this.$element.append('<span> : ' + item.Name + '</span>');
            deleteBtnTitle = 'Delete List';
        } else if (item.ParentID != null) {
            this.$element.append('<span> : ' + item.GetParent().Name + '</span>');
        }
    }

    if (deleteBtnTitle != null) {
        var handler = this.parentControl;
        var $deleteBtn = $('<div class="icon delete-icon"></div>').appendTo(this.$element);
        $deleteBtn.attr('title', deleteBtnTitle);
        $deleteBtn.unbind('click');
        $deleteBtn.bind('click', function () { handler.deleteItem(); });
    }
    if (addBtnTitle != null) {
        var handler = this.parentControl;
        var $addBtn = $('<div class="icon add-icon"></div>').appendTo(this.$element);
        $addBtn.attr('title', addBtnTitle);
        $addBtn.unbind('click');
        $addBtn.bind('click', function () { handler.addItem(true); });
    }
}

ItemPath.prototype.hide = function () {
    this.$element.hide();
}

ItemPath.prototype.show = function () {
    this.$element.show();
}

// ---------------------------------------------------------
// ItemEditor control
function ItemEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.newItem = false;
    this.expanded = false;
    this.item;
}

ItemEditor.prototype.hide = function () {
    this.$element.hide();
}

ItemEditor.prototype.show = function () {
    this.$element.show();
}

ItemEditor.prototype.activeItem = function () {
    return this.item;
}

ItemEditor.prototype.render = function (item, container) {
    if (item == null) { return; }
    if (this.$element == null) {
        this.$element = $('<div class="manager-panel ui-widget-content"></div>').appendTo(container);
    }

    this.newItem = item.IsFolder() || item.IsList;
    this.item = (this.newItem) ? $.extend(new Item(), { Name: '', ItemTypeID: item.ItemTypeID }) : $.extend(new Item(), item);

    this.$element.empty();
    if (this.newItem) {
        $field = this.renderNameField(this.$element);
        $field.val('');    
    } else {
        // display item editor expander
        var $expander = $('<div class="item-editor-expander ui-icon"></div>').appendTo(this.$element);
        var iconClass = (this.expanded) ? 'expanded ui-icon-arrowreturnthick-1-n' : 'ui-icon-arrowreturnthick-1-s';
        $expander.addClass(iconClass);
        $expander.data('control', this);
        $expander.click(this.expandHandler);
        // render all fields
        this.renderFields(this.$element);
    }

    $fldActive = this.$element.find('.fn-name');
    $fldActive.focus();
    //$fldActive.select();
}

ItemEditor.prototype.expandHandler = function () {
    var itemEditor = Control.get(this);
    itemEditor.expanded = !$(this).hasClass('expanded');
    itemEditor.render(itemEditor.item);
}

ItemEditor.prototype.renderFields = function (container) {
    this.renderNameField(container);
    var fields = this.item.GetItemType().Fields;
    for (var name in fields) {
        var field = fields[name];
        if (field.IsPrimary || this.expanded) {
            this.renderField(container, field);
        }
    }
}

ItemEditor.prototype.renderNameField = function (container) {
    var fields = this.item.GetItemType().Fields;
    var field = fields[FieldNames.Complete];
    var $wrapper = $('<div class="item-field wide"></div>').appendTo(container);
    if (field != null && !this.newItem) {
        // render complete field if exists and not new item
        $wrapper.addClass('checked');
        this.renderCheckbox($wrapper, field);
    }
    // render name field
    var $field;
    field = fields[FieldNames.Name];
    if (this.newItem) {
        if (this.item.ItemTypeID == ItemTypes.Location) {
            $field = this.renderAddress($wrapper, field);
        } else if (this.item.ItemTypeID == ItemTypes.Contact) {
            $field = this.renderContactList($wrapper, field);
        }
    }
    if ($field == null) {
        $field = this.renderText($wrapper, field);
    }
    return $field;
}

ItemEditor.prototype.renderField = function (container, field) {
    // ignore, handled by renderNameField
    if (field.Name == FieldNames.Name || field.Name == FieldNames.Complete)
        return;

    var $field, $wrapper;
    var wrapper = '<div class="item-field"><span class="item-field-label">' + field.DisplayName + '</span></div>';

    switch (field.DisplayType) {
        case DisplayTypes.Hidden:
        case DisplayTypes.Priority:
        case DisplayTypes.Reference:
        case DisplayTypes.TagList:
            break;
        case DisplayTypes.ContactList:
            $wrapper = $(wrapper).appendTo(container);
            $wrapper.addClass('wide label');
            $field = this.renderContactList($wrapper, field);
            break;
        case DisplayTypes.LocationList:
            $wrapper = $(wrapper).appendTo(container);
            $wrapper.addClass('wide label');
            $field = this.renderLocationList($wrapper, field);
            break;
        case DisplayTypes.Address:
            $wrapper = $(wrapper).appendTo(container);
            $wrapper.addClass('wide label');
            $field = this.renderAddress($wrapper, field);
            break;
        case DisplayTypes.UrlList:
            $wrapper = $(wrapper).appendTo(container);
            $wrapper.addClass('wide area');
            $field = this.renderUrlList($wrapper, field);
            break;
        case DisplayTypes.DateTimePicker:
            $wrapper = $(wrapper).appendTo(container);
            $field = this.renderDateTimePicker($wrapper, field);
            break;
        case DisplayTypes.DatePicker:
            $wrapper = $(wrapper).appendTo(container);
            $field = this.renderDatePicker($wrapper, field);
            break;
        case DisplayTypes.TextArea:
            $wrapper = $(wrapper).appendTo(container);
            $wrapper.addClass('wide area');
            $field = this.renderTextArea($wrapper, field);
            break;
        case DisplayTypes.Checkbox:
            $wrapper = $(wrapper).appendTo(container);
            $wrapper.find('.item-field-label').addClass('inline');
            $field = this.renderCheckbox($wrapper, field);
            break;
        case DisplayTypes.Text:
        default:
            $wrapper = $(wrapper).appendTo(container);
            $field = this.renderText($wrapper, field);
            break;
    }
    return $field;
}

ItemEditor.prototype.renderText = function (container, field) {
    $field = $('<input type="text" />').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.val(this.item.GetFieldValue(field));
    $field.change(function (event) { Control.get(this).handleChange($(event.srcElement)); });
    $field.keypress(function (event) { Control.get(this).handleEnterPress(event); });
    return $field;
}

ItemEditor.prototype.renderCheckbox = function (container, field) {
    $field = $('<input type="checkbox" />').appendTo(container);
    $field.addClass(field.Class);
    $field.attr('title', field.DisplayName);
    $field.data('control', this);
    if (this.item.GetFieldValue(field) == 'true') {
        $field.attr('checked', 'checked');
    }
    $field.change(function (event) { Control.get(this).handleChange($(event.srcElement)); });
    return $field;
}

ItemEditor.prototype.renderTextArea = function (container, field) {
    $field = $('<textarea></textarea>').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.val(this.item.GetFieldValue(field));
    $field.change(function (event) { Control.get(this).handleChange($(event.srcElement)); });
    return $field;
}

ItemEditor.prototype.renderDatePicker = function (container, field) {
    $field = $('<input type="text" />').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.val(this.item.GetFieldValue(field));
    $field.datepicker({   
        numberOfMonths: 2,
        onClose: function (value, picker) {
            itemEditor = Control.get(this);
            if (itemEditor != null) { itemEditor.handleChange(picker.input); }
        }
    });
    return $field;
}

ItemEditor.prototype.renderDateTimePicker = function (container, field) {
    $field = $('<input type="text" />').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.val(this.item.GetFieldValue(field));
    $field.datetimepicker({
        ampm: true,
        timeFormat: 'h:mm TT',
        hourGrid: 4,
        minuteGrid: 10,
        stepMinute: 5, 
        numberOfMonths: 2,
        onClose: function (value, picker) {
            itemEditor = Control.get(this);
            if (itemEditor != null) {  itemEditor.handleChange(picker.input); }
        }
    });
    return $field;
}

ItemEditor.prototype.renderUrlList = function (container, field) {
    $field = $('<textarea></textarea>').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    // TODO: process links into text for display
    $field.val(this.item.GetFieldValue(field));
    $field.change(function (event) { Control.get(this).handleChange($(event.srcElement)); });
    return $field;
}

ItemEditor.prototype.renderAddress = function (container, field) {
    $field = $('<input type="text" />').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.keypress(function (event) { Control.get(this).handleEnterPress(event); });
    $field.val(this.item.GetFieldValue(field));
    $field.autocomplete({
        source: function (request, response) {
            Service.Geocoder().geocode({ 'address': request.term },
                function (results, status) {
                    if (status == google.maps.GeocoderStatus.OK) {
                        var addresses = $.map(results, function (item) {
                            return {
                                label: item.formatted_address,
                                value: item.formatted_address,
                                latlong: item.geometry.location.toUrlValue()
                            }
                        });
                        response(addresses);
                    }
                });
        },
        select: function (event, ui) {
            $(this).val(ui.item.label);
            $(this).data(FieldNames.LatLong, ui.item.latlong);
            itemEditor = Control.get(this);
            if (itemEditor != null) { itemEditor.handleChange($(this)); }
            return false;
        },
        minLength: 3
    });
    return $field;
}

ItemEditor.prototype.renderLocationList = function (container, field) {
    $field = $('<input type="text" />').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.keypress(function (event) { Control.get(this).handleEnterPress(event); });
    var text = '';
    var value = this.item.GetFieldValue(field);
    if (value != null && value.IsList) {
        var dataModel = Control.findParent(this, 'dataModel').dataModel;
        var locations = value.GetItems();
        for (var id in locations) {
            var locationRef = locations[id].GetFieldValue(FieldNames.ItemRef);
            var address = locationRef.GetFieldValue(FieldNames.Address);
            text += address;
            if (locationRef.Name != address) {
                text += ' ( ' + locationRef.Name + ' )';
            }
            // TODO: support multiple locations
            //text += '; ';
            break;
        }
    }
    $field.val(text);

    var split = function (val) { return val.split(/;\s*/); }
    var lastTerm = function (term) { return split(term).pop(); }
    $field.autocomplete({
        source: function (request, response) {
            Service.Geocoder().geocode({ 'address': lastTerm(request.term) },
                function (results, status) {
                    if (status == google.maps.GeocoderStatus.OK) {
                        var addresses = $.map(results, function (item) {
                            return {
                                label: item.formatted_address,
                                value: item.formatted_address,
                                latlong: item.geometry.location.toUrlValue()
                            }
                        });
                        response(addresses);
                    }
                });
        },
        select: function (event, ui) {
            // multi-selection support
            var terms = split(this.value);
            terms.pop();                        // remove the current input
            terms.push(ui.item.label);          // add the selected item
            terms.push("");                     // placeholder for separator
            this.value = terms.join("; ");      // add separator

            $(this).val(ui.item.label);
            $(this).data(FieldNames.LatLong, ui.item.latlong);
            itemEditor = Control.get(this);
            if (itemEditor != null) { itemEditor.handleChange($(this)); }
            return false;
        },
        minLength: 3
    });
    return $field;
}

ItemEditor.prototype.renderContactList = function (container, field) {
    $field = $('<input type="text" />').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.keypress(function (event) { Control.get(this).handleEnterPress(event); });
    var text = '';
    var value = this.item.GetFieldValue(field);
    if (value != null && value.IsList) {
        var dataModel = Control.findParent(this, 'dataModel').dataModel;
        var contacts = value.GetItems();
        for (var id in contacts) {
            var contactRef = contacts[id].GetFieldValue(FieldNames.ItemRef);
            text += contactRef.Name;
            // TODO: support multiple contacts
            //text += '; ';
            break;
        }
    }
    $field.val(text);

    var split = function (val) { return val.split(/;\s*/); }
    var lastTerm = function (term) { return split(term).pop(); }
    $field.autocomplete({
        source: function (request, response) {
            Service.InvokeController('UserInfo', 'PossibleSubjects',
                { 'startsWith': lastTerm(request.term) },
                function (responseState) {
                    var result = responseState.result;
                    var contacts = [];
                    if (result.Count > 0) {
                        for (var name in result.Subjects) {
                            contacts.push({ label: name, value: name, json: result.Subjects[name] });
                        }
                    }
                    response(contacts);
                });
        },
        select: function (event, ui) {
            // multi-selection support
            var terms = split(this.value);
            terms.pop();                        // remove the current input
            terms.push(ui.item.label);          // add the selected item
            terms.push("");                     // placeholder for separator
            this.value = terms.join("; ");      // add separator

            $(this).val(ui.item.label);
            $(this).data(FieldNames.Contacts, ui.item.json);
            itemEditor = Control.get(this);
            if (itemEditor != null) { itemEditor.handleChange($(this)); }
            return false;
        },
        minLength: 1
    });
    return $field;
}

ItemEditor.prototype.updateField = function ($element) {
    var fields = this.item.GetItemType().Fields;
    for (var name in fields) {
        var field = fields[name];
        if ($element.hasClass(field.ClassName)) {
            var changed = false;
            var currentValue = this.item.GetFieldValue(field);
            var value;
            var displayType = field.DisplayType;

            if (this.newItem) {
                if (this.item.ItemTypeID == ItemTypes.Location) { displayType = DisplayTypes.Address; }
                if (this.item.ItemTypeID == ItemTypes.Contact) { displayType = DisplayTypes.Contact; }
            }

            switch (displayType) {
                case DisplayTypes.Hidden:
                case DisplayTypes.Priority:
                case DisplayTypes.Reference:
                case DisplayTypes.TagList:
                case DisplayTypes.ContactList:
                    this.updateContactList($element, field);
                    break;
                case DisplayTypes.LocationList:
                    this.updateLocationList($element, field);
                    break;
                case DisplayTypes.UrlList:
                    break;
                case DisplayTypes.Checkbox:
                    value = ($element.attr('checked') == 'checked');
                    changed = (value != currentValue);
                    break;
                case DisplayTypes.Contact:
                    if (this.newItem) {
                        value = $element.val();
                        var jsonContact = $element.data(FieldNames.Contacts);
                        if (jsonContact != null) {
                            contact = $.parseJSON(jsonContact);
                            if (contact.ItemTypeID == ItemTypes.Contact) {
                                this.item = $.extend(new Item(), contact);
                            }
                        }
                        changed = true;
                    }
                    break;
                case DisplayTypes.Address:
                    if (this.newItem) {
                        this.item.SetFieldValue(FieldNames.Address, $element.val());
                    }
                    var latlong = $element.data(FieldNames.LatLong);
                    if (latlong != null) {
                        var currentLatLong = this.item.GetFieldValue(FieldNames.LatLong);
                        if (currentLatLong == null || currentLatLong != latlong) {
                            this.item.SetFieldValue(FieldNames.LatLong, latlong);
                            value = $element.val();
                            changed = true;
                        }
                    } else {
                        value = $element.val();
                        changed = (value != currentValue);
                    }
                    break;

                case DisplayTypes.DatePicker:
                case DisplayTypes.DateTimePicker:
                case DisplayTypes.TextArea:
                case DisplayTypes.Text:
                default:
                    value = $element.val();
                    changed = (value != currentValue);
                    break;
            }

            if (changed) {
                this.item.SetFieldValue(field, value);
                return true;
            }
            break;
        }
    }
    return false;
}

ItemEditor.prototype.updateContactList = function ($element, field) {
    var contact;
    var contactName = $element.val();
    if (contactName == null || contactName.length == 0) {
        this.item.RemoveReferences(field);
        return;
    }

    var jsonContact = $element.data(FieldNames.Contacts);
    if (jsonContact != null) {
        contact = $.parseJSON(jsonContact);
    }

    if (this.newItem) {
        if (contact != null && contact.ItemTypeID == ItemTypes.Contact) {
            this.item = $.extend(new Item(), contact);
        } else {
            this.item.Name = contactName;
        }
        return true;
    }


    var dataModel = Control.findParent(this, 'dataModel').dataModel;
    if (contact != null && contact.ItemTypeID == ItemTypes.Reference) {
        // add reference to existing contact
        contact = { Name: contact.Name, ID: contact.FieldValues[0].Value };
        this.item.AddReference(field, contact, true);
    } else {
        if (contact != null) {
            contact = $.extend(new Item(), contact);
            var fbID = contact.GetFieldValue(FieldNames.FacebookID);
            var existingContact = dataModel.FindContact(contact.Name, fbID);
            if (existingContact != null) {
                // add reference to existing contact
                this.item.AddReference(field, contact, true);
                return;
            }
        } else {
            contact = $.extend(new Item(), { Name: contactName, ItemTypeID: ItemTypes.Contact });
        }
        // create new contact and add reference
        var thisItem = this.item;
        var contactFolder = dataModel.FindDefault(ItemTypes.Contact);
        dataModel.InsertItem(contact, contactFolder, null, null, null,
            function (insertedContact) {
                thisItem.AddReference(field, insertedContact, true);
            });
    }
}

ItemEditor.prototype.updateLocationList = function ($element, field) {
    var address = $element.val();
    if (address == null || address.length == 0) {
        this.item.RemoveReferences(field);
        return;
    }

    var latlong = $element.data(FieldNames.LatLong);
    var dataModel = Control.findParent(this, 'dataModel').dataModel;
    var existingLocation = dataModel.FindLocation(address, latlong);
    if (existingLocation != null) {
        // add reference to existing location
        this.item.AddReference(field, existingLocation, true);
    } else {
        // create new location and add reference
        var locationFolder = dataModel.FindDefault(ItemTypes.Location);
        var newLocation = $.extend(new Item(), { Name: address, ItemTypeID: ItemTypes.Location });
        newLocation.SetFieldValue(FieldNames.Address, address);
        if (latlong != null) {
            newLocation.SetFieldValue(FieldNames.LatLong, latlong);
        }
        var thisItem = this.item;
        dataModel.InsertItem(newLocation, locationFolder, null, null, null,
            function (insertedLocation) {
                thisItem.AddReference(field, insertedLocation, true);
            });
    } 
}

ItemEditor.prototype.handleChange = function ($element) {
    if (this.updateField($element)) {
        if (this.newItem) {
            this.parentControl.addItem(false);
        } else {
            this.parentControl.updateItem();
        } 
    }
}

ItemEditor.prototype.handleEnterPress = function (event) {
    if (event.which == 13) {
        if (this.updateField($(event.srcElement))) {
            if (this.newItem) {
                this.parentControl.addItem(false);
            } else {
                this.parentControl.updateItem();
            }
        }
    }
}
