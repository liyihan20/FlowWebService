using FlowWebService.Models;
using Newtonsoft.Json.Linq;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 辅料申购申请流程
    /// </summary>
    public class APRule:BaseRule
    {        
        JObject o;

        /// <summary>
        /// 部门主管
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetCharger(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["charger_no"];
        }

        /// <summary>
        /// 物控
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetController(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["controller_no"];
        }

        /// <summary>
        /// 事业部长
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetMinister(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["minister_no"];
        }

        /// <summary>
        /// PR下单人
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetPRBiller(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["order_no"];
        }

    }
}