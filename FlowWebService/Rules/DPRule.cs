using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 宿舍维修申请流程
    /// </summary>
    public class DPRule:BaseRule,IBeforeStartFlow
    {
        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;

        #region 获取审核人的规则

        /// <summary>
        /// 取得同宿舍的舍友
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetRoomates(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string shareType = (string)o["fee_share_type"];
            bool isOutSide = (bool)o["is_outside"];
            if (!"舍友分摊".Equals(shareType) || isOutSide) {
                return "";
            }
            else {
                string sharePeople = (string)o["fee_share_peple"];
                return sharePeople;
            }
        }

        //获取本宿舍区的维修工
        public string GetRepairers(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string areaName = (string)o["area_name"];
            var repairmans = db.vw_dp_repairman.Where(v => v.area_name == areaName);
            if (repairmans.Count() > 0) {
                return repairmans.First().repairman_card_number;
            }
            return "";
        }
                

        #endregion

        #region OK和NG的Callback规则

        public void SaveFeeInDormSys(string formJson)
        {
            o = JObject.Parse(formJson);
            string dormNumber = (string)o["dorm_num"];
            string applierNumber = (string)o["applier_num"];
            string sysNo = (string)o["sys_no"];
            decimal repairCost = (decimal)o["charge_fee"];
            string shareType = (string)o["fee_share_type"];
            string sharePeople = (string)o["fee_share_peple"];
            string repairSubject=(string)o["repaire_subject"];
            string yearMonth = DateTime.Now.ToString("yyyyMM");
            string empIdsPay = (string)o["emp_id_should_pay"];

            if (repairCost > 0) {
                if (empIdsPay == null) {
                    string[] roomates = new string[] { };
                    if ("舍友分摊".Equals(shareType)) {
                        roomates = sharePeople.Split(new char[] { ';' });
                        repairCost = Math.Round(repairCost / (roomates.Count() + 1), 1);
                    }

                    //申请人扣费
                    db.DP_InsertRepairCost(dormNumber, applierNumber, repairCost, sysNo, repairSubject, yearMonth);

                    //舍友扣费
                    foreach (string roomate in roomates) {
                        db.DP_InsertRepairCost(dormNumber, roomate, repairCost, sysNo, repairSubject, yearMonth);
                    }
                }
                else {
                    if (!empIdsPay.Equals("")) {
                        //用empid导入，可以兼容厂外人员 2020-10-28
                        var empids = empIdsPay.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        repairCost = Math.Round(repairCost / empids.Count(), 1);
                        foreach (var empid in empids) {
                            db.DP_InsertRepairCostNew(dormNumber, int.Parse(empid), repairCost, sysNo, repairSubject, yearMonth);
                        }
                    }
                }
            }

            //插入出库记录和减去库存
            db.DP_InsertRepairItemRecord(sysNo);
        }

        #endregion


        //实现提交前验证的接口
        public void Validate(string formJson, string createUser)
        {
            //有未结束的申请，不能再次申请
            //o = JObject.Parse(formJson);
            //string areaDorm = (string)o["area_name"]+"_"+(string)o["dorm_num"];
            //var existedApply = db.flow_apply.Where(a => a.form_sub_title == areaDorm && a.form_title == "宿舍维修申请单" && a.success == null);
            var existedApply = db.flow_apply.Where(a => a.create_user == createUser && a.flow_template_id == 1 && a.success == null);
            if (existedApply.Count() > 0) {
                throw new Exception("你存在未结束的维修申请，申请单号：" + existedApply.First().sys_no + ";完结前不能再次申请");
            }
            
            //此宿舍区没有维护维修工受理人，也不能申请
            if (string.IsNullOrEmpty(GetRepairers(null, formJson))) {
                throw new Exception("当前宿舍区没有维修单受理人，请先联系后勤部设置，操作失败");
            }
        }


        public void DoBeforeFlow(string formObj)
        {
            
        }
    }
}