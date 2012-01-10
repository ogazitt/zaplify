/* constants.js */

var constants = constants || {};
constants.refresh = function () {
    $.ajax({
        url: "/constants",
        contentType: "application/json",
        success: function (response_data, text, xhrobject) {
            var response = ajaxhelper.validate(response_data);
            for (var key in response) {
                constants[key] = response[key];
            }
            // replace ListType array with associative arrays (objects)
            constants.listTypes = {};
            constants.ListTypesByName = {};
            for (var ix in constants.ListTypes) {
                var listType = constants.ListTypes[ix];
                constants.listTypes[listType.ID] = listType;
                constants.ListTypesByName[listType.Name] = listType;
                listType.fields = [];
                for (var jx in listType.Fields) {
                    var field = listType.Fields[jx];
                    listType.fields[field.SortOrder] = field;
                }
                listType.Fields = listType.fields;
            }
            constants.ListTypes = constants.listTypes;
            // augment Priority array with assocaitive array by name
            constants.PrioritiesByName = {};
            for (var ix in constants.Priorities) {
                var priority = constants.Priorities[ix];
                constants.PrioritiesByName[priority.Name] = priority;
            }
            // hack: replace FieldType Due with DueDate 
            //   (Due, where typeof(Due) == Date only exists for the phone, whereas 
            //   DueDate, where typeof(DueDate) == string is the field we use on the server)
            //for (var ix in constants.FieldTypes) {
            //    var fieldType = constants.FieldTypes[ix];
            //    if (fieldType.Name == "Due")
            //        fieldType.Name = "DueDate";
            //}
        }
    });
};