//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// ItemEditor.js

// ---------------------------------------------------------
// ItemEditor control
function ItemEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.expanded = true;
    this.originalItem;
    this.item;
}

ItemEditor.prototype.hide = function () {
    this.$element.hide();
}

ItemEditor.prototype.show = function () {
    this.$element.show();
}

ItemEditor.prototype.render = function ($element, item) {
    if (item != null && !(item.IsFolder() || item.IsList)) {
        if (this.$element == null) {
            this.$element = $('<form class="row-fluid form-vertical carousel" />').appendTo($element);
            this.$element.data('control', this);
        }
        this.$element.empty();

        this.originalItem = item;
        this.item = item.Copy();
        this.siblings = this.item.GetParentContainer().GetItems(true);  // exclude sibling list items
        // render all fields of item
        this.renderFields(this.$element);

        if (ItemMap.count(this.siblings) > 1) {      // render navigator if more than one item in list
            $('<a class="page-turn-control left" data-slide="prev"><div class="page-turn-prev small"></div></a>').appendTo(this.$element);
            $('<a class="page-turn-control right" data-slide="next"><div class="page-turn-next small"></div></a>').appendTo(this.$element); ;
            this.$element.find('.page-turn-control').click(function (e) {
                var $element = $(e.target);
                var control = Control.get($element.parents('.carousel').first());
                control.renderNextItem($element);
                e.preventDefault();
            });
            this.$element.find('.carousel-control').dblclick(function (e) { e.preventDefault(); });
        }

        // display field expander
        /*
        var $expander = $('<div class="btn expander pull-right"><i></i></div>').appendTo(this.$element);
        if (this.expanded) {
        $expander.addClass('expanded');
        $expander.find('i').addClass('icon-chevron-up');
        } else {
        $expander.find('i').addClass('icon-chevron-down');
        }
        $expander.data('control', this);
        $expander.click(this.expandEditor);
        */

        // TODO: handle focus properly
        var $fldActive = this.$element.find('.fn-name');
        $fldActive.focus();
        //$fldActive.select();
    }
}

ItemEditor.prototype.expandEditor = function () {
    var itemEditor = Control.get(this);
    itemEditor.expanded = !$(this).hasClass('expanded');
    itemEditor.render(null, itemEditor.item);
}

ItemEditor.prototype.renderNextItem = function ($element) {
    var direction = $element.attr('data-slide');
    var index = ItemMap.indexOf(this.siblings, this.item.ID);
    var lastIndex = ItemMap.count(this.siblings) - 1;
    if (direction == 'next') {
        index = (index == lastIndex) ? 0 : index + 1;
    } else {
        index = (index == 0) ? lastIndex : index - 1;
    }
    var nextItem = ItemMap.itemAt(this.siblings, index);
    this.parentControl.fireSelectionChanged(nextItem);
}

ItemEditor.prototype.renderFields = function ($element) {
    this.renderNameField($element);
    var fields = this.item.GetFields();
    for (var name in fields) {
        var field = fields[name];
        if (field.IsPrimary || this.expanded) {
            this.renderField($element, field);
        }
    }
}

ItemEditor.prototype.renderNameField = function ($element) {
    var inputClass = 'input-block-level';
    var fields = this.item.GetFields();
    var field = fields[FieldNames.Complete];
    var $controls = $('<form class="form-inline"/>').appendTo($element);
    if (field != null) {
        // render complete field if exists 
        var $prepend = $('<div class="input-prepend" />').appendTo($controls);
        var $addon = $('<span class="add-on" />').appendTo($prepend);
        Control.Checkbox.render($addon, this.originalItem, field);
        inputClass = 'input-inline-max';
    }

    // render name field
    var $field;
    field = fields[FieldNames.Name];
    $field = this.renderText($controls, field);
    $field.addClass(inputClass);
    return $field;
}

ItemEditor.prototype.renderField = function ($element, field) {
    // handled by renderNameField
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
            $wrapper = $(wrapper).appendTo($element);
            $field = this.renderContactList($wrapper, field);
            break;
        case DisplayTypes.LocationList:
            $wrapper = $(wrapper).appendTo($element);
            $field = this.renderLocationList($wrapper, field);
            break;
        case DisplayTypes.Address:
            $wrapper = $(wrapper).appendTo($element);
            $field = this.renderAddress($wrapper, field);
            break;
        case DisplayTypes.LinkArray:
            $wrapper = $(wrapper).appendTo($element);
            $field = this.renderLinkArray($wrapper, field);
            break;
        case DisplayTypes.DateTimePicker:
            $wrapper = $(wrapper).appendTo($element);
            $field = this.renderDateTimePicker($wrapper, field);
            break;
        case DisplayTypes.DatePicker:
            $wrapper = $(wrapper).appendTo($element);
            $field = this.renderDatePicker($wrapper, field);
            break;
        case DisplayTypes.TextArea:
            $wrapper = $(wrapper).appendTo($element);
            $field = this.renderTextArea($wrapper, field);
            break;
        case DisplayTypes.Checkbox:
            $wrapper = $(wrapper).appendTo($element);
            $field = Control.Checkbox.render($wrapper, this.originalItem, field);
            break;
        case DisplayTypes.Text:
        default:
            $wrapper = $(wrapper).appendTo($element);
            $field = this.renderText($wrapper, field);
            break;
    }
    if ($field != null) { $field.addClass('input-block-level'); }
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

            switch (field.DisplayType) {
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
                case DisplayTypes.Address:
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
    if (this.updateField($element)) {
        this.originalItem.Update(this.item);
    }
}

ItemEditor.prototype.handleEnterPress = function (e) {
    if (e.which == 13) {
        this.handleChange($(e.srcElement));
        return false;       // do not propogate event
    }
}