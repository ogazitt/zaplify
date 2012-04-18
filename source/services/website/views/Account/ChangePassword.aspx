<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<ChangePasswordModel>" %>
<%@ Import Namespace="BuiltSteady.Zaplify.Website.Models" %>

<asp:Content ID="changePasswordTitle" ContentPlaceHolderID="MasterHead" runat="server">
    <title>Change Password</title>
    <script src="<%: Url.Content("~/Scripts/jquery.validate.min.js") %>" type="text/javascript"></script>
    <script src="<%: Url.Content("~/Scripts/jquery.validate.unobtrusive.min.js") %>" type="text/javascript"></script>

    <script type="text/javascript">
        // document ready handler
        $(function () {
            $('.ui-button').hover(function () { $(this).addClass('ui-state-hover'); }, function () { $(this).removeClass('ui-state-hover'); });
        });
    </script>
</asp:Content>

<asp:Content ID="changePasswordContent" ContentPlaceHolderID="MainContent" runat="server">
<div class="dialog-panel ui-widget ui-widget-content ui-corner-all">
    <h2>Change Password</h2>

    <% using (Html.BeginForm()) { %>
        <div>
            <fieldset>
                <legend>User Information</legend>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.OldPassword) %>
                </div>
                <div class="dialog-field">
                    <%: Html.PasswordFor(m => m.OldPassword) %>
                    <%: Html.ValidationMessageFor(m => m.OldPassword) %>
                </div>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.NewPassword) %>
                </div>
                <div class="dialog-field">
                    <%: Html.PasswordFor(m => m.NewPassword) %>
                    <%: Html.ValidationMessageFor(m => m.NewPassword) %>
                </div>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.ConfirmPassword) %>
                </div>
                <div class="dialog-field">
                    <%: Html.PasswordFor(m => m.ConfirmPassword) %>
                    <%: Html.ValidationMessageFor(m => m.ConfirmPassword) %>
                </div>
                
                <input class="ui-button ui-state-default" type="submit" value="Change Password" />
            </fieldset>
        </div>
        <%: Html.ValidationSummary(true, "Unable to change password. Resolve issues and try again.") %>
    <% } %>
</div>
</asp:Content>
