//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// Controls.js


// ---------------------------------------------------------
// Control static object
// shared helpers used by controls
var Control = function Control$() {};
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
                            $fbLink.click(function () { window.open('http://www.facebook.com/' + fbID); });
                        }
                        break;
                    case "Directory":
                        $icons.append('<i class="azure-icon" />');
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
    var $icon = $('<i></i>');
    switch (item.ItemTypeID) {
        case ItemTypes.Task:
            (item.IsFolder()) ? $icon.addClass('icon-calendar') : $icon.addClass('icon-check');
            break;
        case ItemTypes.Contact:
            $icon.addClass('icon-user');
            break;
        case ItemTypes.Location:
            $icon.addClass('icon-map-marker');
            break;
        case ItemTypes.ShoppingItem:
            $icon.addClass('icon-shopping-cart');
            break;
        case ItemTypes.ListItem:
        default:
            $icon.addClass('icon-list-alt');
            break;
    }
    return $icon;
}

// ---------------------------------------------------------
// Control.Checkbox static object
//
Control.Checkbox = {};

Control.Checkbox.update = function Control$Checkbox$update(item, field) {

}

// ---------------------------------------------------------
// Control.ItemTypePicker static object
// static re-usable helper to display and update ItemTypeID on an item
//
Control.ItemTypePicker = {};
Control.ItemTypePicker.render = function Control$ItemTypePicker$render($element, item) {
    var itemTypes = DataModel.Constants.ItemTypes;
    var currentItemTypeName = itemTypes[item.ItemTypeID].Name;
    var labelType = (item.IsFolder() || item.IsList) ? 'List' : 'Item';
    var label = '<label class="control-label">Type of ' + labelType + '</label>';
    var $wrapper = $('<div class="control-group"></div>').appendTo($element);
    $(label).appendTo($wrapper);

    var $btnGroup = $('<div class="btn-group" />').appendTo($wrapper);
    var $btn = $('<a class="btn">' + currentItemTypeName + '</a>').appendTo($btnGroup);
    $btn = $('<a class="btn dropdown-toggle" data-toggle="dropdown" />').appendTo($btnGroup);
    $('<span class="caret"></span>').appendTo($btn);
    var $dropdown = $('<ul class="dropdown-menu" />').appendTo($btnGroup);
    for (var id in itemTypes) {
        var itemType = itemTypes[id];
        if (itemType.UserID == SystemUsers.User || itemType.UserID == DataModel.User.ID) {
            var $menuitem = $('<li><a>' + itemTypes[id].Name + '</a></li>').appendTo($dropdown);
            $menuitem.find('a').attr('value', id);
        }
    }
    $dropdown.click(function (e) {
        var $element = $(e.target);
        var updatedItem = item.Copy();
        updatedItem.ItemTypeID = $element.val();
        $element.parents('.btn-group').find('.btn').first().html($element.html());
        item.Update(updatedItem);
        e.preventDefault();
    });
}

// ---------------------------------------------------------
// Control.ItemTypePicker static object
// static re-usable helper to display theme picker and update UserSettings
//
Control.ThemePicker = {};
Control.ThemePicker.render = function Control$ThemePicker$render($element) {
    var themes = DataModel.Constants.Themes;
    var currentTheme = DataModel.UserSettings.Preferences.Theme;
    var $wrapper = $('<div class="control-group"><label class="control-label">Theme</label></div>').appendTo($element);

    var $btnGroup = $('<div class="btn-group" />').appendTo($wrapper);
    var $btn = $('<a class="btn">' + currentTheme + '</a>').appendTo($btnGroup);
    $btn = $('<a class="btn dropdown-toggle" data-toggle="dropdown" />').appendTo($btnGroup);
    $('<span class="caret"></span>').appendTo($btn);
    var $dropdown = $('<ul class="dropdown-menu" />').appendTo($btnGroup);
    for (var i in themes) {
        $('<li><a>' + themes[i] + '</a></li>').appendTo($dropdown);
    }
    $dropdown.click(function (e) {
        var $element = $(e.target)
        var theme = $element.html();
        DataModel.UserSettings.UpdateTheme(theme);
        $element.parents('.btn-group').find('.btn').first().html(theme);
        e.preventDefault();
    });
}