<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="aboutTitle" ContentPlaceHolderID="TitleContent" runat="server">
    About TaskStore
</asp:Content>

<asp:Content ID="cssContent" ContentPlaceHolderID="ScriptContent" runat="server">
    <link href="/Content/About.css" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content ID="aboutContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="aboutbody">
        <% Html.RenderPartial("Navbar"); %>
        <div class="contentArea">
            <h2>About TaskStore</h2>
            <div id="contentAreaBox">
                <div class="overlay"></div>
                <div class="tdcol3">
				    <h3>A new kind of task organizer.  </h3>
                    While other tasklist applications merely help you track your to-do list, 
                    TaskStore helps you complete your tasks by making valuable suggestions.
				    <br/>
                    <br/>
			    </div>
                <span class="clear"></span>
            </div>
        </div>
    </div>
</asp:Content>
