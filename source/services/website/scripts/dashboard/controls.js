//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// Controls.js


// ---------------------------------------------------------
// Control static object
// shared helpers used by controls
var Control = function Control$() {};
Control.noDelay = { delay: { show: 0, hide: 0} };           // tooltip with no delay
Control.ttDelay = { delay: { show: 500, hide: 200} };       // default tooltip delay

// helper function for preventing event bubbling
Control.preventDefault = function Control$preventDefault(e) { e.preventDefault(); }

// helpers for creating and invoking a delegate
Control.delegate = function Control$delegate(object, funcName) {
    var delegate = { object: object, handler: funcName };
    delegate.invoke = function () { return this.object[this.handler](); };
    return delegate;
}

// get the control object associated with the element
Control.get = function Control$get(element) {
    return $(element).data('control');
}

// get first parent control that contains member
Control.findParent = function Control$findParent(control, member) {
    while (control.parentControl != null) {
        control = control.parentControl;
        if (control[member] != null) {
            return control;
        }
    }
    return null;
}

// expand an element
Control.expand = function Control$expand($element, animate, callback) {
    if (animate == true) {
        $element.show('blind', { direction: 'vertical' }, 400, callback);   // animated
    } else {
        $element.collapse('show');
    }
}
// collapse an element
Control.collapse = function Control$collapse($element, animate, callback) {
    if (animate == true) {
        $element.hide('blind', { direction: 'vertical' }, 300, callback);   // animated
    } else {
        $element.collapse('hide');
    }
}

Control.alert = function Control$alert(message, header, handlerClose) {
    var $modalMessage = $('#modalMessage');
    if ($modalMessage.length == 0) {
        message.replace('<br\>', '\r');
        alert(message);
        if (handlerClose != null) { handlerClose(); }
    } else {
        if (header == null) { header = "Warning!"; }
        $modalMessage.find('.modal-header h3').html(header);
        $modalMessage.find('.modal-body p').html(message);
        $modalMessage.modal('show');
        if (handlerClose != null) {
            $modalMessage.find('.modal-header .close').click(function () { handlerClose(); });
            $modalMessage.find('.modal-footer .btn-primary').click(function () { handlerClose(); });
        }
    }
}

Control.confirm = function Control$confirm(message, header, handlerOK, handlerCancel) {
    var $modalPrompt = $('#modalPrompt');
    if ($modalPrompt.length == 0) {
        message.replace('<br\>', '\r');
        if (confirm(message) == true) {
            if (handlerOK != null) { handlerOK(); }
        } else {
            if (handlerCancel != null) { handlerCancel(); }
        }
    } else {
        if (header == null) { header = 'Confirm?'; }
        $modalPrompt.find('.modal-header h3').html(header);
        $modalPrompt.find('.modal-body p').html(message);
        $modalPrompt.modal({ backdrop: 'static', keyboard: false });
        $modalPrompt.find('.modal-footer .btn-primary').click(function () {
            $modalPrompt.modal('hide');
            if (handlerOK != null) { handlerOK(); }
        });
        $modalPrompt.find('.modal-footer .btn-cancel').click(function () {
            $modalPrompt.modal('hide');
            if (handlerCancel != null) { handlerCancel(); }
        });
    }
}


// ---------------------------------------------------------
// Browser static object
// get information about browser
var Browser = function Browser$() { };
Browser.IsMSIE = function Browser$IsMSIE() { return (navigator.appName == 'Microsoft Internet Explorer'); }
Browser.MSIE_Version = function Browser$MSIE_Version() {
    var version = -1;                                           // return -1 if not MSIE
    if (Browser.IsMSIE) {
        var regex = new RegExp("MSIE ([0-9]{1,}[\.0-9]{0,})");
        if (regex.exec(navigator.userAgent) != null) {
            version = parseFloat(RegExp.$1);
        }
    }
    return version;
}

// ---------------------------------------------------------
// Control.Icons static object
//
Control.Icons = {};

// return an element containing icons for item sources
Control.Icons.forSources = function Control$Icons$forSources(item) {
    var $icons = $('<span />');
    if (item.HasField(FieldNames.Sources)) {
        var sources = item.GetFieldValue(FieldNames.Sources);
        if (sources != null) {
            sources = sources.split(",");
            for (var i in sources) {
                switch (sources[i]) {
                    case "Facebook":
                        var fbID = item.GetFieldValue(FieldNames.FacebookID);
                        var $fbLink = $('<i class="icon-facebook-sign" />').appendTo($icons);
                        if (fbID != null) {
                            $fbLink.click(function () { window.open('http://www.facebook.com/' + fbID); return false; });
                        }
                        break;
                    case "Directory":
                        $icons.append('<i class="icon-azure" />');
                        break;
                }
            }
        } else if (item.ItemTypeID == ItemTypes.Contact) {
            $icons.append('<i class="icon-user"></i>');
        }
    }
    return $icons;
}

// return an element that is an icon for the item type
Control.Icons.forItemType = function Control$Icons$forItemType(item) {
    // allow parameter as Item or ItemTypeID
    var itemType = item;
    if (typeof (item) == 'object') {
        itemType = item.ItemTypeID;
    }

    var $icon = $('<i></i>');
    switch (itemType) {
        case ItemTypes.Task:
            $icon.addClass('icon-check');
            break;
        case ItemTypes.Appointment:
            $icon.addClass('icon-calendar');
            break;
        case ItemTypes.Contact:
            $icon.addClass('icon-user');
            break;
        case ItemTypes.Location:
            $icon.addClass('icon-map-marker');
            break;
        case ItemTypes.Grocery:
            $icon.addClass('icon-shopping-cart');
            break;
        case ItemTypes.ShoppingItem:
            $icon.addClass('icon-tag');
            break;
        case ItemTypes.ListItem:
        default:
            $icon.addClass('icon-list-alt');
            break;
    }
    return $icon;
}

