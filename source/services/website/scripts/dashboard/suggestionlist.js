//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// SuggestionList.js

// ---------------------------------------------------------
// SuggestionList control
function SuggestionList() {
    // fires notification when suggestion is selected
    this.onSelectionChangedHandlers = {};
    this.suggestions = {};
    this.$element = null;

    this.groupButtons = [];
}

SuggestionList.prototype.addSelectionChangedHandler = function (name, handler) {
    this.onSelectionChangedHandlers[name] = handler;
}

SuggestionList.prototype.removeSelectionChangedHandler = function (name) {
    this.onSelectionChangedHandlers[name] = undefined;
}

SuggestionList.prototype.fireSelectionChanged = function (groupID, suggestionID) {
    var suggestion;
    if (this.suggestions != null && this.suggestions[groupID] != null) {
        suggestion = this.suggestions[groupID].Suggestions[suggestionID]
    }
    for (var name in this.onSelectionChangedHandlers) {
        var handler = this.onSelectionChangedHandlers[name];
        if (typeof (handler) == "function") {
            if (suggestion != null) {
                handler(suggestion);
            }
        }
    }
}

SuggestionList.prototype.getGroupButtons = function () {
    // create buttons for each group of suggestions
    this.groupButtons = [];
    for (var i in this.suggestions) {
        this.groupButtons = this.groupButtons.concat(new GroupButton(this, this.suggestions[i]));
    }
    return this.groupButtons;
}

SuggestionList.prototype.render = function (container, suggestions) {
    this.suggestions = suggestions;
    this.$element = $(container).empty();
    this.getGroupButtons();
    for (var i in this.groupButtons) {
        this.groupButtons[i].render(container);
    }
}

SuggestionList.prototype.hideGroup = function (groupID) {
    for (var i in this.groupButtons) {
        var button = this.groupButtons[i];
        if (button.group.groupID == groupID) {
            button.hide();
            break;
        }
    }
}

// ---------------------------------------------------------
// GroupButton control
function GroupButton(parentControl, group) {
    this.parentControl = parentControl;
    this.group = group;
    this.choiceList = null;
    this.$element = null;
}

GroupButton.prototype.render = function (container) {
    if (this.group.Suggestions != {}) {
        this.$element = $('<div class="folder-button ui-widget ui-state-active"></div>').appendTo(container);
        this.$element.append('<span>' + this.group.DisplayName + '</span>');
        this.$element.hover(function () { $(this).addClass('ui-state-hover'); }, function () { $(this).removeClass('ui-state-hover'); });

        var choiceList = new ChoiceList(this, this.group.Suggestions);
        var $container = $('<div class="folder-items"></div>').insertAfter(this.$element);
        choiceList.render($container);
    }
}

GroupButton.prototype.hide = function () {
    Control.animateCollapse(this.$element.next());
    Control.animateCollapse(this.$element);
}

// ---------------------------------------------------------
// ChoiceList control
function ChoiceList(parentControl, choices) {
    this.parentControl = parentControl;
    this.choiceButtons = [];
    for (var i in choices) {
        this.choiceButtons = this.choiceButtons.concat(new ChoiceButton(this, choices[i]));
    }
}

ChoiceList.prototype.render = function (container) {
    $(container).empty();
    for (var i in this.choiceButtons) {
        this.choiceButtons[i].render(container);
    }
    Control.animateExpand($(container));
}

// ---------------------------------------------------------
// ChoiceButton control
function ChoiceButton(parentControl, choice) {
    this.parentControl = parentControl;
    this.choice = choice;
    this.$element = null;
}

ChoiceButton.prototype.render = function (container) {
    this.$element = $('<div></div>').attr('id', this.choice.ID).appendTo(container);
    this.$element.data('control', this);
    this.$element.addClass('item-button');
    this.$element.click(function () { Control.get(this).select(); });
    this.$element.hover(function () { $(this).addClass('ui-state-highlight'); }, function () { $(this).removeClass('ui-state-highlight'); });
    this.$element.append('<span>' + this.choice.DisplayName + '</span>');

    if (this.choice.SuggestionType == SuggestionTypes.ChooseOneSubject) {
        var contact = $.extend(new Item(), $.parseJSON(this.choice.Value));
        Control.renderSourceIcons(this.$element, contact);
    }
}

ChoiceButton.prototype.select = function () {
    var groupButton = this.getGroupButton();
    if (groupButton != null) {
        this.selectChoice(groupButton);
    } 
}

ChoiceButton.prototype.selectChoice = function (groupButton) {
    // fire selection changed 
    if (groupButton != null) {
        var selected = this.$element.hasClass('selected');
        var groupList = groupButton.parentControl;
        if (!selected) {
            //this.$element.addClass('selected');
            groupList.fireSelectionChanged(this.choice.GroupID, this.choice.ID);
        }
    }
}

ChoiceButton.prototype.deselect = function () {
    this.$element.removeClass('selected');
}

// return parent GroupButton
ChoiceButton.prototype.getGroupButton = function () {
    return Control.findParent(this, 'group');
}

