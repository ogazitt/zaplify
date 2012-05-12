<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<BuiltSteady.Zaplify.Website.Models.RegisterModel>" %>

<asp:Content ContentPlaceHolderID="MasterHead" runat="server">
    <title>Register</title>
    <script src="<%: Url.Content("~/Scripts/jquery.validate.min.js") %>" type="text/javascript"></script>
    <script src="<%: Url.Content("~/Scripts/jquery.validate.unobtrusive.min.js") %>" type="text/javascript"></script>

    <script type="text/javascript">
        // document ready handler
        $(function () {
            $('.ui-button').hover(function () { $(this).addClass('ui-state-hover'); }, function () { $(this).removeClass('ui-state-hover'); });
        });
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
<div class="dialog-panel ui-widget ui-widget-content ui-corner-all">
    <h2>Register New User</h2>

    <% using (Html.BeginForm()) { %>
        <div>
            <fieldset>
                <legend>User Information</legend>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.Email) %>
                </div>
                <div class="dialog-field">
                    <%: Html.TextBoxFor(m => m.Email) %>
                    <%: Html.ValidationMessageFor(m => m.Email) %>
                </div>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.Password) %>
                </div>
                <div class="dialog-field">
                    <%: Html.PasswordFor(m => m.Password) %>
                    <%: Html.ValidationMessageFor(m => m.Password) %>
                </div>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.ConfirmPassword) %>
                </div>
                <div class="dialog-field">
                    <%: Html.PasswordFor(m => m.ConfirmPassword) %>
                    <%: Html.ValidationMessageFor(m => m.ConfirmPassword) %>
                </div>

                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.AccessCode) %>
                </div>
                <div class="dialog-field">
                    <%: Html.TextBoxFor(m => m.AccessCode) %>
                    <%: Html.ValidationMessageFor(m => m.AccessCode) %>
                </div>
                                
                <input class="ui-button ui-state-default" type="submit" value="Register" />
            </fieldset>
        </div>
        <%: Html.ValidationSummary(true, "Unable to complete registration. Resolve issues and try again.") %>
    <% } %>
    <p>
    The product is under development and user registration is currently restricted.<br />
    To request an access code, register an email address at <a href="http://www.builtsteady.com">www.builtsteady.com</a>.
    </p>
</div>
</asp:Content>