// return an element that is an icon for a map link
Control.Icons.forMap = function Control$Icons$forMap(item) {
    var json = item.GetFieldValue(FieldNames.WebLinks);
    if (json != null && json.length > 0) {
        var links = new LinkArray(json).Links();
        for (var i in links) {
            var link = links[i];
            if (link.Name == 'Map' && link.Url != null) {
                var $link = $('<i class="icon-map-marker"></i>');
                $link.attr('href', link.Url);
                $link.attr('title', 'Map').tooltip(Control.ttDelay);
                $link.click(function () { window.open($(this).attr('href')); return false; });
                return $link;
            }
        }
    }
    return $('<i></i>');
}

// return an element that is an icon for deleting an item
Control.Icons.deleteBtn = function Control$Icons$deleteBtn(item) {
    var $icon = $('<i class="icon-remove-sign"></i>');
    $icon.css('cursor', 'pointer');
    $icon.data('item', item);
    $icon.attr('title', 'Delete Item').tooltip(Control.noDelay);
    $icon.bind('click', function () {
        var $this = $(this);
        $this.tooltip('hide');
        // don't delete if in middle of sorting
        var sorting = $this.parents('.ui-sortable-helper').length > 0;
        if (!sorting) {
            var item = $this.data('item');
            var activeItem = (item.ParentID == null) ? item.GetFolder() : item.GetParent();
            item.Delete(activeItem);
        }
        return false;   // do not propogate event
    });
    // wrap in anchor tag to get tooltips to work in Chrome
    return $('<a class="icon" />').append($icon);
}

// ---------------------------------------------------------
// Control.Text static object
//
Control.Text = {};

