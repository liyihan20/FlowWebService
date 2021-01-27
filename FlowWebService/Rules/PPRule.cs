using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 宿舍公共区域维修流程
    /// </summary>
    public class PPRule:BaseRule,IFinishFlow
    {

        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;

        public string GetAreaAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string areaName = (string)o["area_name"];

            if (areaName.Contains("红草")) {
                return "06020610"; //林敬森
            }
            else {
                return "06112002"; //卢政锐
            }
        }

        //流程结束后操作库存和出库记录
        public void DoAfterFlowSucceed(string formObj, Models.flow_apply app)
        {            
            o = JObject.Parse(formObj);
            string sysNo = (string)o["sys_no"];

            //插入出库记录和减去库存
            db.DP_InsertRepairItemRecord(sysNo);
        }

        public void DoAfterFlowFailed(string formObj, Models.flow_apply app)
        {
            
        }
    }
}