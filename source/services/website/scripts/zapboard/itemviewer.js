//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// ItemViewer.js

// ---------------------------------------------------------
// ItemViewer control
function ItemViewer(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.list = null;
}

// static helper for getting attached item from $element
ItemViewer.getItem = function ItemViewer$getItem($element) {
    return $element.parent('a').data('item');
}

ItemViewer.prototype.hide = function () {
    if (this.$element != null) {
        this.$element.hide();
    }
}

ItemViewer.prototype.show = function (height) {
    if (this.$element != null) {
        if (height != null) { this.$element.height(height); }
        this.$element.show();
    }
}

ItemViewer.prototype.render = function (list, container) {
    if (list == null) { return; }
    if (this.$element == null) {
        this.$element = $('<ul class="nav nav-list" />').appendTo(container);
    }
    this.$element.empty();
    if (this.renderListItems(list.GetItems()) == 0) { this.hide(); }
}

ItemViewer.prototype.renderListItems = function (listItems) {
    var itemCount = 0;
    for (var id in listItems) {
        var item = listItems[id];
        if (!item.IsList) {
            var $li = $('<li />').appendTo(this.$element);
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

ItemViewer.prototype.renderNameField = function ($item, item) {
    var fields = item.GetFields();
    // render complete field if exists 
    var field = fields[FieldNames.Complete];
    if (field != null) {
        this.renderCheckbox($item, item, field);
    }
    // render name field
    $item.append(Control.getIconsForSources(item));
    field = fields[FieldNames.Name];
    this.renderLabel($item, item, field);
}

ItemViewer.prototype.renderDeleteBtn = function ($element) {
    var $button = $('<i class="icon-remove pull-right"></i>').appendTo($element);
    $button.attr('title', 'Delete Item').tooltip(Control.ttDelay);
    $button.bind('click', function () {
        var item = ItemViewer.getItem($(this));
        var activeItem = (item.ParentID == null) ? item.GetFolder() : item.GetParent();
        item.Delete(activeItem);
        return false;   // do not propogate event
    });
}

ItemViewer.prototype.renderFields = function ($element, item) {
    var fields = item.GetFields();
    for (var name in fields) {
        var field = fields[name];
        this.renderField($element, item, field);
    }
}

ItemViewer.prototype.renderField = function ($element, item, field) {
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

ItemViewer.prototype.renderLabel = function ($element, item, field) {
    var $field;
    var value = item.GetFieldValue(field);
    if (value != null) {
        $field = $('<label style="font-weight:bold;"/>').appendTo($element);
        $field.addClass(field.Class);
        $field.html(value);
    }
    return $field;
}

ItemViewer.prototype.renderText = function ($element, item, field, textBefore, textAfter) {
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

ItemViewer.prototype.renderEmail = function ($element, item, field) {
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

ItemViewer.prototype.renderCheckbox = function ($element, item, field) {
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

ItemViewer.prototype.updateCheckbox = function ($checkbox, field) {
    var item = ItemViewer.getItem($checkbox);
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
