//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// ListEditor.js

// ---------------------------------------------------------
// ListEditor control
function ListEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;

    this.newItemEditor = new NewItemEditor(this);
    this.listView = new ListView(this);
}

ListEditor.prototype.render = function ($element, list, maxHeight) {
    this.$element = $element;
    var $newItem = this.newItemEditor.render($element, list);
    var newItemHeight = ($newItem != null) ? $newItem.outerHeight() : 0;
    this.listView.render($element, list, maxHeight - newItemHeight - 28);   // exclude top & bottom padding
}

ListEditor.prototype.selectItem = function (item) {
    this.parentControl.fireSelectionChanged(item);
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
    return this.$element;
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

    // TODO: figure out how to append button but keep on one line 100% wide
    //var $append = $('<div class="input-append" />').appendTo($form);
    //var $addButton = $('<span class="add-on"><i class="icon-plus-sign"></i></span>').appendTo($append);

    $field.addClass('input-block-level');
    $field.attr('placeholder', '-- new item --');
    return $field;
}

NewItemEditor.prototype.renderText = function (container, field) {
    $field = $('<input type="text" />').appendTo(container);
    $field.addClass(field.Class);
    $field.data('control', this);
    $field.val(this.newItem.GetFieldValue(field));
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
        if (value == null || value.length == 0) { return false; }

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
// ListView control
function ListView(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.list = null;
}

// static helper for getting attached item from $element
ListView.getItem = function ListView$getItem($element) {
    return $element.parent('a').data('item');
}

ListView.prototype.hide = function () {
    if (this.$element != null) {
        this.$element.hide();
    }
}

ListView.prototype.show = function () {
    if (this.$element != null) {
        this.$element.show();
    }
}

ListView.prototype.render = function ($element, list, height) {
    if (list == null) { return; }
    if (this.$element == null) {
        this.$element = $('<ul class="nav nav-list" />').appendTo($element);
    }
    this.hide();
    this.$element.empty();
    if (height != null) { this.$element.css('max-height', height); }
    if (this.renderListItems(list.GetItems()) > 0) {
        this.show();
        //this.$element.scrollTop(0);
        var $selected = this.$element.find('li.selected');
        var scroll = $selected.offset().top - height + this.$element.scrollTop();
        if (scroll > 0) {
            this.$element.animate({ scrollTop: scroll }, 500);
        }
    }
}

ListView.prototype.renderListItems = function (listItems) {
    var itemCount = 0;
    for (var id in listItems) {
        var item = listItems[id];
        if (!item.IsList) {
            var $li = $('<li />').appendTo(this.$element);
            if (item.IsSelected()) { $li.addClass('selected'); }
            var $item = $('<a class="form-inline" />').appendTo($li);
            $item.data('control', this);
            $item.data('item', item);
            this.renderDeleteBtn($item);
            this.renderNameField($item, item);

            // click item to edit
            $item.bind('click', function (event) {
                if (!$(event.srcElement).hasClass('dt-checkbox')) {
                    var item = $(this).data('item');
                    Control.get(this).parentControl.selectItem(item);
                }
            });

            this.renderFields($item, item);
            itemCount++;
        }
    }
    return itemCount;
}

ListView.prototype.renderNameField = function ($item, item) {
    var fields = item.GetFields();
    // render complete field if exists 
    var field = fields[FieldNames.Complete];
    if (field != null) {
        this.renderCheckbox($item, item, field);
    }
    // render name field
    $item.append(Control.Icons.forSources(item));
    field = fields[FieldNames.Name];
    this.renderLabel($item, item, field);
}

ListView.prototype.renderDeleteBtn = function ($element) {
    var $button = $('<i class="icon-remove-sign pull-right"></i>').appendTo($element);
    $button.attr('title', 'Delete Item').tooltip(Control.ttDelay);
    $button.bind('click', function () {
        var item = ListView.getItem($(this));
        var activeItem = (item.ParentID == null) ? item.GetFolder() : item.GetParent();
        $(this).tooltip('hide');
        item.Delete(activeItem);
        return false;   // do not propogate event
    });
}

ListView.prototype.renderFields = function ($element, item) {
    var fields = item.GetFields();
    for (var name in fields) {
        var field = fields[name];
        this.renderField($element, item, field);
    }
}

ListView.prototype.renderField = function ($element, item, field) {
    var $field;
    var renderByDisplayType = false;

    switch (field.Name) {
        case FieldNames.Name:
            break;
        case FieldNames.DueDate:
            $field = this.renderText($element, item, field, 'Due on ');
            break;
        case FieldNames.Category:
            $field = this.renderText($element, item, field);
            break;
        case FieldNames.Email:
            $field = this.renderEmail($element, item, field);
            break;
        case FieldNames.Address:
            var address = item.GetFieldValue(FieldNames.Address);
            if (address != item.Name) {
                $field = this.renderText($element, item, field);
            }
            break;
        default:
            break;
    }

    if (renderByDisplayType) {
        switch (field.DisplayType) {
            case DisplayTypes.Checkbox:
            case DisplayTypes.Hidden:
            case DisplayTypes.Priority:
            case DisplayTypes.Reference:
            case DisplayTypes.TagList:
                break;
            default:
                $field = this.renderText($element, item, field);
                break;
        }
    }

    return $field;
}

ListView.prototype.renderLabel = function ($element, item, field) {
    var $field;
    var value = item.GetFieldValue(field);
    if (value != null) {
        $field = $('<label style="font-weight:bold;"/>').appendTo($element);
        $field.addClass(field.Class);
        $field.html(value);
    }
    return $field;
}

ListView.prototype.renderText = function ($element, item, field, textBefore, textAfter) {
    var $field;
    var value = item.GetFieldValue(field);
    if (value != null) {
        $field = $('<div />').appendTo($element);
        $field.addClass(field.Class);
        value = ((textBefore == null) ? '' : textBefore) + value + ((textAfter == null) ? '' : textAfter);
        $field.html(value);
    }
    return $field;
}

ListView.prototype.renderEmail = function ($element, item, field) {
    var $field;
    var value = item.GetFieldValue(field);
    if (value != null) {
        var $div = $('<div />').appendTo($element);
        $field = $('<a />').appendTo($div);
        $field.addClass(field.Class);
        $field.attr('href', 'mailto:' + value);
        $field.html(value);
    }
    return $field;
}

ListView.prototype.renderCheckbox = function ($element, item, field) {
    $field = $('<input type="checkbox" class="checkbox" />').appendTo($element);
    $field.addClass(field.Class);
    $field.attr('title', field.DisplayName).tooltip(Control.ttDelay);
    $field.data('control', this);
    if (item.GetFieldValue(field) == 'true') {
        $field.attr('checked', 'checked');
    }
    $field.change(function () { Control.get(this).updateCheckbox($(this), field); });
    return $field;
}

ListView.prototype.updateCheckbox = function ($checkbox, field) {
    var item = ListView.getItem($checkbox);
    if (item != null) {
        var currentValue = item.GetFieldValue(field);
        var value = ($checkbox.attr('checked') == 'checked');
        if (value != currentValue) {
            var updatedItem = item.Copy();
            updatedItem.SetFieldValue(field, value);
            item.Update(updatedItem, null);
        }
    }
}

// ---------------------------------------------------------
// PropertyEditor control
function PropertyEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.list;
}

