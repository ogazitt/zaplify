//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// SuggestionList.js

// ---------------------------------------------------------
// SuggestionList control
function SuggestionList() {
    // fires notification when suggestion is selected
    this.onSelectionChangedHandlers = {};
    this.groups = {};
    this.$element = null;
}

SuggestionList.prototype.addSelectionChangedHandler = function (name, handler) {
    this.onSelectionChangedHandlers[name] = handler;
}

SuggestionList.prototype.removeSelectionChangedHandler = function (name) {
    this.onSelectionChangedHandlers[name] = undefined;
}

SuggestionList.prototype.fireSelectionChanged = function (suggestion) {
    for (var name in this.onSelectionChangedHandlers) {
        var handler = this.onSelectionChangedHandlers[name];
        if (typeof (handler) == "function") {
            if (suggestion != null) {
                handler(suggestion);
            }
        }
    }
}

SuggestionList.prototype.working = function (on) {
    if (this.$element != null) {
        if (on == true) {
            this.$element.prepend('<div class="working"><span /></div>');
        } else {
            this.$element.find('.working').remove();
        }
    }
}

SuggestionList.prototype.render = function ($element, groups) {
    this.groups = groups;
    this.$element = $element.empty();
    for (var id in this.groups) {
        var group = this.groups[id];
        if (group.Suggestions != {}) {
            var $group = $('<ul class="nav nav-pills nav-stacked" />').appendTo(this.$element);
            $group.attr('id', group.GroupID);
            $('<li class="active"><a><strong>' + group.DisplayName + '</strong></a></li>').appendTo($group);
            this.renderSuggestions($group, group);
        }
    }
}

SuggestionList.prototype.renderSuggestions = function ($group, group) {
    var suggestions = group.Suggestions;
    for (var id in suggestions) {
        var suggestion = suggestions[id];
        if (suggestion.SuggestionType == SuggestionTypes.ChooseManyWithChildren) {
            // only display if children are present
            if (ItemMap.count(suggestion.Suggestions) > 0) {
                $dropdown = $('<li class="dropdown"><a class="dropdown-toggle" data-toggle="dropdown"><b class="caret"></b>&nbsp;&nbsp;' + suggestion.DisplayName + '</a></li>').appendTo($group);
                this.renderChildSuggestions($dropdown, suggestion);
            }
        } else {
            this.renderSuggestion($group, suggestion);
        }
    }
}

SuggestionList.prototype.renderChildSuggestions = function ($dropdown, parent) {
    var children = parent.Suggestions;
    var $dropdownmenu = $('<ul class="dropdown-menu pull-right" />').appendTo($dropdown);
    for (var id in children) {
        var suggestion = children[id];
        this.renderSuggestion($dropdownmenu, suggestion);
    }
}

SuggestionList.prototype.renderSuggestion = function ($element, suggestion) {
    $suggestion = $('<li><a>' + suggestion.DisplayName + '</a></li>').appendTo($element);
    $suggestion.data('control', this);
    $suggestion.data('suggestion', suggestion);
    $suggestion.click(function () { Control.get(this).suggestionClicked($(this)); });

    if (suggestion.SuggestionType == SuggestionTypes.ChooseOneSubject) {
        var contact = Item.Extend($.parseJSON(suggestion.Value));
        $icons = Control.Icons.forSources(contact).addClass('pull-right');
        $suggestion.find('a').append($icons);
    }
}

SuggestionList.prototype.suggestionClicked = function ($element) {
    var suggestion = $element.data('suggestion');
    this.fireSelectionChanged(suggestion);
}

SuggestionList.prototype.hideGroup = function (groupID) {
    $('#' + groupID).collapse('hide');
}