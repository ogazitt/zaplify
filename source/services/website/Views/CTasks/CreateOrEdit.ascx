<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<TaskStoreServerEntities.Task>" %>

<div class="editor-label">
    <%: Html.LabelFor(model => model.Name) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.Name) %>
    <%: Html.ValidationMessageFor(model => model.Name) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.Complete) %>
</div>
<div class="editor-field">
    <%: Html.CheckBoxFor(model => model.Complete) %>
    <%: Html.ValidationMessageFor(model => model.Complete) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.Description) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.Description) %>
    <%: Html.ValidationMessageFor(model => model.Description) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.DueDate) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.DueDate) %>
    <%: Html.ValidationMessageFor(model => model.DueDate) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.Location) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.Location) %>
    <%: Html.ValidationMessageFor(model => model.Location) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.Phone) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.Phone) %>
    <%: Html.ValidationMessageFor(model => model.Phone) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.Website) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.Website) %>
    <%: Html.ValidationMessageFor(model => model.Website) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.Email) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.Email) %>
    <%: Html.ValidationMessageFor(model => model.Email) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.LinkedTaskListID) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.LinkedTaskListID) %>
    <%: Html.ValidationMessageFor(model => model.LinkedTaskListID) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.Created) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.Created) %>
    <%: Html.ValidationMessageFor(model => model.Created) %>
</div>

<div class="editor-label">
    <%: Html.LabelFor(model => model.LastModified) %>
</div>
<div class="editor-field">
    <%: Html.EditorFor(model => model.LastModified) %>
    <%: Html.ValidationMessageFor(model => model.LastModified) %>
</div>

<div class="editor-label">
    TaskList
</div>
<div class="editor-field">
    <%: Html.DropDownListFor(model => model.TaskListID, ((IEnumerable<TaskStoreServerEntities.TaskList>)ViewBag.PossibleTaskLists).Select(option => new SelectListItem {
		Text = (option == null ? "None" : option.Name), 
        Value = option.ID.ToString(),
        Selected = (Model != null) && (option.ID == Model.TaskListID)
    }), "Choose...") %>
	<%: Html.ValidationMessageFor(model => model.TaskListID) %>
</div>
<div class="editor-label">
    Priority
</div>
<div class="editor-field">
    <%: Html.DropDownListFor(model => model.PriorityID, ((IEnumerable<TaskStoreServerEntities.Priority>)ViewBag.PossiblePriorities).Select(option => new SelectListItem {
		Text = (option == null ? "None" : option.Name), 
        Value = option.PriorityID.ToString(),
        Selected = (Model != null) && (option.PriorityID == Model.PriorityID)
    }), "Choose...") %>
	<%: Html.ValidationMessageFor(model => model.PriorityID) %>
</div>
