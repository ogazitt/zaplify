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
            <h2>Team</h2>
            <div id="contentAreaBox">
                <div class="overlay"></div>
                <div class="tdcol3">
                	<div class="leadershipDivTop">
                        <a href="http://www.linkedin.com/pub/omri-gazitt/0/b4/923">
                            <img class="img-left" alt="Omri Gazitt" src="/Images/omri-small.png" width="150" height="150"/>
                        </a>
                        <a name="#Omri"></a><h3 class="nomargin">Omri Gazitt</h3>
                        <h5>Founder &amp; CEO</h5>
                            Omri Gazitt is the Founder and CEO of TaskStore. 
                            Prior to founding TaskStore, Omri spent 13 years at 
                            Microsoft working on .NET, Visual Studio, Windows Server, 
                            SQL Server, the Azure Services Platform, and Xbox.  &nbsp;
						<a href="http://www.linkedin.com/pub/omri-gazitt/0/b4/923">view full profile</a>
					</div>	
                	<div class="leadershipDivBottom">
                        <a href="#NewMember">
                            <img class="img-left" alt="Your pic here!" src="" width="150" height="150"/>
                        </a>
                        <a name="#NewMember"></a><h3 class="nomargin">Your Name Here!</h3>
                        <h5>Software Engineer</h5>
                            Join TaskStore! :-)
                            &nbsp;
					</div>				
<!--
            	    <div style="width: 100%;" class="clear">
                	    <div class="leadershipDivLeft">
                            <a href="http://www.linkedin.com/profile/view?id=236331&trk=tab_pro">
                                <img class="img-left" alt="Omri Gazitt" src="/Images/omri-small.png" width="150" height="150"/>
                            </a>
                            <a name="#Omri"></a><h3 class="nomargin">Omri Gazitt</h3>
                            <h5>Founder &amp; CEO</h5>
                                Omri Gazitt is the Founder and CEO of TaskStore. 
                                Prior to founding TaskStore, Omri spent 13 years at 
                                Microsoft working on .NET, Visual Studio, Windows Server, 
                                SQL Server, the Azure Services Platform, and Xbox.  &nbsp;
						    <a href="http://www.linkedin.com/profile/view?id=236331&trk=tab_pro">view full profile</a>
					    </div>				
					    <div class="leadershipDivRight">
                            <a href="#NewMember">
                                <img class="img-left" alt="Your pic here!" src="" width="150" height="150"/>
                            </a>
                            <a name="#NewMember"></a><h3 class="nomargin">Your Name Here!</h3>
                            <h5>Software Engineer</h5>
                                Join TaskStore! :-)
                                &nbsp;
					    </div>
					</div>
				    <br/>
				    <br/>
            	    <div style="width: 100%;" class="clear">
                	    <div class="leadershipDivLeft">
					    </div>
			
					    <div class="leadershipDivRight">
					    </div>
				    </div>
				    <br/>
-->
				</div>
			</div>
            <span class="clear"></span>
        </div>
    </div>
</asp:Content>
