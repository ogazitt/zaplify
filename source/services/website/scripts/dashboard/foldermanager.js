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
    this.$element = null;

    this.listEditor = new ListEditor(this);
    this.managerHelp = new ManagerHelp(this);
    this.managerSettings = new ManagerSettings(this);
}

FolderManager.prototype.render = function (container) {
    if (this.$element == null) {
        $(container).empty();
        // manager region
        this.$element = $('<div class="manager-region" />').appendTo($(container));
    }

    // list editor
    this.listEditor.render(this.$element);
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
    this.$element = $('<div class="manager-settings" />').appendTo($(container));
    var $header = $('<div class="manager-header ui-state-active"><label>User Preferences</label></div>').appendTo(this.$element);
    var $settings = $('<div class="manager-panel ui-widget-content" />').appendTo(this.$element);
    this.renderThemePicker($settings);

    // close button
    var $closeBtn = $('<div class="ui-icon ui-icon-circle-close" />').appendTo($header);
    $closeBtn.data('control', this);
    $closeBtn.attr('title', 'Close');
    $closeBtn.click(function () {
        var control = Control.get(this);
        control.parentControl.selectItem(control.parentControl.currentItem); 
    });
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

    var $themePicker = $('<select />').appendTo($wrapper);
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
    this.itemViewer = new ItemViewer(this);
}

