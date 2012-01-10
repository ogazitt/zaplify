<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>

<div class="subnav">

    <ul>
	    <li><a id="about-About" href="About">About TaskStore</a></li>
	    <li><a id="about-Team" href="Team">Team</a></li>
        <li><a id="about-News" href="News">News</a></li>
        <li><a id="about-Blog" href="Blog">Blog</a></li>
        <li><a id="about-Jobs" href="Jobs">Jobs</a></li>
        <li><a id="about-Contact" href="Contact">Contact</a></li>
    </ul>
 
    <div>
    </div>

    <% string str = Request.Url.Segments[Request.Url.Segments.Count() - 1]; %>
    <script language="javascript">
        $(document).ready(function () {
            $("li a#about-<%: str %>").addClass("active");
        });
    </script>

</div>
