//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// FolderManager.js

// ---------------------------------------------------------
// FolderManager control
function FolderManager(parentControl, $parentElement) {
    this.parentControl = parentControl;
    this.$parentElement = $parentElement;
    this.addWell = false;

    this.$element = null;
    this.currentFolder = null;
    this.currentItem = null;
    this.views = {};
    this.listEditor = new ListEditor(this);
}

FolderManager.ListView = "fm-list-view";
FolderManager.ItemView = "fm-item-view";
FolderManager.PropertyView = "fm-property-view";

FolderManager.prototype.hide = function () {
    if (this.$element != null) {
        this.$element.hide();
    }
}

FolderManager.prototype.show = function () {
    if (this.$element == null) {
        this.$element = $('<div class="manager-folders" />').appendTo(this.$parentElement);
        // render tabs
        var $tabs = $('<ul class="nav nav-tabs" />').appendTo(this.$element);
        $tabs.data('control', this);
        var $tab = $('<li><a data-toggle="tab">List View</a></li>').appendTo($tabs);
        $tab.find('a').attr('href', '.' + FolderManager.ListView);
        $tab = $('<li><a data-toggle="tab">Item View</a></li>').appendTo($tabs);
        $tab.find('a').attr('href', '.' + FolderManager.ItemView);
        $tab = $('<li><a data-toggle="tab">Properties</a></li>').appendTo($tabs);
        $tab.find('a').attr('href', '.' + FolderManager.PropertyView);
        // render views
        var $tabContent = $('<div class="tab-content" />').appendTo(this.$element);
        var $view = $('<div class="tab-pane" />').appendTo($tabContent);
        $view.addClass(FolderManager.ListView);
        this.views[FolderManager.ListView] = $view;
        $view = $('<div class="tab-pane" />').appendTo($tabContent);
        $view.addClass(FolderManager.ItemView);
        this.views[FolderManager.ItemView] = $view;
        $view = $('<div class="tab-pane" />').appendTo($tabContent);
        $view.addClass(FolderManager.PropertyView);
        this.views[FolderManager.PropertyView] = $view;

        $('a[data-toggle="tab"]').on('shown', function (e) {
            var $tabs = $(e.target).parents('.nav-tabs');
            Control.get($tabs).viewChanged($(e.target)); ;
        });

        $tabs.find('a[href=".' + this.activeView() + '"]').tab('show');
    }
    this.render();
    this.$element.show();
}

// render is only called internally by show method
FolderManager.prototype.render = function () {
    var $tabs = this.$element.find('.nav-tabs');
    var $tabContent = this.$element.find('.tab-content');
    $tabs.find('li a:first').html(this.activeListName());

    var activeView = this.activeView();
    var $view = this.views[activeView];
    if (activeView == FolderManager.ListView) {
        this.listEditor.render($view, this.activeList());
    }
}

FolderManager.prototype.selectFolder = function (folder) {
    this.currentFolder = folder;
    this.currentItem = null;
    if (this.currentFolder != null) {
        this.render();
    }
}

FolderManager.prototype.selectItem = function (item) {
    this.currentItem = item;
    if (this.currentItem != null) {
        this.currentFolder = this.currentItem.GetFolder();
        this.render();
    } else {
        this.selectFolder(this.currentFolder);
    }
}

FolderManager.prototype.activeList = function () {
    if (this.currentItem != null) {
        return (this.currentItem.IsList) ? this.currentItem :
            (this.currentItem.ParentID == null) ? this.currentItem.GetFolder() :
             this.currentItem.GetParent();
    } else if (this.currentFolder != null) {
        return this.currentFolder;
    }
    return null;
}

FolderManager.prototype.activeListName = function () {
    var activeList = this.activeList();
    return (activeList != null) ? activeList.Name : 'List View';
}

FolderManager.prototype.activeView = function (viewName) {
    var dataModel = Control.findParent(this, 'dataModel').dataModel;
    if (viewName === undefined) {       // get
        return (dataModel.UserSettings.ViewState.ActiveView != null) ? dataModel.UserSettings.ViewState.ActiveView : FolderManager.ListView;
    } else {                            // set
        dataModel.UserSettings.ViewState.ActiveView = viewName;
    }
}

FolderManager.prototype.viewChanged = function ($tab) {
    this.activeView($tab.attr('href').substr(1));
}

