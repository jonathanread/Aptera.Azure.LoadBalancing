Built on: 
Sitefinity 7.0.5100.
.Net 4.5
Azure SDK 2.2


Add the project to your Sitefinity Solution.  Add the reference to the SitefinityWebApp project.  

In the global.asax add use:

protected void Application_Start(object sender, EventArgs e)
{
   Bootstrapper.Initialized += new EventHandler<ExecutedEventArgs>(Bootstrapper_Initialized);
}

protected void Bootstrapper_Initialized(object sender, ExecutedEventArgs e)
{
	if (e.CommandName == "Bootstrapped")
	{
		Aptera.Azure.LoadBalancing.LoadBalance.InitializeLoadBalancingMaintenance();
	}
}