PropertyEditor.prototype.render = function ($element, list, maxHeight) {
    this.$element = $element.empty();
    var $form = $('<form class="row-fluid form-vertical" />').appendTo(this.$element);
    $form.data('control', this);
    this.list = list;

    // name property
    var $wrapper = $('<div class="control-group"><label class="control-label">Name</label></div>').appendTo($form);
    var $property = $('<input type="text" class="" />').appendTo($wrapper);
    $property.addClass('li-name');
    $property.data('control', this);
    $property.val(this.list.Name);
    $property.change(function (e) { Control.get(this).handleChange($(e.srcElement)); });
    $property.keypress(function (e) { return Control.get(this).handleEnterPress(e); });

    // itemtype property
    Control.ItemTypePicker.render($form, this.list);
}

PropertyEditor.prototype.updateProperty = function ($element) {
    if ($element.hasClass('li-name')) {
        var updatedList = this.list.Copy();
        updatedList.Name = $element.val();
        return updatedList;
    }
    return null;
}

PropertyEditor.prototype.handleChange = function ($element) {
    var updatedList = this.updateProperty($element);
    if (updatedList != null) {
        this.list.Update(updatedList);
    }
}

PropertyEditor.prototype.handleEnterPress = function (e) {
    if (e.which == 13) {
        this.handleChange($(e.srcElement));
        return false;       // do not propogate event
    }
}