// render text in tag (span is default)
Control.Text.render = function Control$Text$render($element, item, field, tag, textBefore, textAfter) {
    var tag = (tag == null) ? 'span' : tag;
    var $tag;
    var value = item.GetFieldValue(field);
    if (field.DisplayType == DisplayTypes.DatePicker) {
        value = Control.DateTime.format(value);
    } else if (field.DisplayType == DisplayTypes.DateTimePicker) {
        value = Control.DateTime.format(value);
    }

    if (value != null) {
        $tag = $('<' + tag + '/>').appendTo($element);
        $tag.addClass(field.Class);
        value = ((textBefore == null) ? '' : textBefore) + value + ((textAfter == null) ? '' : textAfter);
        $tag.html(value);
    }
    return $tag;
}
// render label strong
Control.Text.renderLabel = function Control$Text$renderLabel($element, item, field) {
    var $label;
    var value = item.GetFieldValue(field);
    if (value != null) {
        $label = $('<label><strong>' + value + '</strong></label>').appendTo($element);
        $label.addClass(field.Class);
    }
    return $label;
}
// render email link
Control.Text.renderEmail = function Control$Text$renderEmail($element, item, field) {
    var $link;
    var value = item.GetFieldValue(field);
    if (value != null) {
        $link = $('<a />').appendTo($element);
        $link.addClass(field.Class);
        $link.attr('href', 'mailto:' + value);
        $link.html(value);
    }
    return $link;
}
// render input with update onchange and onkeypress
Control.Text.renderInput = function Control$Text$renderInput($element, item, field) {
    $text = $('<input type="text" />').appendTo($element);
    $text = Control.Text.base($text, item, field);
    Control.Text.placeholder($text, field);
    $text.change(function (e) { Control.Text.update($(e.srcElement)); });
    $text.keypress(function (e) { if (e.which == 13) { Control.Text.update($(e.srcElement)); return false; } });
    return $text;
}
// render input with insert into list onkeypress (autocomplete based on ItemType)
Control.Text.renderInputNew = function Control$Text$renderInput($element, item, field, list) {
    $text = $('<input type="text" />').appendTo($element);
    $text = Control.Text.base($text, item, field);
    $text.data('list', list);       // list to insert into
    $text.keypress(function (e) { if (e.which == 13) { Control.Text.insert($(e.srcElement)); return false; } });
    // support autocomplete for new Locations and Contacts
    if (item.ItemTypeID == ItemTypes.Location) {
        //Control.Text.autoCompleteAddress($text, Control.Text.insert);
        Control.Text.autoCompletePlace($text, Control.Text.insert);
    } else if (item.ItemTypeID == ItemTypes.Contact) {
        Control.Text.autoCompleteContact($text, Control.Text.insert);
        Control.Text.placeholder($text, 'Enter a contact');
    } else if (item.ItemTypeID == ItemTypes.Grocery) {
        Control.Text.autoCompleteGrocery($text, Control.Text.insert);
        Control.Text.placeholder($text, 'Enter an item');
    } else if (item.ItemTypeID == ItemTypes.Task) {
        Control.Text.placeholder($text, 'Enter a task');
    } else if (item.ItemTypeID == ItemTypes.Appointment) {
        Control.Text.placeholder($text, 'Enter an appointment');
    } else {
        Control.Text.placeholder($text, 'Enter an item');
    }
    return $text;
}
// render textarea with update onchange
Control.Text.renderTextArea = function Control$Text$renderTextArea($element, item, field) {
    $text = $('<textarea></textarea>').appendTo($element);
    $text = Control.Text.base($text, item, field);
    Control.Text.placeholder($text, field);
    $text.change(function (e) { Control.Text.update($(e.srcElement)); });
    return $text;
}
// render input with update on keypress and autocomplete
Control.Text.renderInputAddress = function Control$Text$renderInputAddress($element, item, field) {
    $text = $('<input type="text" />').appendTo($element);
    $text = Control.Text.base($text, item, field);
    $text.keypress(function (e) { if (e.which == 13) { Control.Text.updateAddress($(e.srcElement)); return false; } });
    //$text = Control.Text.autoCompleteAddress($text, Control.Text.updateAddress);
    $text = Control.Text.autoCompletePlace($text, Control.Text.updateAddress);
    return $text;
}
// render input with update on keypress and autocomplete
Control.Text.renderInputGrocery = function Control$Text$renderInputGrocery($element, item, field) {
    $text = $('<input type="text" />').appendTo($element);
    $text = Control.Text.base($text, item, field);
    $text.keypress(function (e) { if (e.which == 13) { Control.Text.updateGrocery($(e.srcElement)); return false; } });
    $text = Control.Text.autoCompleteGrocery($text, Control.Text.updateGrocery);
    return $text;
}
// attach place autocomplete behavior to input element
Control.Text.autoCompletePlace = function Control$Text$autoCompletePlace($input, selectHandler) {
    $input.unbind('keypress');
    $text.keypress(function (e) { if (e.which == 13) { return false; } });
    var autoComplete = new google.maps.places.Autocomplete($input[0]);

    // TODO: temporary bound to Seattle area (calculate bounds from UserProfile GeoLocation)
    var getBounds = function (lat, lng) {
        var x = 0.5;
        return new google.maps.LatLngBounds(new google.maps.LatLng(lat - x, lng - x), new google.maps.LatLng(lat + x, lng + x));
    }
    var seattleArea = getBounds(47.65, -122.3);
    autoComplete.setBounds(seattleArea);

    google.maps.event.addListener(autoComplete, 'place_changed', function () {
        $input.data('place', autoComplete.getPlace());
        selectHandler($input);
    });
    return $input;
}
// attach address autocomplete behavior to input element
Control.Text.autoCompleteAddress = function Control$Text$autoCompleteAddress($input, selectHandler) {
    $input.autocomplete({
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
        select: function (e, ui) {
            $(this).val(ui.item.label);
            $(this).data(FieldNames.LatLong, ui.item.latlong);
            selectHandler($(this));
            return false;
        },
        minLength: 2
    });
    return $input;
}
// attach contact autocomplete behavior to input element
Control.Text.autoCompleteContact = function Control$Text$autoCompleteContact($input, selectHandler) {
    $input.autocomplete({
        source: function (request, response) {
            Service.InvokeController('UserInfo', 'PossibleContacts',
                { 'startsWith': request.term },
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
            $(this).val(ui.item.label);
            $(this).data(FieldNames.Contacts, ui.item.json);
            selectHandler($(this));
            return false;
        },
        minLength: 1
    }); 
    return $input;
}
// attach grocery autocomplete behavior to input element
Control.Text.autoCompleteGrocery = function Control$Text$autoCompleteGrocery($input, selectHandler) {
    $input.autocomplete({
        source: function (request, response) {
            Service.InvokeController('Grocery', 'GroceryNames',
                { 'startsWith': request.term },
                function (responseState) {
                    var result = responseState.result;
                    var groceries = [];
                    if (result.Count > 0) {
                        for (var i in result.Groceries) {
                            var item = result.Groceries[i];
                            groceries.push({ label: item.Name, value: item.Name, json: item });
                        }
                    }
                    response(groceries);
                });
        },
        select: function (event, ui) {
            $(this).val(ui.item.label);
            $(this).data('grocery', ui.item.json);
            selectHandler($(this));
            return false;
        },
        minLength: 1
    });
    return $input;
}

// handler for inserting new item into list
Control.Text.insert = function Control$Text$insert($input) {
    var field = $input.data('field');
    if (field.Name == FieldNames.Name) {
        var value = $input.val();
        if (value == null || value.length == 0) { return false; }

        var item = $input.data('item');         // item to insert
        var list = $input.data('list');         // list to insert into
        if (item.ItemTypeID == ItemTypes.Location) {
            // autocomplete for new Locations
            var place = $input.data('place');
            var latlong = $input.data(FieldNames.LatLong);
            if (place != null && place.geometry != null) {
                item = Control.Text.applyPlace(item, place);
                value = place.name;
            } else if (latlong != null) {
                item.SetFieldValue(FieldNames.LatLong, latlong);
                item.SetFieldValue(FieldNames.Address, value);
            }
        }
        if (item.ItemTypeID == ItemTypes.Contact) {
            // autocomplete for new Contacts
            var jsonContact = $input.data(FieldNames.Contacts);
            if (jsonContact != null) {
                contact = $.parseJSON(jsonContact);
                if (contact.ItemTypeID == ItemTypes.Contact) {
                    item = Item.Extend(contact);
                }
            }
        }
        if (item.ItemTypeID == ItemTypes.Grocery) {
            // autocomplete for new Grocery
            var grocery = $input.data('grocery');
            if (grocery != null) {
                Control.Text.applyGrocery(item, grocery);
                value = grocery.Name;
            }
        }

        item.SetFieldValue(field, value);
        list.InsertItem(item);
    }
}
// handler for updating text
Control.Text.update = function Control$Text$update($input, activeItem) {
    var item = $input.data('item');
    var field = $input.data('field');
    var value = $input.val();
    var currentValue = item.GetFieldValue(field);
    if (field.FieldType == FieldTypes.DateTime) {
        // store DateTime fields in UTC format
        value = Control.DateTime.formatUTC(value);
    }
    if (value != currentValue) {
        var updatedItem = item.Copy();
        updatedItem.SetFieldValue(field, value);
        item.Update(updatedItem, activeItem);
    }
}
// handler for updating address
Control.Text.updateAddress = function Control$Text$updateAddress($input) {
    var item = $input.data('item');
    var field = $input.data('field');
    var value = $input.val();
    var currentValue = item.GetFieldValue(field);
    var updatedItem = item.Copy();
    var place = $input.data('place');
    var latlong = $input.data(FieldNames.LatLong);
    if (place != null && place.geometry != null) {
        updatedItem = Control.Text.applyPlace(updatedItem, place, field.Name);
    } else if (latlong != null) {
        var currentLatLong = item.GetFieldValue(FieldNames.LatLong);
        if (currentLatLong == null || currentLatLong != latlong) {
            updatedItem.SetFieldValue(FieldNames.LatLong, latlong);
            updatedItem.SetFieldValue(field, value);
        }
    } else if (value != currentValue) {
        updatedItem.SetFieldValue(field, value);
    }
    item.Update(updatedItem);
}
// helper function for applying place properties to an item
Control.Text.applyPlace = function Control$Text$applyPlace(item, place, fieldName) {
    if (item.Name == null || fieldName == FieldNames.Name) { item.SetFieldValue(FieldNames.Name, place.name); }
    item.SetFieldValue(FieldNames.LatLong, place.geometry.location.toUrlValue());
    item.SetFieldValue(FieldNames.Address, place.formatted_address);
    if (place.formatted_phone_number != null && item.GetFieldValue(FieldNames.Phone) == null) {
        item.SetFieldValue(FieldNames.Phone, place.formatted_phone_number);
    }
    var links = item.GetFieldValue(FieldNames.WebLinks);
    if (links == null || links == '[]') {
        var weblinks = new LinkArray();
        if (place.types[0] == 'street_address') {
            weblinks.Add('http://maps.google.com/maps?z=15&t=m&q=' + place.formatted_address, 'Map');
        } else {
            weblinks.Add(place.url, 'Map');
        }
        if (place.website != null) { weblinks.Add(place.website, 'Website'); }
        item.SetFieldValue(FieldNames.WebLinks, weblinks.ToJson());
    }
    return item;
}
// handler for updating grocery
Control.Text.updateGrocery = function Control$Text$updateGrocery($input) {
    var item = $input.data('item');
    var field = $input.data('field');
    var value = $input.val();
    var currentValue = item.GetFieldValue(field);
    var updatedItem = item.Copy();
    var grocery = $input.data('grocery');
    if (grocery != null) {
        updatedItem = Control.Text.applyGrocery(updatedItem, grocery, field.Name);
    } else if (value != currentValue) {
        updatedItem.SetFieldValue(field, value);
    }
    item.Update(updatedItem);
}
// helper function for applying grocery properties to an item
Control.Text.applyGrocery = function Control$Text$applyGrocery(item, grocery, fieldName) {
    if (item.Name == null || fieldName == FieldNames.Name) { item.SetFieldValue(FieldNames.Name, grocery.Name); }
    item.SetFieldValue(FieldNames.Category, grocery.Category);
    item.SetFieldValue(FieldNames.Picture, grocery.ImageUrl);
    return item;
}
// base function for applying class, item, field, and value to element
Control.Text.base = function Control$Text$base($element, item, field) {
    $element.addClass(field.Class);
    $element.data('item', item);
    $element.data('field', field);
    $element.val(item.GetFieldValue(field));
    return $element;
}
// add placeholder attribute to an input element, work-around in IE
Control.Text.placeholder = function Control$Text$placeholder($input, placeholdertext) {
    if (typeof (placeholdertext) == 'object') {     // may pass field object to get a default placeholder
        placeholdertext = 'Enter ' + placeholdertext.DisplayName.toLowerCase();
    }

    $input.attr('placeholder', placeholdertext);
    if (Browser.IsMSIE()) {
        if ($input.val() == null || $input.val().length == 0) {
            $input.val(placeholdertext);
            $input.addClass('ie-placeholder');
        }

        $input.focus(function () {
            var $this = $(this);
            if ($this.val() == $this.attr('placeholder')) {
                $this.val('');
                $this.removeClass('ie-placeholder');
            }
        });
        $input.blur(function () {
            var $this = $(this);
            if ($this.val() == null || $this.val().length == 0) {
                $this.val($this.attr('placeholder'));
                $this.addClass('ie-placeholder');
            }
        });
    }
}


// ---------------------------------------------------------
// Control.Checkbox static object
//
Control.Checkbox = {};
Control.Checkbox.render = function Control$Checkbox$render($element, item, field) {
    $checkbox = $('<input type="checkbox" class="checkbox" />').appendTo($element);
    $checkbox.addClass(field.Class);
    $checkbox.attr('title', field.DisplayName).tooltip(Control.ttDelay);
    if (item.GetFieldValue(field) == true) {
        $checkbox.attr('checked', 'checked');
    }
    $checkbox.data('item', item);
    $checkbox.data('field', field);
    $checkbox.change(function () { Control.Checkbox.update($(this)); });
    return $checkbox;
}

Control.Checkbox.update = function Control$Checkbox$update($checkbox) {
    var item = $checkbox.data('item');
    var field = $checkbox.data('field');
    var value = ($checkbox.attr('checked') == 'checked');
    var currentValue = item.GetFieldValue(field);
    if (value != currentValue) {
        $checkbox.tooltip('hide');
        var updatedItem = item.Copy();
        updatedItem.SetFieldValue(field, value);
        // when Complete field is set true, timestamp CompletedOn field
        if (value == true && field.Name == FieldNames.Complete && item.HasField(FieldNames.CompletedOn)) {
            updatedItem.SetFieldValue(FieldNames.CompletedOn, new Date().format());
        }
        item.Update(updatedItem);
    }
}

// ---------------------------------------------------------
// Control.LinkArray static object
//
Control.LinkArray = {};
Control.LinkArray.render = function Control$LinkArray$render($element, item, field) {
    var $wrapper = $('<div class="link-array" />').appendTo($element);
    $text = $('<input type="text" />').appendTo($wrapper);
    $text = Control.Text.base($text, item, field);
    $text.val('');
    Control.Text.placeholder($text, 'Add a link');
    $text.change(function (e) { Control.LinkArray.update($(e.srcElement)); });
    $text.keypress(function (e) { if (e.which == 13) { Control.LinkArray.update($(e.srcElement)); return false; } });

    var linkArray = new LinkArray(item.GetFieldValue(field));
    var links = linkArray.Links();
    if (links.length > 0) {                 // display existing links with delete
        for (var i in links) {
            Control.LinkArray.deleteBtn(i).appendTo($wrapper);
            var link = links[i];
            var $link = $('<a/>').appendTo($wrapper);
            var name = (link.Name == null) ? link.Url : link.Name;
            $link.html(name);
            $link.attr('href', link.Url);
            $link.attr('target', 'new');
        }
    }
    return $text;
}

Control.LinkArray.update = function Control$LinkArray$update($input) {
    var item = $input.data('item');
    var field = $input.data('field');
    var linkArray = new LinkArray(item.GetFieldValue(field));
    var value = $input.val();
    if (value != null && value.length > 0) {
        linkArray.Add(value);
        var updatedItem = item.Copy();
        updatedItem.SetFieldValue(field, linkArray.ToJson());
        item.Update(updatedItem);
    }
}

// return an element that is an icon for deleting an link in the array
Control.LinkArray.deleteBtn = function Control$Icons$deleteBtn(index) {
    var $icon = $('<i class="icon-remove-circle"></i>');
    $icon.css('cursor', 'pointer');
    $icon.attr('title', 'Remove Link').tooltip(Control.noDelay);
    $icon.data('index', index);
    $icon.bind('click', function () {
        var $this = $(this);
        $this.tooltip('hide');
        var $input = $this.parents('.link-array').first().find('input');
        var item = $input.data('item');
        var field = $input.data('field');
        var linkArray = new LinkArray(item.GetFieldValue(field));
        linkArray.Remove($this.data('index'));
        var updatedItem = item.Copy();
        updatedItem.SetFieldValue(field, linkArray.ToJson());
        item.Update(updatedItem);
        return false;   // do not propogate event
    });
    // wrap in anchor tag to get tooltips to work in Chrome
    return $('<a class="icon" />').append($icon);
}

// ---------------------------------------------------------
// Control.DateTime static object
//
Control.DateTime = {};
Control.DateTime.renderDatePicker = function Control$DateTime$renderDatePicker($element, item, field) {
    $date = $('<input type="text" />').appendTo($element);
    $date.addClass(field.Class);
    $date.data('item', item);
    $date.data('field', field);
    $date.val(Control.DateTime.format(item.GetFieldValue(field), 'shortDate'));
    $date.datepicker({
        numberOfMonths: 2,
        onClose: function (value, picker) { Control.DateTime.update(picker.input); }
    });
    return $date;
}

Control.DateTime.renderDateTimePicker = function Control$DateTime$renderDateTimePicker($element, item, field) {
    $date = $('<input type="text" />').appendTo($element);
    $date.addClass(field.Class);
    $date.data('item', item);
    $date.data('field', field);
    $date.val(Control.DateTime.format(item.GetFieldValue(field), 'shortDateTime'));
    $date.datetimepicker({
        ampm: true,
        timeFormat: 'h:mm TT',
        hourGrid: 4,
        minuteGrid: 15,
        stepMinute: 15,
        numberOfMonths: 2,
        onClose: function (value, picker) { Control.DateTime.update(picker.input); }
    });
    return $date;
}

Control.DateTime.renderRange = function Control$DateTime$renderRange($element, item, fieldStart, fieldEnd, tag) {
    var tag = (tag == null) ? 'span' : tag;
    var $tag, value;
    var startValue = item.GetFieldValue(fieldStart);
    var endValue = item.GetFieldValue(fieldEnd);
    if (startValue != null && endValue != null) {
        if (fieldStart.FieldType == FieldTypes.DateTime && fieldEnd.FieldType == FieldTypes.DateTime) {
            var startDate = new Date().parseRFC3339(startValue);
            var endDate = new Date().parseRFC3339(endValue);
            if (startDate.toDateString() == endDate.toDateString()) {
                // display range as same day
                var endParts = endDate.format().split(' ');
                var endHour = endParts[endParts.length - 2] + ' ' + endParts[endParts.length - 1];
                value = 'On ' + startDate.format() + ' to ' + endHour;
            } else {
                // display range as multiple days
                value = 'From ' + startDate.format() + ' until ' + endDate.format();
            }
        } else {
            value = 'From ' + startValue + ' until ' + endValue;
        }
    }
    if (value != null) {
        $tag = $('<' + tag + '/>').appendTo($element);
        $tag.addClass(fieldStart.Class);
        $tag.html(value);
    }
    return $tag;
}

Control.DateTime.update = function Control$DateTime$update($input) {
    // suppress refresh to prevent jquery-ui bug using multiple datetimepickers
    Control.Text.update($input, null);
}

// format for DateTime
Control.DateTime.format = function Control$DateTime$format(text, mask) {
    if (text != null && text.length > 0) {
        try {
            var date = new Date().parseRFC3339(text);
            return date.format(mask);
        } catch (e) {
            // if not a valid date, just return text
        }
    }
    return text;
}
// format for UTC for storage
Control.DateTime.formatUTC = function Control$DateTime$formatUTC(text) {
    return Control.DateTime.format(text, "isoUtcDateTime");
}

// ---------------------------------------------------------
// Control.ContactList static object
//
Control.ContactList = {};
Control.ContactList.renderInput = function Control$ContactList$renderInput($element, item, field) {
    $input = $('<input type="text" />').appendTo($element);
    $input.addClass(field.Class);
    $input.data('item', item);
    $input.data('field', field);
    $input.keypress(function (e) { if (e.which == 13) { Control.ContactList.update($(e.srcElement)); return false; } });
    var value = item.GetFieldValue(field);
    var text = '';
    if (value != null && value.IsList) {
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
    $input.val(text);

    Control.Text.autoCompleteContact($input, Control.ContactList.update);

    return $input;
}

Control.ContactList.update = function Control$ContactList$update($input) {
    var item = $input.data('item');
    var field = $input.data('field');
    var contactName = $input.val();
    if (contactName == null || contactName.length == 0) {
        item.RemoveReferences(field);
        return;
    }

    var contact;
    var jsonContact = $input.data(FieldNames.Contacts);
    if (jsonContact != null) {
        contact = $.parseJSON(jsonContact);
    }

    if (contact != null && contact.ItemTypeID == ItemTypes.Reference) {
        // add reference to existing contact
        contact = { Name: contact.Name, ID: contact.FieldValues[0].Value };
        item.AddReference(field, contact, true);
    } else {
        if (contact != null) {
            contact = Item.Extend(contact);
            var fbID = contact.GetFieldValue(FieldNames.FacebookID);
            var existingContact = DataModel.FindContact(contact.Name, fbID);
            if (existingContact != null) {
                // add reference to existing contact
                item.AddReference(field, contact, true);
                return;
            }
        } else {
            contact = Item.Extend({ Name: contactName, ItemTypeID: ItemTypes.Contact });
        }
        // create new contact and add reference
        var contactList = DataModel.UserSettings.GetDefaultList(ItemTypes.Contact);
        DataModel.InsertItem(contact, contactList, null, null, null,
            function (insertedContact) {
                item.AddReference(field, insertedContact, true);
            });
    }
}

// ---------------------------------------------------------
// Control.LocationList static object
//
Control.LocationList = {};
Control.LocationList.renderInput = function Control$LocationList$renderInput($element, item, field) {
    $input = $('<input type="text" />').appendTo($element);
    $input.addClass(field.Class);
    $input.data('item', item);
    $input.data('field', field);
    $input.keypress(function (e) { if (e.which == 13) { Control.LocationList.update($(e.srcElement)); return false; } });
    var value = item.GetFieldValue(field);
    var text = '';
    if (value != null && value.IsList) {
        var locations = value.GetItems();
        for (var id in locations) {
            var locationRef = locations[id].GetFieldValue(FieldNames.EntityRef);
            if (locationRef != null) {
                /*
                var address = locationRef.GetFieldValue(FieldNames.Address);
                text += address;
                if (locationRef.Name != address) {
                text += ' ( ' + locationRef.Name + ' )';
                }
                */
                text = locationRef.Name;
            }
            break;
        }
    }
    $input.val(text);

    //Control.Text.autoCompleteAddress($input, Control.LocationList.update);
    Control.Text.autoCompletePlace($input, Control.LocationList.update);

    return $input;
}

Control.LocationList.update = function Control$LocationList$update($input) {
    var item = $input.data('item');
    var field = $input.data('field');
    var address = $input.val();
    if (address == null || address.length == 0) {
        item.RemoveReferences(field);
        return;
    }

    var latlong = $input.data(FieldNames.LatLong);
    var place = $input.data('place');
    if (place != null && place.geometry != null) {
        latlong = place.geometry.location.toUrlValue();
    }
    var existingLocation = DataModel.FindLocation(address, latlong);
    if (existingLocation != null) {
        // add reference to existing location
        item.AddReference(field, existingLocation, true);
    } else {
        // create new location and add reference
        var locationList = DataModel.UserSettings.GetDefaultList(ItemTypes.Location);
        var newLocation = Item.Extend({ Name: address, ItemTypeID: ItemTypes.Location });
        if (place != null && place.geometry != null) {
            Control.Text.applyPlace(newLocation, place);
        } else {
            newLocation.SetFieldValue(FieldNames.Address, address);
            if (latlong != null) {
                newLocation.SetFieldValue(FieldNames.LatLong, latlong);
            }
        }
        DataModel.InsertItem(newLocation, locationList, null, null, null,
            function (insertedLocation) {
                item.AddReference(field, insertedLocation, true);
            });
    }
}

// ---------------------------------------------------------
// Control.List static object
// static re-usable helper to support List elements <ul>
//
Control.List = {};

// make a list of items sortable, apply to <ul> element
// each <li> in list must have attached data('item')
Control.List.sortable = function Control$List$sortable($element) {
    $element.sortable({
        revert: true,
        stop: function (e, ui) {
            $('i').tooltip('hide');
            var $item = ui.item;
            var item = $item.data('item');
            if (item != null) {
                var liElements = $item.parent('ul').children('li');
                for (var i in liElements) {
                    if (item.ID == $(liElements[i]).data('item').ID) {
                        var $liBefore = $(liElements[i]).prevAll('li').first();
                        var before = Number(($liBefore.length == 0) ? 0 : $liBefore.data('item').SortOrder);
                        var $liAfter = $(liElements[i]).nextAll('li').first();
                        var after = Number(($liAfter.length == 0) ? before + 1000 : $liAfter.data('item').SortOrder);
                        var updatedItem = item.Copy();
                        updatedItem.SortOrder = before + ((after - before) / 2);
                        item.Update(updatedItem);
                        break;
                    }
                }
            }
        }
    });
}

// ---------------------------------------------------------
// Control.ItemType static object
// static re-usable helper to display and update ItemTypeID on an item
//
Control.ItemType = {};
Control.ItemType.renderDropdown = function Control$ItemType$renderDropdown($element, item, noLabel) {
    var itemTypes = DataModel.Constants.ItemTypes;
    var currentItemTypeName = itemTypes[item.ItemTypeID].Name;
    var $wrapper = $wrapper = $('<div class="control-group"></div>').appendTo($element);
    if (noLabel != true) {
        var labelType = (item.IsFolder() || item.IsList) ? 'List' : 'Item';
        var label = '<label class="control-label">Type of ' + labelType + '</label>';
        $(label).appendTo($wrapper);
    }

    var $btnGroup = $('<div class="btn-group" />').appendTo($wrapper);
    var $btn = $('<a class="btn dropdown-toggle" data-toggle="dropdown" />').appendTo($btnGroup);
    Control.Icons.forItemType(item).appendTo($btn);
    if (noLabel != true) {
        $('<span>&nbsp;&nbsp;' + currentItemTypeName + '</span>').appendTo($btn);
        $('<span class="pull-right">&nbsp;&nbsp;<span class="caret" /></span>').appendTo($btn);
    }

    var $dropdown = $('<ul class="dropdown-menu" />').appendTo($btnGroup);
    $dropdown.data('item', item);
    for (var id in itemTypes) {
        var itemType = itemTypes[id];
        if (itemType.UserID == SystemUsers.User || itemType.UserID == DataModel.User.ID) {
            var $menuitem = $('<li><a></a></li>').appendTo($dropdown);
            $menuitem.find('a').append(Control.Icons.forItemType(id));
            $menuitem.find('a').append('<span>&nbsp;&nbsp;' + itemTypes[id].Name + '</span>');
            $menuitem.data('value', id);
            $menuitem.click(function (e) { Control.ItemType.update($(this)); e.preventDefault(); });
        }
    }

    return $wrapper;
}

Control.ItemType.update = function Control$ItemType$update($menuitem) {
    var item = $menuitem.parent().data('item');
    var updatedItem = item.Copy();
    updatedItem.ItemTypeID = $menuitem.data('value');
    var $button = $menuitem.parents('.btn-group').first().find('.btn');
    $button.find('i').replaceWith(Control.Icons.forItemType(updatedItem));
    var $label = $menuitem.find('span').first();
    if ($label.length > 0) { $button.find('span').first().html($label.html()); }
    item.Update(updatedItem);
}

// ---------------------------------------------------------
// Control.ThemePicker static object
// static re-usable helper to display theme picker and update UserSettings
//
Control.ThemePicker = {};
Control.ThemePicker.render = function Control$ThemePicker$render($element) {
    var themes = DataModel.Constants.Themes;
    var currentTheme = DataModel.UserSettings.Preferences.Theme;
    var $wrapper = $('<div class="control-group"><label class="control-label">Theme</label></div>').appendTo($element);

    var $btnGroup = $('<div class="btn-group" />').appendTo($wrapper);
    var $btn = $('<a class="btn dropdown-toggle" data-toggle="dropdown" />').appendTo($btnGroup);
    $('<span>' + currentTheme + '</span>').appendTo($btn);
    $('<span class="pull-right">&nbsp;&nbsp;<span class="caret" /></span>').appendTo($btn);

    var $dropdown = $('<ul class="dropdown-menu" />').appendTo($btnGroup);
    for (var i in themes) {
        $('<li><a>' + themes[i] + '</a></li>').appendTo($dropdown);
    }
    $dropdown.click(function (e) {
        var $element = $(e.target)
        var theme = $element.html();
        DataModel.UserSettings.UpdateTheme(theme);
        $element.parents('.btn-group').find('span').first().html(theme);
        e.preventDefault();
    });
    return $wrapper;
}


// ---------------------------------------------------------
// Control.DateFormat function for formatting dates and times
//
Control.DateFormat = function Control$DateFormat() {
    var token = /d{1,4}|m{1,4}|yy(?:yy)?|([HhMsTt])\1?|[LloSZ]|"[^"]*"|'[^']*'/g,
		timezone = /\b(?:[PMCEA][SDP]T|(?:Pacific|Mountain|Central|Eastern|Atlantic) (?:Standard|Daylight|Prevailing) Time|(?:GMT|UTC)(?:[-+]\d{4})?)\b/g,
		timezoneClip = /[^-+\dA-Z]/g,
		pad = function (val, len) {
		    val = String(val);
		    len = len || 2;
		    while (val.length < len) val = "0" + val;
		    return val;
		};

    // regexes and supporting functions are cached through closure
    return function (date, mask, utc) {
        var dF = Control.DateFormat;

        // cannot provide utc if skipping other args (use "UTC:" mask prefix)
        if (arguments.length == 1 && Object.prototype.toString.call(date) == "[object String]" && !/\d/.test(date)) {
            mask = date;
            date = undefined;
        }

        // applies Date.parse, if necessary
        date = date ? new Date(date) : new Date;
        if (isNaN(date)) throw SyntaxError("invalid date");

        mask = String(dF.masks[mask] || mask || dF.masks["default"]);

        // allow setting utc argument via the mask
        if (mask.slice(0, 4) == "UTC:") {
            mask = mask.slice(4);
            utc = true;
        }

        var _ = utc ? "getUTC" : "get",
			d = date[_ + "Date"](),
			D = date[_ + "Day"](),
			m = date[_ + "Month"](),
			y = date[_ + "FullYear"](),
			H = date[_ + "Hours"](),
			M = date[_ + "Minutes"](),
			s = date[_ + "Seconds"](),
			L = date[_ + "Milliseconds"](),
			o = utc ? 0 : date.getTimezoneOffset(),
			flags = {
			    d: d,
			    dd: pad(d),
			    ddd: dF.i18n.dayNames[D],
			    dddd: dF.i18n.dayNames[D + 7],
			    m: m + 1,
			    mm: pad(m + 1),
			    mmm: dF.i18n.monthNames[m],
			    mmmm: dF.i18n.monthNames[m + 12],
			    yy: String(y).slice(2),
			    yyyy: y,
			    h: H % 12 || 12,
			    hh: pad(H % 12 || 12),
			    H: H,
			    HH: pad(H),
			    M: M,
			    MM: pad(M),
			    s: s,
			    ss: pad(s),
			    l: pad(L, 3),
			    L: pad(L > 99 ? Math.round(L / 10) : L),
			    t: H < 12 ? "a" : "p",
			    tt: H < 12 ? "am" : "pm",
			    T: H < 12 ? "A" : "P",
			    TT: H < 12 ? "AM" : "PM",
			    Z: utc ? "UTC" : (String(date).match(timezone) || [""]).pop().replace(timezoneClip, ""),
			    o: (o > 0 ? "-" : "+") + pad(Math.floor(Math.abs(o) / 60) * 100 + Math.abs(o) % 60, 4),
			    S: ["th", "st", "nd", "rd"][d % 10 > 3 ? 0 : (d % 100 - d % 10 != 10) * d % 10]
			};

        return mask.replace(token, function ($0) {
            return $0 in flags ? flags[$0] : $0.slice(1, $0.length - 1);
        });
    };
} ();

// common format strings
Control.DateFormat.masks = {
    "default": "ddd, mmm dd, yyyy h:MM TT",
    shortDate: "m/d/yy",
    mediumDate: "mmm d, yyyy",
    longDate: "mmmm d, yyyy",
    fullDate: "dddd, mmmm d, yyyy",
    shortTime: "h:MM TT",
    mediumTime: "h:MM:ss TT",
    longTime: "h:MM:ss TT Z",
    isoDate: "yyyy-mm-dd",
    isoTime: "HH:MM:ss",
    shortDateTime: "mm/dd/yyyy h:MM TT",
    isoDateTime: "yyyy-mm-dd'T'HH:MM:ss",
    isoUtcDateTime: "UTC:yyyy-mm-dd'T'HH:MM:ss'Z'"
};

// internationalization strings
Control.DateFormat.i18n = {
    dayNames: [
		"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat",
		"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
	],
    monthNames: [
		"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
		"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
	]
};

// extend Date object with format prototype
Date.prototype.format = function (mask, utc) {
    return Control.DateFormat(this, mask, utc);
};

// parse RFC3339 datetime format for downlevel browsers
Date.prototype.parseRFC3339 = function (text) {
    var regexp = /(\d\d\d\d)(-)?(\d\d)(-)?(\d\d)(T)?(\d\d)(:)?(\d\d)(:)?(\d\d)(\.\d+)?(Z|([+-])(\d\d)(:)?(\d\d))/;

    if (text.toString().match(new RegExp(regexp))) {
        var d = text.match(new RegExp(regexp));
        var offset = 0;

        this.setUTCDate(1);
        this.setUTCFullYear(parseInt(d[1], 10));
        this.setUTCMonth(parseInt(d[3], 10) - 1);
        this.setUTCDate(parseInt(d[5], 10));
        this.setUTCHours(parseInt(d[7], 10));
        this.setUTCMinutes(parseInt(d[9], 10));
        this.setUTCSeconds(parseInt(d[11], 10));
        if (d[12])
            this.setUTCMilliseconds(parseFloat(d[12]) * 1000);
        else
            this.setUTCMilliseconds(0);
        if (d[13] != 'Z') {
            offset = (d[15] * 60) + parseInt(d[17], 10);
            offset *= ((d[14] == '-') ? -1 : 1);
            this.setTime(this.getTime() - offset * 60 * 1000);
        }
    }
    else {
        this.setTime(Date.parse(text));
    }
    return this;
};