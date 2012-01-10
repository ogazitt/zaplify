<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<TaskStoreWeb.Models.LogOnModel>" %>

<script src="<%: Url.Content("~/Scripts/jquery.validate.min.js") %>" type="text/javascript"></script>
<script src="<%: Url.Content("~/Scripts/jquery.validate.unobtrusive.min.js") %>" type="text/javascript"></script>

<% using (Html.BeginForm()) { %>
    <div id="logincontrol">
        <fieldset>
            <legend>Login (or <%: Html.ActionLink("sign up", "Register", "Account") %> for a new free account)</legend>
        
            <%: Html.ValidationSummary(true, "") %>
                
            <div class="editor-label">
                <%: Html.LabelFor(m => m.UserName) %>
            </div>
            <div class="editor-field">
                <%: Html.TextBoxFor(m => m.UserName) %>
                <%: Html.ValidationMessageFor(m => m.UserName) %>
            </div>
                
            <div class="editor-label">
                <%: Html.LabelFor(m => m.Password) %>
            </div>
            <div class="editor-field">
                <%: Html.PasswordFor(m => m.Password) %>
                <%: Html.ValidationMessageFor(m => m.Password) %>
            </div>
                
            <div class="editor-label">
                <%: Html.CheckBoxFor(m => m.RememberMe) %>
                <%: Html.LabelFor(m => m.RememberMe) %>
            </div>
                
            <p>
            </p>
            <input type="submit" value="Log In" />

        </fieldset>
    </div>
<% } %>
    
<script type="text/javascript">
    $(function () {
        $("input:submit").button();
    });
</script>
