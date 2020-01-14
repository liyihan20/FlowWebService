using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 仓库管理员权限申请流程
    /// </summary>
    public class SARule:BaseRule,IBeforeStartFlow,IFinishFlow
    {
        FlowDBDataContext db = new FlowDBDataContext();

        public void Validate(string formObj, string createUser)
        {
            
        }

        public void DoBeforeFlow(string formObj)
        {
            
        }

        //获取仓库管理员
        public string GetStockAuditor(flow_apply app, string formObj)
        {
            var o = JObject.Parse(formObj);

            return (string)o["stock_auditor_num"];
        }

        public void DoAfterFlowSucceed(string formObj, flow_apply app)
        {
            var o = JObject.Parse(formObj);
            string accountName = (string)o["k3_account_name"];
            string stockNumber = (string)o["k3_stock_num"];
            string cardNumber = (string)o["applier_num"];

            //设置管理员权限到K3 
            db.SetK3StockAdmin(accountName, stockNumber, cardNumber);            
            
        }

        public void DoAfterFlowFailed(string formObj, flow_apply app)
        {
            
        }
    }
}