// ---------------------------------------------------------
// ListEditor control
function ListEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;

    this.newItemEditor = new NewItemEditor(this);
    this.listView = new ListView(this);
}

ListEditor.prototype.render = function ($element, list) {
    this.$element = $element;
    this.newItemEditor.render($element, list);
    this.listView.render($element, list);
}

ListEditor.prototype.selectItem = function (item) {
    // TEMPORARY - fireSelectionChanged instead
    Dashboard.ManageFolder(item.FolderID, item.ID);
}

// ---------------------------------------------------------
// NewItemEditor control
function NewItemEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.list;
    this.newItem;
}

NewItemEditor.prototype.render = function ($element, list) {
    if (list != null && (list.IsFolder() || list.IsList)) {
        if (this.$element == null) {
            this.$element = $('<div class="row-fluid" />').appendTo($element);
        }
        this.$element.empty();

        this.list = list;
        this.newItem = Item.Extend({ Name: '', ItemTypeID: this.list.ItemTypeID });

        // render name field for new item 
        $field = this.renderNameField(this.$element);
        $field.val('');
        $field.focus();
    }
}

NewItemEditor.prototype.renderNameField = function ($element) {
    // render name field
    var fields = this.newItem.GetFields();
    var nameField = fields[FieldNames.Name];
    var $form = $('<form class="form-inline"/>').appendTo($element);


    var $field = this.renderText($form, nameField);
    // support autocomplete for new Locations and Contacts
    if (this.newItem.ItemTypeID == ItemTypes.Location) {
        this.autoCompleteAddress($field);
    } else if (this.newItem.ItemTypeID == ItemTypes.Contact) {
        this.autoCompleteContact($field);
    }

    $field.addClass('input-block-level');
    $field.attr('placeholder', '-- new item --');
    return $field;
}

NewItemEditor.prototype.renderText = function (container, field) {
    $field = $('<input type="text" />').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.val(this.newItem.GetFieldValue(field));
    //$field.change(function (e) { Control.get(this).handleChange($(e.srcElement)); });
    $field.keypress(function (e) { return Control.get(this).handleEnterPress(e); });
    return $field;
}

NewItemEditor.prototype.autoCompleteAddress = function ($field) {
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
            var editor = Control.get(this);
            if (editor != null) { editor.handleChange($(this)); }
            return false;
        },
        minLength: 2
    });
}

