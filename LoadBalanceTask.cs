using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Sitefinity.Scheduling;

namespace Aptera.Azure.LoadBalancing
{
    public class LoadBalanceTask : ScheduledTask
    {
	   public const string GuidKey = "Aptera.Azure.LoadBalancing.LoadBalanceTask";
 
	   public LoadBalanceTask()
	   {
		  this.Key = GuidKey;
	   }
 
	   public override void ExecuteTask()
	   {
		  LoadBalance.MaintainLoadBalancingUrls();
		  //Reset Task
		  SchedulingManager schedulingManager = SchedulingManager.GetManager();
		  
		  var newTask = new LoadBalanceTask()
		  {
			 Key = this.Key,
			 ExecuteTime = DateTime.UtcNow.AddMinutes(5)
		  };
		  schedulingManager.AddTask(newTask);
		  schedulingManager.SaveChanges();
	   }
		  
	   public override string TaskName
	   {
		  get
		  {
			 return "Aptera.Azure.LoadBalancing.LoadBalanceTask, Aptera.Azure.LoadBalancing";
		  }
	   }
    }
}
