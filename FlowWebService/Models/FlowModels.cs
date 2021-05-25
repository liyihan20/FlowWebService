using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlowWebService.Models
{
    public class FlowRecordModels
    {
        public int? step { get; set; }
        public string stepName { get; set; }
        public string auditors { get; set; }
        public string auditTimes { get; set; }
        public string auditResult { get; set; }
        public string opinions { get; set; }
    }

    public class FlowAuditListModel
    {
        public DateTime? applyTime { get; set; }
        public string sysNo { get; set; }
        public string billType { get; set; }
        public string processName { get; set; }
        public string title { get; set; }
        public string subTitle { get; set; }
        public string applier { get; set; }
        public int? step { get; set; }
        public string stepName { get; set; }
        public string auditResult { get; set; }
        public string finalResult { get; set; }
        public string auditors { get; set; }
        public string finalAuditors { get; set; }
        public bool isUserAobrt { get; set; }
        public string opinion { get; set; }
    }

    public class FlowMyAppliesModel
    {
        public DateTime? applyTime { get; set; }
        public string sysNo { get; set; }
        public string title { get; set; }
        public string subTitle { get; set; }
        public string billType { get; set; }
        public string auditResult { get; set; }
        public DateTime? finishTime { get; set; }
        public bool isUserAobrt { get; set; }
    }

    public class FlowResultModel
    {
        public FlowResultModel()
        {
            this.suc = false;
        }
        public FlowResultModel(bool suc)
        {
            this.suc = suc;
        }
        public FlowResultModel(bool suc, string msg)
        {
            this.suc = suc;
            this.msg = msg;
        }
        public FlowResultModel(bool suc, string msg, string nextAuditors)
        {
            this.suc = suc;
            this.msg = msg;
            this.nextAuditors = nextAuditors;
        }

        public bool suc { get; set; }
        public string msg { get; set; }
        public string nextAuditors { get; set; }
    }

    public class CurrentAuditModel
    {
        public int step { get; set; }
        public string stepName { get; set; }
        public string auditors { get; set; }
    }    

    public class HasAuditModel
    {
        public bool suc { get; set; }
        public bool? isPass { get; set; }
        public string opinion { get; set; }
        public string msg { get; set; }
        public string stepName { get; set; }

        public HasAuditModel()
        {

        }
        public HasAuditModel(bool suc, string msg)
        {
            this.suc = suc;
            this.msg = msg;
        }

        public HasAuditModel(bool suc, bool? isPass, string opinion,string stepName)
        {
            this.suc = suc;
            this.isPass = isPass;
            this.opinion = opinion;
            this.stepName = stepName;
        }
    }

}