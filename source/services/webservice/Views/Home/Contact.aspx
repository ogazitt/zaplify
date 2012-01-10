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
            <h2>Contact</h2>
            <div id="contentAreaBox">
                <div class="overlay"></div>
                <div class="tdcol3">
       	            <div style="width: 100%;" class="clear">
                        <strong>Main Office</strong>
                        <br/>
                        <a title="Map of TaskStore offices" href="maps:7004%20218th%20Ave%20NE,%20Redmond%20WA%2098053" target="_blank">
                            7004 218th Ave NE
                            <br/>
                            Redmond, WA 98053
                        </a>
				        <br/>
				        (425) 765-0079
                        <br/>
				        <a href="mailto:info@taskstore.net">info@taskstore.net</a>
				        <br/>
				        <br/>
			        </div>	
                    <span class="clear"></span>
                </div>
			</div>
        </div>
    </div>
</asp:Content>
