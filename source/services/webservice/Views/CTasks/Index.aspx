<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<IEnumerable<TaskStoreServerEntities.Task>>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    Index
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="ScriptContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

<h2>Index</h2>

<p>
    <%: Html.ActionLink("Create New", "Create") %>
</p>

<table>
    <tr>
        <th></th>
        <th>
            Complete
        </th>
        <th>
            Priority
        </th>
        <th>
            Name
        </th>
        <th>
            Description
        </th>
        <th>
            DueDate
        </th>
    </tr>

<% foreach (var item in Model) { %>
    <tr>
        <td>
            <%: Html.ActionLink("Edit", "Edit", new { id=item.ID }) %> |
            <%: Html.ActionLink("Details", "Details", new { id=item.ID }) %> |
            <%: Html.ActionLink("Delete", "Delete", new { id=item.ID }) %>
        </td>
        <td>
			<%: Html.CheckBox("Complete", item.Complete) %>
        </td>
        <td>
            <%: Html.DropDownList("PriorityID", ((IEnumerable<TaskStoreServerEntities.Priority>)ViewBag.PossiblePriorities).Select(option => new SelectListItem {
		        Text = (option == null ? "None" : option.Name), 
                Value = option.PriorityID.ToString(),
                Selected = (item != null) && (option.PriorityID == item.PriorityID)
            }), "Choose...") %>
	        <%: Html.ValidationMessage("PriorityID") %>
        </td>

        <td>
			<%: item.Name %>
        </td>
        <td>
			<%: item.Description %>
        </td>
        <td>
			<%: item.DueDate %> 
        </td>
    </tr>  
<% } %>

</table>

</asp:Content>

