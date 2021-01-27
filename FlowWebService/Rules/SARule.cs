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

        //信息管理部开通
        public string GetIMAuditor(flow_apply app, string formObj)
        {
            var o = JObject.Parse(formObj);
            string account = (string)o["k3_account_name"];

            //光电总部、半导体总部、电子、科技、仁寿经过林剑辉，其它帐套不用 2020-12-14
            foreach (var a in new string[] { "光电股份有限公司总部", "半导体总部", "信利电子", "光电科技", "光电仁寿" }) {
                if (account.Contains(a)) {
                    return "201019001";
                }
            }
            return "";
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