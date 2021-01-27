using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 请假流程
    /// 这个没有固定的流程模板，大部分的审批节点只能在代码中动态生成，连第一级审批都是未知的
    /// </summary>
    public class ALRule:BaseRule,IBeforeStartFlow,IFinishFlow,IFlowQueue
    {
        JObject o;
        FlowDBDataContext db = new FlowDBDataContext();
        private const string PROCESSNAME = "请假";

        public void Validate(string formObj, string createUser)
        {
            if (db.flow_apply.Where(a => a.create_user == createUser && a.flow_template.bill_type=="AL" && a.success == null).Count() > 0) {
                throw new Exception("存在未完成的请假申请流程，结束之前不能再次申请");
            }
        }

        public void DoBeforeFlow(string formObj)
        {
            o = JObject.Parse(formObj);
            string auditorQueues = (string)o["auditor_queues"];
            if (string.IsNullOrEmpty(auditorQueues)) {
                //流程开始之前，先将审核人队列准备好
                var queue = GetALAuditQueue(formObj);
                db.flow_applyEntryQueue.InsertAllOnSubmit(queue);
                db.SubmitChanges();
            }
            else {
                List<flow_applyEntryQueue> list = JsonConvert.DeserializeObject<List<flow_applyEntryQueue>>(auditorQueues);
                db.flow_applyEntryQueue.InsertAllOnSubmit(list);
                db.SubmitChanges();
            }
        }

        public void DoAfterFlowSucceed(string formObj,flow_apply apply)
        {
            o = JObject.Parse(formObj);
            string cardNo = (string)o["applier_num"];
            string agentNumber = (string)o["agent_man"];
            DateTime beginTime = (DateTime)o["from_date"];
            DateTime endTime = (DateTime)o["to_date"];

            //如果申请了撤销，最后审批成功，需要将apply的success状态改为false，表示此请假流程是无效的
            if (apply.user_abort == true) {
                apply.success = false;
            }

            //如果有代理人，插入代理表
            if (!string.IsNullOrEmpty(agentNumber)) {
                ei_workAgent agent = new ei_workAgent();
                agent.agent_number = agentNumber;
                agent.emp_number = cardNo;
                agent.begin_time = beginTime;
                agent.end_time = endTime;
                db.ei_workAgent.InsertOnSubmit(agent);
                db.SubmitChanges();
            }            

        }

        public void DoAfterFlowFailed(string formObj, flow_apply apply)
        {
            //如果申请了撤销，最后审批失败，需要将apply的success状态改为true，表示此请假流程是有效的
            if (apply.user_abort == true) {
                apply.success = true;
            }
        }

        private List<flow_applyEntryQueue> GetALAuditQueue(string formObj)
        {
            List<flow_applyEntryQueue> list = new List<flow_applyEntryQueue>();

            o = JObject.Parse(formObj);
            string sysNo = (string)o["sys_no"];
            string cardNo=(string)o["applier_num"];
            string depNo = (string)o["dep_no"];
            int empLevel = (int)o["emp_level"];
            bool isDirectCharge = ((bool?)o["is_direct_charge"]) ?? false;
            string leaveType = (string)o["leave_type"];
            int workDays = (int)o["work_days"];
            decimal workHours = (decimal)o["work_hours"];
            bool isContinue = ((bool?)o["is_continue"]) ?? false;

            //走集团流程的部门
            bool isCopFlow = false;
            string[] copDepNames = new string[] { "信利工业有限公司", "信利仪器有限公司" };
            foreach (var d in db.ei_department.Where(d => copDepNames.Contains(d.FName)).ToList()) {
                if (depNo.StartsWith(d.FNumber)) {
                    isCopFlow = true;
                    break;
                }
            }

            ei_department AHDep = GetNearestParentDepByNodeName(depNo, "AH审批", PROCESSNAME); //最近的AH审批节点
            int stepNum = 1;

            if (isCopFlow) {
                //集团流程
                string presidentNo;
                try {
                    presidentNo = db.ei_department.Where(d => d.FName == "总裁办").First().FNumber; //总裁办
                }
                catch {
                    throw new Exception("[总裁办]部门不存在");
                }
                
                if (empLevel < 1) {
                    //组长以下，至少需要本部门和上一级部门负责人审批 
                    var n0 = GetParentDepAuditor(depNo, PROCESSNAME, 0, false);                    
                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);

                    var n1 = GetParentDepAuditor(depNo, PROCESSNAME, 1);
                    if (n1 != null) {
                        n1.sys_no = sysNo;
                        n1.step = stepNum++;
                        list.Add(n1);
                    }
                   
                    if (workDays >= 2)  {
                        //大于2天，AH，上上级部门审批
                        if (AHDep != null) {
                            var n2 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                            n2.sys_no = sysNo;
                            n2.step = stepNum++;
                            list.Add(n2);
                        }

                        var n3 = GetParentDepAuditor(depNo, PROCESSNAME, 2);
                        if (n3 != null) {
                            n3.sys_no = sysNo;
                            n3.step = stepNum++;
                            list.Add(n3);
                        }                        
                    }
                }
                else if (empLevel < 7) {
                    //组长以上，经理/主管以下，至少需要本部门负责人和行政/HR审批
                    var n0 = GetParentDepAuditor(depNo, PROCESSNAME, 0, false);
                    if (n0.step_name.Contains("组长")) {
                        n0 = GetParentDepAuditor(depNo, PROCESSNAME, 1, false); //如果当前选择的部门是组长审批的，此职位级别要上级部门审批
                    }
                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);

                    if (AHDep != null) {
                        var n1 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                        n1.sys_no = sysNo;
                        n1.step = stepNum++;                        
                        list.Add(n1);
                    }                    

                    if (workDays >= 0) {
                        //需要上上级部门负责人审批
                        var n2 = GetParentDepAuditor(depNo, PROCESSNAME, 1);
                        if (n2 != null) {
                            n2.step = stepNum++;
                            n2.sys_no = sysNo;
                            list.Add(n2);
                        }
                    }
                }
                else {
                    //经理/主管以上，总裁办审批
                    var n0 = GetGivenDepAuditor(presidentNo, PROCESSNAME);
                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);
                }

            }
            else {
                //光电和半导体的请假流程
                var ceoDep = GetNearestDep(depNo, "董事办公室");
                if (ceoDep == null) {
                    ceoDep = GetNearestDep(depNo, "总经理办公室"); //没有董事办公室，看有没有总经理办公室
                }

                if (empLevel < 1) {
                    //组长以下，必须本部门和上级部门审批
                    var n0 = GetParentDepAuditor(depNo, PROCESSNAME, 0,false);
                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);

                    var n1 = GetParentDepAuditor(depNo, PROCESSNAME, 1);
                    if (n1 != null) {
                        if (list.Where(l => l.auditors.Contains(n1.auditors)).Count() < 1) { //如果此节点审核人不存在已之前步骤的审批人中，即加入
                            n1.sys_no = sysNo;
                            n1.step = stepNum++;
                            list.Add(n1);
                        }
                    }

                    if (workDays >= 10) {
                        //10天以上需要上上级审批
                        var n2 = GetParentDepAuditor(depNo, PROCESSNAME, 2);
                        if (n2 != null) {
                            if (list.Where(l => l.auditors.Contains(n2.auditors)).Count() < 1) { //如果此节点审核人不存在已之前步骤的审批人中，即加入
                                n2.sys_no = sysNo;
                                n2.step = stepNum++;
                                list.Add(n2);
                            }
                        }
                    }
                    if (workDays >= 15) {
                        //15天以上需要AH部审批
                        if (AHDep != null) {
                            var n3 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                            n3.sys_no = sysNo;
                            n3.step = stepNum++;
                            list.Add(n3);
                        }
                    }
                }
                else if (empLevel < 7) {
                    //组长以上，经理/主管以下，至少需要本部门负责人审批
                    int skipNum = 0;
                    var n0 = GetParentDepAuditor(depNo, PROCESSNAME, skipNum, false);
                    if (n0.step_name.Contains("组长")) {
                        n0 = GetParentDepAuditor(depNo, PROCESSNAME, ++skipNum, false); //如果当前选择的部门是组长审批的，此职位级别要上级部门审批
                    }

                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);

                    if (workDays >= 3) { 
                        //3天以上需要上一级负责人审批
                        var n1 = GetParentDepAuditor(depNo, PROCESSNAME, ++skipNum);                        
                        if (n1 != null) {
                            if (list.Where(l => l.auditors.Contains(n1.auditors)).Count() < 1) { //如果此节点审核人不存在已之前步骤的审批人中，即加入
                                n1.sys_no = sysNo;
                                n1.step = stepNum++;
                                list.Add(n1);
                            }
                        }
                    }
                    if (workDays >= 10) {
                        //10天以上需要AH部审批
                        if (AHDep != null) {
                            var n2 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                            n2.sys_no = sysNo;
                            n2.step = stepNum++;
                            list.Add(n2);
                        }
                    }
                }
                else {
                    //经理/主管以上
                    if (isDirectCharge) {
                        //直管
                        if (ceoDep == null) {
                            throw new Exception("[董事办公室]审核人未设置");
                        }
                        var n0 = GetGivenDepAuditor(ceoDep, PROCESSNAME);
                        n0.sys_no = sysNo;
                        n0.step = stepNum++;
                        list.Add(n0);
                    }
                    else {
                        //不是直管，都要上一级部门负责人审批
                        var n0 = GetParentDepAuditor(depNo, PROCESSNAME, 1,false);
                        n0.step = stepNum++;
                        n0.sys_no = sysNo;
                        list.Add(n0);

                        if (workDays > 3) {
                            //3天以上需要AH部审批
                            if (AHDep != null) {
                                var n1 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                                n1.sys_no = sysNo;
                                n1.step = stepNum++;
                                list.Add(n1);
                            }
                        }
                        if (workDays >= 10) {
                            //10天以上需要董事总经理审批
                            if (ceoDep != null) {
                                var n2 = GetGivenDepAuditor(ceoDep, PROCESSNAME,true);
                                if (n2 != null) {
                                    n2.sys_no = sysNo;
                                    n2.step = stepNum++;
                                    list.Add(n2);
                                }
                            }
                        }
                    }
                }
            }

            //病假60天内累计请假大于30天，或除了产假、病假之外，其它假60天内累计大于15天,需要行政部审批
            DateTime twoMonthsAgo = DateTime.Now.AddMonths(-2);
            string administrationNo = ""; //集团,行政审批
            //惠州的由惠州那边行政负责
            if (depNo.StartsWith("106")) {
                administrationNo = "106";
            }
            else if (depNo.StartsWith("4")) {
                administrationNo = "4";
            }
            else {
                administrationNo = "1";
            }
            //else if (workDays <= 20) { //最近1年没有请过假的，在过年的月份里，请假少于20天的不需要经过行政部
            //    if (DateTime.Now.Month == 1 || DateTime.Now.Month == 2) {
            //        var lastYear = DateTime.Parse(DateTime.Now.AddYears(-1).ToString("yyyy-02-01"));
            //        if (db.flow_apply.Where(f => f.create_user == cardNo && f.start_date >= lastYear && f.sys_no!=sysNo).Count() == 0) {
            //            return list;
            //        }
            //    }
            //}
            
            if ("产假延期".Equals(leaveType)) {
                //选择了产假延期，不管多少天，都要经过行政部审批
                var ad = GetGivenDepAuditor(administrationNo, PROCESSNAME);
                ad.sys_no = sysNo;
                ad.step = stepNum++;
                list.Add(ad);
            }else if ("病假".Equals(leaveType)) {
                //2019-10-17起，只要病假请假天数大于10天，就需要行政部审批
                if (workDays >= 10) {
                    var ad = GetGivenDepAuditor(administrationNo, PROCESSNAME);
                    ad.sys_no = sysNo;
                    ad.step = stepNum++;
                    list.Add(ad);
                }
            }
            else if ("工伤".Equals(leaveType)) {
                //2020-08-17 增加工伤假审批,光电仁寿到袁大军，其它的到锡标                
                string copName;
                if (depNo.StartsWith("4")) {
                    copName = "光电仁寿";
                }
                else {
                    copName = "集团";
                }
                string auditors = string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == "AL" && f.relate_name == "工伤假审批" && f.relate_text == copName).Select(f => f.relate_value).ToList());

                var ad = new flow_applyEntryQueue();
                ad.auditors = auditors;
                ad.countersign = false;
                ad.step = stepNum++;
                ad.step_name = "行政部确认";
                ad.sys_no = sysNo;
                list.Add(ad);
            }
            else if (!"产假".Equals(leaveType) && !"年假".Equals(leaveType)) {
                if (workDays >= 15) {
                    //单次请假大于15天的，需要到行政审批
                    var ad = GetGivenDepAuditor(administrationNo, PROCESSNAME);
                    ad.sys_no = sysNo;
                    ad.step = stepNum++;
                    list.Add(ad);
                }
                else {

                    string[] otherLeaveType = new string[] { "病假", "产假", "年假" };

                    //以下判断两个月累计是否超过30天的
                    var leaveRecord = db.vw_leaving_days.Where(v => v.applier_num == cardNo && !otherLeaveType.Contains(v.leave_type) && v.to_date > twoMonthsAgo).ToList();
                    int leaveDaysInTwoMonths = workDays;
                    decimal leaveHoursInTwoMonths = workHours;

                    foreach (var lr in leaveRecord.OrderByDescending(l=>l.from_date)) {
                        if (db.ei_leaveDayExceedPushLog.Where(e => e.sys_no == lr.sys_no).Count() > 0) {
                            break; //行政约谈过的不用到行政部
                        }
                        if (lr.from_date > twoMonthsAgo) {
                            leaveDaysInTwoMonths += lr.work_days ?? 0;
                        }
                        else {
                            leaveDaysInTwoMonths += ((DateTime)lr.to_date - twoMonthsAgo).Days;
                        }
                        leaveHoursInTwoMonths += lr.work_hours ?? 0;
                    }

                    leaveDaysInTwoMonths += (int)Math.Floor(leaveHoursInTwoMonths / 8);
                    if (leaveDaysInTwoMonths >= 30) {
                        var ad = GetGivenDepAuditor(administrationNo, PROCESSNAME);
                        ad.sys_no = sysNo;
                        ad.step = stepNum++;
                        list.Add(ad);
                    }
                    else {
                        //延假 2018-07-10
                        //以下判断是否连续请假15天，行政约谈过的就不用计算
                        if (leaveRecord.Count() > 0 && isContinue) {
                            int totalDays = workDays;
                            decimal totalHours = workHours;
                            var lastRecord = leaveRecord.OrderByDescending(l => l.to_date).First();
                            var hasPush = db.ei_leaveDayExceedPushLog.Where(e => e.sys_no == lastRecord.sys_no).Count() > 0;
                            if (!hasPush) {
                                totalDays += lastRecord.work_days ?? 0;
                                totalHours += lastRecord.work_hours ?? 0;
                            }
                            int skipNum = 0;
                            while (lastRecord.is_continue) {
                                if (leaveRecord.Count() > (++skipNum)) {
                                    lastRecord = leaveRecord.OrderByDescending(l => l.to_date).Skip(skipNum).First();
                                    hasPush = db.ei_leaveDayExceedPushLog.Where(e => e.sys_no == lastRecord.sys_no).Count() > 0;
                                    if (!hasPush) {
                                        //为行政约谈的
                                        totalDays += lastRecord.work_days ?? 0;
                                        totalHours += lastRecord.work_hours ?? 0;
                                    }
                                    else {
                                        //已行政约谈的，跳出循环
                                        break;
                                    }
                                }
                                else {
                                    //请假条数不足，跳出循环
                                    break;
                                }
                            }
                            totalDays += (int)Math.Floor(totalHours / 8);
                            if (totalDays >= 15) {
                                var ad = GetGivenDepAuditor(administrationNo, PROCESSNAME);
                                ad.sys_no = sysNo;
                                ad.step = stepNum++;
                                list.Add(ad);
                            }
                        }

                    }
                }
            }

            return list;
        }

        public FlowResultModel AbortAfterFinish(string sysNo, string reason)
        {
            var apps = db.flow_apply.Where(s => s.sys_no == sysNo).ToList();
            if (apps.Count() == 0) {
                return new FlowResultModel(false,"流水号不存在");
            }

            var app = apps.First();
            if (app.success == null) {
                return new FlowResultModel(false,"流程未结束，不能走撤销流程");
            }
            if (app.success == false) {
                return new FlowResultModel(false, "流程已被拒绝，不能走撤销流程");
            }
            if(app.user_abort==true){
                return new FlowResultModel(false,"撤销流程只能走一次");
            }

            //修改表头
            app.success = null;
            app.user_abort = true;
            app.finish_date = null;
            
            int maxStep=(int)app.flow_applyEntry.Max(f=>f.step);

            //插入表体记录
            maxStep++;
            flow_applyEntry ae = new flow_applyEntry();
            ae.apply_id = app.id;
            ae.auditors = app.create_user;
            ae.final_auditor = app.create_user;
            ae.opinion = reason;
            ae.pass = true;
            ae.step = maxStep;
            ae.step_name = "申请撤销";
            ae.audit_time = DateTime.Now;
            db.flow_applyEntry.InsertOnSubmit(ae);

            //第一步的审核人
            var s1 = GetPreStep(app, 1);
            maxStep++;
            foreach (var s in s1) {
                flow_applyEntry ae1 = new flow_applyEntry();
                ae1.apply_id = s.apply_id;
                ae1.auditors = s.auditors;
                ae1.flow_template_entry_id = s.flow_template_entry_id;
                ae1.step = maxStep;
                ae1.step_name = s.step_name;
                db.flow_applyEntry.InsertOnSubmit(ae1);
            }

            db.SubmitChanges();
            return new FlowResultModel(true, "撤销申请提交成功", string.Join(";", s1.Select(s => s.auditors).ToArray()));

        }

        
        public List<flow_applyEntryQueue> GetFlowQueue(string formObj)
        {
            return GetALAuditQueue(formObj);
        }
    }
}