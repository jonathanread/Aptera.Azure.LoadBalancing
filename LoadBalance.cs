using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.LoadBalancing.Configuration;
using Telerik.Sitefinity.Scheduling;
using Telerik.Sitefinity.Services;

namespace Aptera.Azure.LoadBalancing
{
    public class LoadBalance
    {
	   #region Properties
	
	   /// <summary>
	   /// Gets Azure IPs
	   /// </summary>
	   private static IEnumerable<string> SitefinityInternalEndpointIPs
	   {
		  get
		  {
			 var ips = new List<string>();
			 try
			 {
				var currentRole = RoleEnvironment.Roles["SitefinityWebApp"];
				if (currentRole != null)
				{
				    foreach (var instance in currentRole.Instances)
				    {
					   string ip = GetInternalIP(instance);
					   if (!ip.IsNullOrEmpty())
					   {
						  ips.Add(ip);
					   }
				    }
				}
			 }
			 catch 
			 {
				//used to surpress exception so it can be ran outside Azure
			 }
			 return ips;
		  }
	   }
	   
	   /// <summary>
	   /// Gets/Sets IPs from Sitefinity Config 
	   /// </summary>
	   private static IEnumerable<string> ConfiguredUrls
	   {
		  get
		  {
			 try
			 {
				var lbConfig = Config.Get<SystemConfig>()
								 .LoadBalancingConfig;
				
				if (lbConfig != null && lbConfig.URLS != null)
				{
				    return lbConfig.URLS
							    .Select<InstanceUrlConfigElement, String>(entry => entry.Value)
							    .ToList();
				}
				else
				    return new List<String>();
			 }
			 catch (Exception ex)
			 {
				ex.ToString();
				return new List<String>();
			 }
		  }
		  set
		  {
			 try
			 {
				ConfigManager manager = Config.GetManager();
				SystemConfig section = manager.GetSection<SystemConfig>();
				
				if (section.LoadBalancingConfig == null)
				{
				    section.LoadBalancingConfig = new LoadBalancingConfig(section);
				}
				
				var instanceUrlList = new ConfigElementList<InstanceUrlConfigElement>(section.LoadBalancingConfig);
				
				if (value != null)
				{
				    foreach (var url in value.ToList())
				    {
					   var elem = new InstanceUrlConfigElement(instanceUrlList);
					   elem.Value = url;
					   instanceUrlList.Add(elem);
				    }
				}
				
				section.LoadBalancingConfig.URLS = instanceUrlList;
				manager.Provider.SuppressSecurityChecks = true;
				manager.SaveSection(section);
				manager.Provider.SuppressSecurityChecks = false;
			 }
			 catch
			 { // suppress exceptions
			 }
		  }
	   }
	   
	   #endregion
	   
	   #region Methods
	    /// <summary>
	   /// Get Azure RoleInstance IP
	   /// </summary>
	   /// <param name="ri"></param>
	   /// <returns></returns>
	   private static string GetInternalIP(RoleInstance ri)
	   {
		  var internalEndpoint = ri.InstanceEndpoints["SitefinityInternalEndpoint"];
		  if (internalEndpoint != null)
		  {
			 return internalEndpoint.IPEndpoint.Address.ToString();
		  }
		  else
		  {
			 return String.Empty;
		  }
	   }

	   /// <summary>
	   /// Runs the load balace config editor and schedules a task.
	   /// </summary>
	   public static void InitializeLoadBalancingMaintenance()
	   {
		  MaintainLoadBalancingUrls();
		  CreateMaintainLoadBalancingUrlsTask();
	   }
	   
	   /// <summary>
	   /// Maintian URLs based on the number of Azure instances
	   /// </summary>
	   /// <param name="removeThisInstance"></param>
	   public static void MaintainLoadBalancingUrls(bool removeThisInstance = false)
	   {
		  var liveUrls = SitefinityInternalEndpointIPs.Select(ip => "http://" + ip + "/");
		  var configuredUrls = ConfiguredUrls;
		  
		  if (removeThisInstance)
		  {
			 liveUrls = liveUrls.Except(new string[] { "http://" + GetInternalIP(RoleEnvironment.CurrentRoleInstance) + "/" });
		  }
		  
		  if (liveUrls.Except(configuredUrls).Count() > 0 // there are running role instances that are not in the configs
			 ||
			 configuredUrls.Except(liveUrls).Count() > 0 // there are configed role instances that are no longer running
			 ||
			 liveUrls.Count() == 1 // there is only one running role instance				  
		  )
		  { // update the load balancing configs
			 if (liveUrls.Count() > 1)
			 {
				ConfiguredUrls = liveUrls;
			 }
			 else
			 { 
				ConfiguredUrls = new List<String>();
			 }
		  }
	   }
	   
	   internal static void CreateMaintainLoadBalancingUrlsTask()
	   {
		  SchedulingManager manager = SchedulingManager.GetManager();
		  
		  var key = LoadBalanceTask.GuidKey;
		  
		  var existingTasks = manager.GetTaskData().Where(i => i.Key == key).ToList();
		  if (existingTasks.Count() > 0)
		  {
			 foreach (var task in existingTasks)
			 {
				manager.DeleteTaskData(task);
			 }
			 manager.SaveChanges();
		  }
		  
		  existingTasks = manager.GetTaskData().Where(i => i.Key == key).ToList();
		  
		  if (existingTasks.Count() == 0)
		  {
			 var newTask = new LoadBalanceTask();
			 
			 //Execution time  must be expressed in UTC                		
			 newTask.ExecuteTime = DateTime.UtcNow.AddMinutes(1);
			 newTask.Key = LoadBalanceTask.GuidKey;
			 manager.AddTask(newTask);
			 manager.SaveChanges();
		  }
	   }
	   
	  
    
	   #endregion
    }
}
