using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowWebService.Models;

namespace FlowWebService.Interface
{
    /// <summary>
    /// 退回到之前的审核步骤然后继续流转
    /// </summary>
    interface IReturnToBeforeStep
    {
        string ReturnTo(string cardNumber, string sysNo, string currentStepName, string returnToStepName);
    }
}
