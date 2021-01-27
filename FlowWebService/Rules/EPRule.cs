using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace FlowWebService.Rules
{
    public class EPRule:BaseRule,IRollBack
    {
        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;

        /// <summary>
        /// 获取此生产部门对应的设备部维修人员
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetRepairers(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string prDepName = (string)o["produce_dep_name"];
            var repairers = db.vw_ep_repairers.Where(e => e.pr_dep_name == prDepName).Select(e => e.repairer_num).ToArray();
            return string.Join(";", repairers);                          
        }

        /// <summary>
        /// 获取生产部门主管，作为服务评价人员
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetPrDepartmentCharger(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["produce_dep_charger_no"];
        }

        /// <summary>
        /// 获取设备经理，作为难度评分人员
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetEqDepartmentCharger(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["equitment_dep_charger_no"];
        }

        /// <summary>
        /// 在维修确认环节可以转移给其他维修工
        /// </summary>
        /// <param name="formJson"></param>
        public void ShouldTransferToOther(string formJson)
        {
            o = JObject.Parse(formJson);
            string sysNo = (string)o["sys_no"];
            string transferToRepairer = (string)o["transfer_to_repairer"];

            if (!string.IsNullOrEmpty(transferToRepairer)) {
                try {
                    var applyEntry = db.flow_applyEntry.Where(a => a.flow_apply.sys_no == sysNo && a.step_name.Contains("维修处理")).OrderByDescending(a => a.step).First();
                    var toAddEntry = new flow_applyEntryQueue();
                    toAddEntry.auditors = transferToRepairer;
                    toAddEntry.countersign = false;
                    toAddEntry.flow_template_entry_id = applyEntry.flow_template_entry_id;
                    toAddEntry.step = applyEntry.step + 1;
                    toAddEntry.step_name = "转移->维修处理";
                    toAddEntry.sys_no = sysNo;
                    db.flow_applyEntryQueue.InsertOnSubmit(toAddEntry);
                    db.SubmitChanges();
                }
                catch (Exception ex) {
                    throw new Exception("转移给其他维修人员失败：" + ex.Message);
                }
            }


        }


        public void RollBack(string sysNo, int step)
        {
            var apply = db.flow_apply.Where(a => a.sys_no == sysNo).FirstOrDefault();
            if (apply == null) {
                throw new Exception("此申请记录不存在");
            }
            var applyDetail = apply.flow_applyEntry.Where(a => a.step == step).FirstOrDefault();
            if (applyDetail == null) {
                throw new Exception("审批记录不存在");
            }
            if (applyDetail.pass == null) {
                throw new Exception("还未处理的不能收回");
            }
            if (apply.success != null) {
                throw new Exception("流程已完结，不能收回");
            }
            if (apply.flow_applyEntry.Where(a => a.step > step && a.step_name.Contains("评价") && a.pass == null).Count() > 0) {
                //下一步是服务评价且未处理的，才可以收回。表示此步是维修处理的步骤
                applyDetail.pass = null;
                applyDetail.final_auditor = null;
                applyDetail.audit_time = null;
                applyDetail.opinion = null;

                db.flow_applyEntry.DeleteAllOnSubmit(db.flow_applyEntry.Where(a => a.apply_id == apply.id && a.step > step && a.step_name.Contains("评价") && a.pass == null).ToList());
                try {
                    db.SubmitChanges();
                }
                catch (Exception ex) {
                    throw new Exception("操作失败："+ex.Message);
                }
            }
            else {
                throw new Exception("此处理步骤不支持收回操作");
            }
            
        }

    }
}