ListEditor.prototype.render = function (container) {
    var folder = this.parentControl.currentFolder;
    var item = this.parentControl.currentItem;
    if (this.$element == null) {
        this.$element = $('<div class="list-editor" />').appendTo(container);
    }

    var selection = (item == null) ? folder : item;
    this.itemViewer.hide();
    this.itemPath.closeDelegate = null;
    this.itemPath.render(this.$element, folder, item);
    this.itemEditor.render(selection, this.$element);
    if (selection != null && (selection.IsFolder() || selection.IsList)) {
        // calculate and set height for itemViewer
        var viewerHeight = this.$element.outerHeight() - this.itemPath.$element.outerHeight();
        if (this.itemEditor.$element != null) { viewerHeight -= this.itemEditor.$element.outerHeight(); }
        this.itemViewer.show(viewerHeight);
        // display items
        this.itemViewer.render(selection, this.$element);
    }
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

ListEditor.prototype.selectItem = function (item) {
    $('#' + item.ID).click();
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
    var updatedItem = this.activeItem();
    if (updatedItem.IsFolder() && this.parentControl.currentFolder != null) {
        this.parentControl.currentFolder.Update(updatedItem);
    } else if (this.parentControl.currentItem != null) {
        this.parentControl.currentItem.Update(updatedItem);
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
        if (this.parentControl.currentFolder.IsDefault()) {
            alert('This is a default folder and cannot be deleted.');
        } else if (confirm('Are you sure?\n\nThis will delete the folder and all items contained within!')) {
            // WARN USER when deleting a Folder
            this.parentControl.currentFolder.Delete();
        }
    }
}

ListEditor.prototype.showListInfo = function (show) {
    if (show == null) { show = false; }
    if (show) {
        this.itemPath.closeDelegate = Control.delegate(this, 'showListInfo');
        this.itemPath.render(this.$element, this.parentControl.currentFolder, this.parentControl.currentItem);
        this.itemEditor.mode = ItemEditorMode.List;
        this.itemEditor.render(this.itemEditor.listItem);
    } else {
        this.itemPath.closeDelegate = null;
        this.itemEditor.mode = ItemEditorMode.New;
        this.render();
    }
}

// ---------------------------------------------------------
// ItemPath control
function ItemPath(parentControl) {
    this.parentControl = parentControl;
    this.closeDelegate = null;
    this.$element = null;
}

ItemPath.prototype.render = function (container, folder, item) {
    if (this.$element == null) {
        this.$element = $('<div class="manager-header ui-widget ui-state-active" />').appendTo(container);
    }

    var addBtnTitle;
    var deleteBtnTitle;
    var infoBtnTitle;
    this.$element.empty();
    var $label = $('<label />').appendTo(this.$element);
    if (folder != null) {
        $label.append('<span>' + folder.Name + '</span>');
        if (!folder.IsDefault()) { deleteBtnTitle = 'Delete Folder'; }
        if (item == null) {
            addBtnTitle = 'Add List';
            infoBtnTitle = 'Folder Properties';
        }

    }
    if (item != null) {
        deleteBtnTitle = 'Delete Item';
        if (item.IsList) {
            $label.append('<span> : ' + item.Name + '</span>');
            deleteBtnTitle = 'Delete List';
            infoBtnTitle = 'List Properties';
        } else if (item.ParentID != null) {
            $label.append('<span> : ' + item.GetParent().Name + '</span>');
        }
    }

    if (this.closeDelegate != null) {
        // only display close icon and invoke delegate if close is clicked
        var $closeBtn = $('<div class="ui-icon ui-icon-circle-close" />').appendTo(this.$element);
        $closeBtn.data('control', this);
        $closeBtn.attr('title', 'Close');
        $closeBtn.bind('click', function () { Control.get(this).closeDelegate.invoke(); });
        return;
    }

    if (deleteBtnTitle != null) {
        var $deleteBtn = $('<div class="ui-icon delete-icon" />').appendTo(this.$element);
        $deleteBtn.data('control', this);
        $deleteBtn.attr('title', deleteBtnTitle);
        $deleteBtn.bind('click', function () { Control.get(this).parentControl.deleteItem(); });
    }
    if (addBtnTitle != null) {
        var $addBtn = $('<div class="ui-icon add-icon" />').appendTo(this.$element);
        $addBtn.data('control', this);
        $addBtn.attr('title', addBtnTitle);
        $addBtn.bind('click', function () { Control.get(this).parentControl.addItem(true); });
    }
    if (infoBtnTitle != null) {
        var $infoBtn = $('<div class="ui-icon info-icon" />').appendTo(this.$element);
        $infoBtn.data('control', this);
        $infoBtn.attr('title', infoBtnTitle);
        $infoBtn.bind('click', function () { Control.get(this).parentControl.showListInfo(true); });
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
    this.mode = ItemEditorMode.New;
    this.expanded = false;
    this.listItem;
    this.item;
}

ItemEditorMode = { New: 0, Item: 1, List: 2 };

ItemEditor.prototype.hide = function () {
    this.$element.hide();
}

ItemEditor.prototype.show = function () {
    this.$element.show();
}

ItemEditor.prototype.activeItem = function () {
    return (this.mode == ItemEditorMode.List) ? this.listItem : this.item;
}

ItemEditor.prototype.render = function (item, container) {
    if (item == null) { return; }
    if (this.$element == null) {
        this.$element = $('<div class="manager-panel ui-widget-content" />').appendTo(container);
    }
    this.$element.empty();

    if (item.IsFolder() || item.IsList) {
        if (this.mode == ItemEditorMode.List && item.ID == this.listItem.ID) {
            // render list info editor
            this.renderListInfo(this.$element);
            return;
        } else {
            this.mode = ItemEditorMode.New;
            this.listItem = item;
            this.item = $.extend(new Item(), { Name: '', ItemTypeID: item.ItemTypeID });
        }
    } else {
        this.mode = ItemEditorMode.Item;
        this.listItem = null;
        this.item = $.extend(true, new Item(), item);   // must be deep copy for updating FieldValues
    }

    if (this.mode == ItemEditorMode.New) {
        // render name field for new item 
        $field = this.renderNameField(this.$element);
        $field.val('');
    } else {
        // display item editor expander
        var $expander = $('<div class="expander"><span class="ui-icon" /><span class="ui-icon" /><span class="ui-icon" /></div>').appendTo(this.$element);
        if (this.expanded) {
            $expander.addClass('expanded');
            //$expander.attr('title', 'Less');
            $expander.find('.ui-icon').addClass('ui-icon-carat-1-n');
        } else {
            //$expander.attr('title', 'More');
            $expander.find('.ui-icon').addClass('ui-icon-carat-1-s');
        }
        $expander.data('control', this);
        $expander.click(this.expandEditor);
        // render all fields of item
        this.renderFields(this.$element);
    }
    // TODO: handle focus properly
    $fldActive = this.$element.find('.fn-name');
    $fldActive.focus();
    //$fldActive.select();
}

ItemEditor.prototype.expandEditor = function () {
    var itemEditor = Control.get(this);
    itemEditor.expanded = !$(this).hasClass('expanded');
    itemEditor.render(itemEditor.item);
}

ItemEditor.prototype.renderFields = function (container) {
    this.renderNameField(container);
    var fields = this.item.GetFields();
    for (var name in fields) {
        var field = fields[name];
        if (field.IsPrimary || this.expanded) {
            this.renderField(container, field);
        }
    }
}

ItemEditor.prototype.renderNameField = function (container) {
    var fields = this.item.GetFields();
    var field = fields[FieldNames.Complete];
    var $wrapper = $('<div class="item-field wide" />').appendTo(container);
    if (field != null && this.mode == ItemEditorMode.Item) {
        // render complete field if exists 
        $wrapper.addClass('checked');
        this.renderCheckbox($wrapper, field);
    }
    // render name field
    var $field;
    field = fields[FieldNames.Name];
    if (this.mode == ItemEditorMode.New) {
        // support autocomplete for new Locations and Contacts
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
        case DisplayTypes.LinkArray:
            $wrapper = $(wrapper).appendTo(container);
            $wrapper.addClass('wide area');
            $field = this.renderLinkArray($wrapper, field);
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

ItemEditor.prototype.renderLinkArray = function (container, field) {
    $field = $('<textarea></textarea>').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    var linkArray = new LinkArray(this.item.GetFieldValue(field));
    $field.val(linkArray.ToText());
    $field.change(function (event) { Control.get(this).handleChange($(event.srcElement)); });
    return $field;
}

ItemEditor.prototype.renderAddress = function (container, field) {
    $field = $('<input type="text" />').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.keypress(function (event) { Control.get(this).handleEnterPress(event); });
    if (this.mode == ItemEditorMode.New) {
        $field.change(function (event) { Control.get(this).updateField($(this)); });
    }
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
            var locationRef = locations[id].GetFieldValue(FieldNames.EntityRef);
            if (locationRef != null) {
                var address = locationRef.GetFieldValue(FieldNames.Address);
                text += address;
                if (locationRef.Name != address) {
                    text += ' ( ' + locationRef.Name + ' )';
                }
                //text += '; ';             // TODO: support multiple locations
            }
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
    if (this.mode == ItemEditorMode.New) {
        $field.change(function (event) { Control.get(this).updateField($(this)); });
    }
    var text = '';
    var value = this.item.GetFieldValue(field);
    if (value != null && value.IsList) {
        var dataModel = Control.findParent(this, 'dataModel').dataModel;
        var contacts = value.GetItems();
        for (var id in contacts) {
            var contactRef = contacts[id].GetFieldValue(FieldNames.EntityRef);
            if (contactRef != null) {
                text += contactRef.Name;
                //text += '; ';                 // TODO: support multiple contacts
            }
            break;
        }
    }
    $field.val(text);

    var split = function (val) { return val.split(/;\s*/); }
    var lastTerm = function (term) { return split(term).pop(); }
    $field.autocomplete({
        source: function (request, response) {
            Service.InvokeController('UserInfo', 'PossibleContacts',
                { 'startsWith': lastTerm(request.term) },
                function (responseState) {
                    var result = responseState.result;
                    var contacts = [];
                    if (result.Count > 0) {
                        for (var name in result.Contacts) {
                            contacts.push({ label: name, value: name, json: result.Contacts[name] });
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
    var fields = this.item.GetFields();
    for (var name in fields) {
        var field = fields[name];
        if ($element.hasClass(field.ClassName)) {
            var changed = false;
            var currentValue = this.item.GetFieldValue(field);
            var value;
            var displayType = field.DisplayType;

            if (this.mode == ItemEditorMode.New) {
                // support autocomplete for new Locations and Contacts
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
                case DisplayTypes.LinkArray:
                    var linkArray = new LinkArray();
                    linkArray.Parse($element.val());
                    value = linkArray.ToJson();
                    changed = (value != currentValue);
                    break;
                case DisplayTypes.Checkbox:
                    value = ($element.attr('checked') == 'checked');
                    changed = (value != currentValue);
                    break;
                case DisplayTypes.Contact:
                    if (this.mode == ItemEditorMode.New) {
                        // autocomplete for new Contacts
                        value = $element.val();
                        var jsonContact = $element.data(FieldNames.Contacts);
                        if (jsonContact != null) {
                            contact = $.parseJSON(jsonContact);
                            if (contact.ItemTypeID == ItemTypes.Contact) {
                                this.item = $.extend(new Item(), contact);
                                this.parentControl.addItem(false);
                                return false;
                            }
                        }
                        changed = true;
                    }
                    break;
                case DisplayTypes.Address:
                    var latlong = $element.data(FieldNames.LatLong);
                    if (latlong != null) {
                        var currentLatLong = this.item.GetFieldValue(FieldNames.LatLong);
                        if (currentLatLong == null || currentLatLong != latlong) {
                            this.item.SetFieldValue(FieldNames.LatLong, latlong);
                            value = $element.val();
                            changed = true;
                            if (this.mode == ItemEditorMode.New) {
                                // autocomplete for new Locations
                                this.item.SetFieldValue(FieldNames.Name, value);
                                this.item.SetFieldValue(FieldNames.Address, value);
                                this.parentControl.addItem(false);
                                return false;
                            }
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
        var contactList = dataModel.UserSettings.GetDefaultList(ItemTypes.Contact);
        dataModel.InsertItem(contact, contactList, null, null, null,
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
        var locationList = dataModel.UserSettings.GetDefaultList(ItemTypes.Location);
        var newLocation = $.extend(new Item(), { Name: address, ItemTypeID: ItemTypes.Location });
        newLocation.SetFieldValue(FieldNames.Address, address);
        if (latlong != null) {
            newLocation.SetFieldValue(FieldNames.LatLong, latlong);
        }
        var thisItem = this.item;
        dataModel.InsertItem(newLocation, locationList, null, null, null,
            function (insertedLocation) {
                thisItem.AddReference(field, insertedLocation, true);
            });
    }
}

ItemEditor.prototype.handleChange = function ($element) {
    if (this.mode == ItemEditorMode.List && this.updateListInfo($element)) {
        this.parentControl.updateItem();
    }
    if (this.updateField($element) && this.mode == ItemEditorMode.Item) {
        this.parentControl.updateItem();
    }
}

ItemEditor.prototype.handleEnterPress = function (event) {
    if (event.which == 13) {
        if (this.mode == ItemEditorMode.List && this.updateListInfo($(event.srcElement))) {
            this.parentControl.updateItem();
        }

        if (this.updateField($(event.srcElement))) {
            if (this.mode == ItemEditorMode.New) {
                this.parentControl.addItem(false);
            } else {
                this.parentControl.updateItem();
            }
        }
    }
}

ItemEditor.prototype.renderListInfo = function (container) {
    var listType = (this.listItem.IsFolder()) ? 'Folder' : 'List';
    // Name field
    var label = 'Name of ' + listType;
    var $wrapper = $('<div class="item-field"><span class="item-field-label">' + label + '</span></div>').appendTo(container);
    var $field = $('<input type="text" />').appendTo($wrapper);
    $field.addClass('li-name');
    $field.data('control', this);
    $field.val(this.listItem.Name);
    $field.change(function (event) { Control.get(this).handleChange($(event.srcElement)); });
    $field.keypress(function (event) { Control.get(this).handleEnterPress(event); });

    // ItemTypeID property
    label = 'Kind of ' + listType;
    $wrapper = $('<div class="setting"><label>' + label + '</label></div>').appendTo(container);
    var $itemTypePicker = $('<select />').appendTo($wrapper);
    $itemTypePicker.addClass('li-type');
    $itemTypePicker.data('control', this);

    var dataModel = Control.findParent(this, 'dataModel').dataModel;
    var itemTypes = dataModel.Constants.ItemTypes;
    for (var id in itemTypes) {
        var itemType = itemTypes[id];
        if (itemType.UserID == '00000000-0000-0000-0000-000000000002' || itemType.UserID == dataModel.User.ID) {
            $('<option value="' + id + '">' + itemType.Name + '</option>').appendTo($itemTypePicker);
        }
    }
    $itemTypePicker.val(this.listItem.ItemTypeID);
    $itemTypePicker.combobox({ selected: function () { Control.get(this).updateListInfo($(this)); } });
}

ItemEditor.prototype.updateListInfo = function ($element) {
    this.listItem = (this.listItem.IsFolder()) ? $.extend(new Folder(), this.listItem) : $.extend(new Item(), this.listItem);
    var value = $element.val();
    if ($element.hasClass('li-name') && this.listItem.Name != value) {
        this.listItem.Name = value;
        return true;
    }
    if ($element.hasClass('li-type')) {
        this.listItem.ItemTypeID = value;
        this.parentControl.updateItem();
    }
    return false;
}