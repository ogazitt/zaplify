<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Bootstrap.Master" Inherits="System.Web.Mvc.ViewPage<SignInModel>" %>
<%@ Import Namespace="BuiltSteady.Zaplify.Website.Models" %>

<asp:Content ContentPlaceHolderID="MasterHead" runat="server">
    <title>Sign-in</title>
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
    <% using (Html.BeginForm("signin", "account", FormMethod.Post, new { @class = "form-horizontal" })) { %>
            <fieldset>
                <legend>Sign In</legend>
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
                    <div class="controls">
                        <label class="checkbox">
                            <%: Html.CheckBoxFor(m => m.RememberMe)%>
                            Remember Me?
                        </label>
                    </div>
                </div>
                              
                <div class="form-actions">
                    <button class="btn btn-primary" type="submit">Sign-in</button>
                </div>

                <%: Html.ValidationSummary(true, "Invalid or unrecognized email or password. Please try again.", new { @class = "alert alert-error" })%>

            </fieldset>
        </div>
        </div>
        </div>
    <% } %>

</asp:Content>