NewItemEditor.prototype.autoCompleteContact = function ($field) {
    $field.autocomplete({
        source: function (request, response) {
            Service.InvokeController('UserInfo', 'PossibleSubjects',
                { 'startsWith': request.term },
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
            $(this).val(ui.item.label);
            $(this).data(FieldNames.Contacts, ui.item.json);
            var editor = Control.get(this);
            if (editor != null) { editor.handleChange($(this)); }
            return false;
        },
        minLength: 1
    });
}

NewItemEditor.prototype.handleChange = function ($element) {
    if (this.updateNameField($element)) {
        this.list.InsertItem(this.newItem);
    }
}

NewItemEditor.prototype.handleEnterPress = function (e) {
    if (e.which == 13) {
        if (this.updateNameField($(e.srcElement))) {
            this.list.InsertItem(this.newItem);
        }
        return false;       // do not propogate event
    }
}

NewItemEditor.prototype.updateNameField = function ($element) {
    var fields = this.newItem.GetFields();
    var nameField = fields[FieldNames.Name];

    if ($element.hasClass(nameField.ClassName)) {
        var value = $element.val();

        if (this.newItem.ItemTypeID == ItemTypes.Location) {
            // autocomplete for new Locations
            var latlong = $element.data(FieldNames.LatLong);
            if (latlong != null) {
                this.newItem.SetFieldValue(FieldNames.Name, value);
                this.newItem.SetFieldValue(FieldNames.Address, value);
                return true;
            } 
        }
        if (this.newItem.ItemTypeID == ItemTypes.Contact) {
            // autocomplete for new Contacts
            var jsonContact = $element.data(FieldNames.Contacts);
            if (jsonContact != null) {
                contact = $.parseJSON(jsonContact);
                if (contact.ItemTypeID == ItemTypes.Contact) {
                    this.newItem = Item.Extend(contact);
                    return true;
                }
            }
        }
        // update name field with new value
        this.newItem.SetFieldValue(nameField, value);
        return true;
    }
    return false;
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
        this.$element = $('<div class="manager-panel row-fluid" />').appendTo(container);
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
            this.item = Item.Extend({ Name: '', ItemTypeID: item.ItemTypeID });
        }
    } else {
        this.mode = ItemEditorMode.Item;
        this.listItem = null;
        this.item = item.Copy();                // deep copy to include FieldValues
    }

    if (this.mode == ItemEditorMode.New) {
        // render name field for new item 
        $field = this.renderNameField(this.$element);
        $field.val('');
    } else {
        // render all fields of item
        this.renderFields(this.$element);
        // display item editor expander
        var $expander = $('<div class="expander pull-right"><i></i></div>').appendTo(this.$element);
        if (this.expanded) {
            $expander.addClass('expanded');
            $expander.find('i').addClass('icon-chevron-up');
        } else {
            $expander.find('i').addClass('icon-chevron-down');
        }
        $expander.data('control', this);
        $expander.click(this.expandEditor);
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

ItemEditor.prototype.renderNameField = function ($element) {
    var fields = this.item.GetFields();
    var field = fields[FieldNames.Complete];
    var $controls = $('<form class="form-inline"/>').appendTo($element);
    if (field != null && this.mode == ItemEditorMode.Item) {
        // render complete field if exists 
        var $prepend = $('<div class="input-prepend" />').appendTo($controls);
        this.renderCheckbox($('<span class="add-on" />').appendTo($prepend), field);
    }
    // render name field
    var $field;
    field = fields[FieldNames.Name];
    if (this.mode == ItemEditorMode.New) {
        // support autocomplete for new Locations and Contacts
        if (this.item.ItemTypeID == ItemTypes.Location) {
            $field = this.renderAddress($controls, field);
        } else if (this.item.ItemTypeID == ItemTypes.Contact) {
            $field = this.renderContactList($controls, field);
        }
    }
    if ($field == null) {
        $field = this.renderText($controls, field);
    }
    if (this.mode == ItemEditorMode.New) {
        $field.addClass('input-block-level');
        $field.attr('placeholder', '-- new item --');
    }
    return $field;
}

ItemEditor.prototype.renderField = function (container, field) {
    // ignore, handled by renderNameField
    if (field.Name == FieldNames.Name || field.Name == FieldNames.Complete)
        return;

    var $field, $wrapper;
    var wrapper = '<div class="control-group"><label class="control-label">' + field.DisplayName + '</label></div>';

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
    $field.keypress(function (event) { return Control.get(this).handleEnterPress(event); });
    return $field;
}

ItemEditor.prototype.renderCheckbox = function (container, field) {
    $field = $('<input type="checkbox" class="checkbox" />').appendTo(container);
    $field.addClass(field.Class);
    $field.attr('title', field.DisplayName).tooltip(Control.ttDelay); ;
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
    $field.keypress(function (event) { return Control.get(this).handleEnterPress(event); });
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
    $field.keypress(function (event) { return Control.get(this).handleEnterPress(event); });
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
    $field.keypress(function (event) { return Control.get(this).handleEnterPress(event); });
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
                                this.item = Item.Extend(contact);
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
            contact = Item.Extend(contact);
            var fbID = contact.GetFieldValue(FieldNames.FacebookID);
            var existingContact = dataModel.FindContact(contact.Name, fbID);
            if (existingContact != null) {
                // add reference to existing contact
                this.item.AddReference(field, contact, true);
                return;
            }
        } else {
            contact = Item.Extend({ Name: contactName, ItemTypeID: ItemTypes.Contact });
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
        var newLocation = Item.Extend({ Name: address, ItemTypeID: ItemTypes.Location });
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
        return false;       // do not propogate event
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
    $field.keypress(function (event) { return Control.get(this).handleEnterPress(event); });

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
        if (itemType.UserID == null || itemType.UserID == dataModel.User.ID) {
            $('<option value="' + id + '">' + itemType.Name + '</option>').appendTo($itemTypePicker);
        }
    }
    $itemTypePicker.val(this.listItem.ItemTypeID);
    $itemTypePicker.combobox({ selected: function () { Control.get(this).updateListInfo($(this)); } });
}

ItemEditor.prototype.updateListInfo = function ($element) {
    this.listItem = (this.listItem.IsFolder()) ? Folder.Extend(this.listItem) : Item.Extend(this.listItem);
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