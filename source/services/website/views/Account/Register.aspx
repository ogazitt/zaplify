<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Bootstrap.Master" Inherits="System.Web.Mvc.ViewPage<BuiltSteady.Zaplify.Website.Models.RegisterModel>" %>

<asp:Content ContentPlaceHolderID="MasterHead" runat="server">
    <title>Register</title>
    <style type="text/css">
      .field-validation-valid { display: none; }
    </style>

    <script type="text/javascript">
        // document ready handler
        $(function () {
        });
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="container-fluid">
    <div class="row-fluid">
    <div class="span1"></div>
    <div class="span10 well">
    <% using (Html.BeginForm("register", "account", FormMethod.Post, new { @class = "form-horizontal" })) { %>
            <fieldset>
                <legend>Register New User</legend>
                <div class="control-group">
                    <label class="control-label" for="Email">Email address</label>
                    <div class="controls">
                        <%: Html.TextBoxFor(m => m.Email, new { @class = "input-large" })%>
                        <%: Html.ValidationMessageFor(m => m.Email, "", new { @class="badge badge-important"})%>                 
                    </div>
                </div>

                <div class="control-group">
                    <label class="control-label" for="Password">Password</label>
                    <div class="controls">
                        <%: Html.PasswordFor(m => m.Password, new { @class = "input-large" })%>
                        <%: Html.ValidationMessageFor(m => m.Password, "", new { @class = "badge badge-important" })%>
                    </div>      
                </div> 
                
                <div class="control-group">
                    <label class="control-label" for="ConfirmPassword">Confirm password</label>
                    <div class="controls">
                        <%: Html.PasswordFor(m => m.ConfirmPassword, new { @class = "input-large" })%>
                        <%: Html.ValidationMessageFor(m => m.ConfirmPassword, "", new { @class = "badge badge-important" })%>
                    </div>      
                </div> 
                
                <div class="control-group">
                    <label class="control-label" for="AccessCode">Access code</label>
                    <div class="controls">
                        <%: Html.TextBoxFor(m => m.AccessCode, new { @class = "input-large" })%>
                        <%: Html.ValidationMessageFor(m => m.AccessCode, "", new { @class = "badge badge-important" })%>                 
                    </div>
                </div>
                                
                <div class="form-actions">
                    <button class="btn btn-primary" type="submit">Register</button>
                </div>
                <%: Html.ValidationSummary(true, "Unable to complete registration. Resolve issues and try again.", new { @class = "alert alert-error" })%>            </fieldset>

    <% } %>
    </div>
    </div>
    <div class="row-fluid">
    <div class="span1"></div>
    <div class="span10">
        <p>
        This product is under development and user registration is currently restricted.<br />
        To request an access code, register an email address at <a href="http://www.builtsteady.com">www.builtsteady.com</a>.
        </p>
    </div>
    </div>
    </div>
</asp:Content>
